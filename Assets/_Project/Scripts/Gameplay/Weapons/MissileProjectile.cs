using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 미사일 — 이 게임의 간판 연출 (GDD 3.4 무기 3번). 4단 연출:
    /// ① 팝업(수직 사출) → ② 정렬(멈칫하며 노즈 회전 — 화려함의 8할)
    /// → ③ 유도(서로 다른 곡선으로 흩어졌다 수렴) → ④ 착탄(스태거는 무기가 만든다).
    /// 곧게 날아가면 하나도 안 화려하다 — 일단 위로 튀고 곡선으로 꺾여야 궤적이 그려진다.
    /// </summary>
    public sealed class MissileProjectile : Projectile
    {
        private enum Phase
        {
            Popup,
            Align,
            Homing,
        }

        [Header("① 팝업")]
        [SerializeField] private float _popupSpeed = 14f;
        [SerializeField] private float _popupDuration = 0.22f;

        [Tooltip("팝업 방향 흩뿌림(도) — 미사일마다 다른 곳으로 튀어오른다")]
        [SerializeField] private float _popupJitterAngle = 25f;

        [Header("② 정렬 — 여기가 화려함의 8할")]
        [SerializeField] private float _alignDuration = 0.18f;

        [Header("③ 유도")]
        [SerializeField] private float _homingTurnRateOverride = 360f;

        [Tooltip("곡선 편향 강도 — 클수록 나선/S자 궤적이 커진다")]
        [SerializeField] private float _curveStrength = 10f;

        [Tooltip("곡선 편향이 사라지는 시간(초) — 이후 목표로 수렴")]
        [SerializeField] private float _curveDecayTime = 0.7f;

        [Header("④ 착탄 — 폭발 범위 피해 (무리를 지운다)")]
        [Tooltip("폭발 반경. 0 = 직격만")]
        [SerializeField] private float _explosionRadius = 2.5f;

        [Tooltip("타깃 소멸 시 재타겟 탐색 반경 — 느린 미사일이 헛발이 되지 않게")]
        [SerializeField] private float _retargetRadius = 30f;

        private Phase _phase;
        private float _phaseAge;
        private float _homingAge;
        private float _cruiseSpeed;
        private Vector3 _fallbackTarget;
        private Vector3 _curveBias;
        private Transform _retarget;

        /// <summary>
        /// 미사일 발사. data.Direction은 무시되고 팝업 → 정렬 → 유도 순서로 스스로 궤도를 만든다.
        /// fallbackTarget은 유도 타깃이 죽었을 때 향할 지점(발사 시 조준점).
        /// </summary>
        public void LaunchMissile(in ProjectileLaunchData data, Vector3 fallbackTarget)
        {
            Launch(data);

            _fallbackTarget = fallbackTarget;
            _cruiseSpeed = data.Speed;
            _phase = Phase.Popup;
            _phaseAge = 0f;
            _homingAge = 0f;
            _retarget = null;

            // ① 수직 팝업 — 미사일마다 조금씩 다른 방향으로.
            Quaternion jitter = Quaternion.Euler(
                Random.Range(-_popupJitterAngle, _popupJitterAngle),
                Random.Range(0f, 360f),
                0f);
            Vector3 popupDirection = jitter * Vector3.up;
            Velocity = popupDirection * _popupSpeed;

            // ③에서 쓸 곡선 편향 — 진행 축에 수직인 임의 방향.
            Vector2 lateral = Random.insideUnitCircle.normalized;
            _curveBias = new Vector3(lateral.x, Random.Range(-0.3f, 0.6f), lateral.y) * _curveStrength;

            transform.rotation = Quaternion.LookRotation(popupDirection);
        }

        protected override void Steer(float deltaTime)
        {
            _phaseAge += deltaTime;

            switch (_phase)
            {
                case Phase.Popup:
                    if (_phaseAge >= _popupDuration)
                    {
                        _phase = Phase.Align;
                        _phaseAge = 0f;
                    }

                    break;

                case Phase.Align:
                {
                    // ② 공중에서 멈칫하며 노즈를 목표로 돌린다.
                    float t = Mathf.Clamp01(_phaseAge / _alignDuration);
                    Vector3 toTarget = (TargetPoint() - transform.position).normalized;
                    Vector3 slowed = Velocity.normalized * Mathf.Lerp(_popupSpeed, _cruiseSpeed * 0.25f, t);
                    Velocity = Vector3.Slerp(slowed, toTarget * (_cruiseSpeed * 0.25f), t);

                    if (_phaseAge >= _alignDuration)
                    {
                        _phase = Phase.Homing;
                        _phaseAge = 0f;
                    }

                    break;
                }

                case Phase.Homing:
                {
                    // ③ 곡선 편향이 감쇠하며 목표로 수렴 — 나선/S자 궤적.
                    _homingAge += deltaTime;
                    float bias = _curveDecayTime > 0f
                        ? Mathf.Clamp01(1f - _homingAge / _curveDecayTime)
                        : 0f;

                    Vector3 desired =
                        (TargetPoint() - transform.position).normalized * _cruiseSpeed
                        + _curveBias * bias;

                    float speed = Mathf.Min(_cruiseSpeed, Velocity.magnitude + _cruiseSpeed * 3f * deltaTime);
                    Velocity = Vector3.RotateTowards(
                        Velocity.normalized * speed,
                        desired,
                        _homingTurnRateOverride * Mathf.Deg2Rad * deltaTime,
                        _cruiseSpeed * 3f * deltaTime);
                    break;
                }
            }
        }

        private Vector3 TargetPoint()
        {
            if (HomingTarget != null && HomingTarget.gameObject.activeInHierarchy)
            {
                return HomingTarget.position;
            }

            // 원래 타깃 소멸 — 근처의 다른 적으로 재타겟 (느린 미사일의 헛발 방지).
            if (_retarget != null && _retarget.gameObject.activeInHierarchy)
            {
                return _retarget.position;
            }

            _retarget = FindRetarget();
            return _retarget != null ? _retarget.position : _fallbackTarget;
        }

        private Transform FindRetarget()
        {
            var enemies = EnemyBrain.ActiveEnemies;
            Transform nearest = null;
            float nearestSqr = _retargetRadius * _retargetRadius;
            Vector3 self = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                float sqr = (enemies[i].transform.position - self).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemies[i].transform;
                }
            }

            return nearest;
        }

        /// <summary>착탄/소멸 지점 폭발 — 반경 내 모든 적에게 피해 (GDD: 무리를 지운다).</summary>
        protected override void OnExpire(Vector3 position)
        {
            if (_explosionRadius <= 0f)
            {
                return;
            }

            float radius = _explosionRadius * Mathf.Max(transform.localScale.x, 0.1f);
            AreaDamage.Apply(position, radius, Damage, SourceId);
        }
    }
}
