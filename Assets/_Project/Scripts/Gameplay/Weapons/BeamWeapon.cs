using System.Collections;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 대출력 빔 (GDD 3.4 무기 6번 — Finisher).
    /// ① 차징(예고) → ② 발사(관통) → ③ 지속(조준을 따라 휩쓸림) → ④ 종료.
    /// 시각은 원통 메시(볼륨 빔) — 어느 각도에서도 둥근 기둥으로 보인다.
    /// 판정은 매 틱 레이캐스트, 시각은 매 프레임 갱신 (판정과 화려함의 분리 — GDD 3.6-7).
    /// </summary>
    public sealed class BeamWeapon : Weapon
    {
        [Header("빔 시각화 — 원통 (비우면 자식 'BeamCylinder' 탐색)")]
        [SerializeField] private Transform _beamCylinder;

        [Tooltip("Enemy(9), Wall(13), Ground(14)")]
        [SerializeField] private LayerMask _hitMask = (1 << 9) | (1 << 13) | (1 << 14);

        [Tooltip("빔 기본 지름 — VisualScale을 곱해 최종 굵기가 된다")]
        [SerializeField] private float _baseWidth = 0.9f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];

        private bool _beamActive;

        public bool IsBeaming => _beamActive;

        private void Awake()
        {
            if (_beamCylinder == null)
            {
                Transform found = transform.Find("BeamCylinder");
                if (found != null)
                {
                    _beamCylinder = found;
                }
            }

            HideBeam();
        }

        protected override void Fire(MechaAimer aimer)
        {
            if (!_beamActive)
            {
                StartCoroutine(BeamRoutine(aimer));
            }
        }

        private IEnumerator BeamRoutine(MechaAimer aimer)
        {
            _beamActive = true;

            // ① 차징 — "무언가 온다"의 예고.
            if (Data.ChargeTime > 0f)
            {
                yield return new WaitForSeconds(Data.ChargeTime);
            }

            // ② ③ 발사·지속 — 시각은 매 프레임, 판정은 틱 간격으로.
            float width = _baseWidth * Data.GetVisualScale(Level);
            float tickInterval = 1f / Mathf.Max(1f, Data.BeamTicksPerSecond);
            float damagePerTick = Data.GetDamage(Level) * tickInterval;

            float elapsed = 0f;
            float tickTimer = 0f;

            while (elapsed < Data.BeamDuration)
            {
                yield return null;
                float dt = Time.deltaTime;
                elapsed += dt;
                tickTimer += dt;

                Vector3 origin = Muzzle.position;
                Vector3 direction = aimer != null
                    ? aimer.FireDirectionFrom(origin)
                    : Muzzle.forward;

                float visualLength = Data.Range;

                if (tickTimer >= tickInterval)
                {
                    tickTimer -= tickInterval;
                    visualLength = DamageAlongBeam(origin, direction, damagePerTick);
                }

                UpdateBeamVisual(origin, direction, visualLength, width);
            }

            // ④ 종료.
            HideBeam();
            _beamActive = false;
        }

        /// <summary>
        /// 일직선상의 모든 적을 관통 — 뭉친 무리를 한 줄로 증발시킨다.
        /// 반환: 벽/지면에 막힌 지점까지의 거리 (시각 길이용. 막힘 없으면 Range).
        /// </summary>
        private float DamageAlongBeam(Vector3 origin, Vector3 direction, float damage)
        {
            int count = Physics.RaycastNonAlloc(
                origin, direction, HitBuffer, Data.Range, _hitMask, QueryTriggerInteraction.Ignore);

            float blockedDistance = Data.Range;
            for (int i = 0; i < count; i++)
            {
                int layer = HitBuffer[i].collider.gameObject.layer;
                if ((layer == 13 || layer == 14) && HitBuffer[i].distance < blockedDistance)
                {
                    blockedDistance = HitBuffer[i].distance;
                }
            }

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                if (hit.distance > blockedDistance)
                {
                    continue; // 벽 뒤의 적은 맞지 않는다.
                }

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                damageable.TakeDamage(damage,
                    new DamageInfo(hit.point, direction, false, Data.Id));
                EventBus<DamageDealtEvent>.Raise(
                    new DamageDealtEvent(Data.Id, damage, hit.point, !damageable.IsAlive));
            }

            return blockedDistance;
        }

        /// <summary>원통을 총구→끝점 축에 맞춰 배치 (Unity 원통은 Y축 길이 ±1).</summary>
        private void UpdateBeamVisual(Vector3 origin, Vector3 direction, float length, float width)
        {
            if (_beamCylinder == null)
            {
                return;
            }

            if (!_beamCylinder.gameObject.activeSelf)
            {
                _beamCylinder.gameObject.SetActive(true);
            }

            _beamCylinder.SetPositionAndRotation(
                origin + direction * (length * 0.5f),
                Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f));
            _beamCylinder.localScale = new Vector3(width, length * 0.5f, width);
        }

        private void HideBeam()
        {
            if (_beamCylinder != null)
            {
                _beamCylinder.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            _beamActive = false;
            HideBeam();
        }
    }
}
