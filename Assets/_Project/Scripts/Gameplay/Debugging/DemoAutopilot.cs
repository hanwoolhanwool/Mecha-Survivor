using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 데모/녹화용 오토파일럿. MechaInput에 합성 입력을 주입해
    /// 선회 기동 + 가장 가까운 적 자동 조준 + 무기 로테이션을 시연한다.
    /// 씬에 두지 않고 필요 시 런타임에 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public sealed class DemoAutopilot : MonoBehaviour
    {
        public MechaInput Input;
        public CameraDirector Camera;

        [Tooltip("타깃이 없을 때 초당 카메라 요 회전(도)")]
        public float IdleYawDegreesPerSecond = 30f;

        [Tooltip("조준 수렴 속도 (클수록 빨리 적을 문다)")]
        public float AimGain = 6f;

        [Tooltip("CameraDirector의 감도와 맞춰야 실제 회전 속도가 나온다")]
        public float LookSensitivity = 0.12f;

        [Tooltip("리드 샷 계산에 쓸 탄속 (0이면 슬롯 1 무기 데이터에서 읽음)")]
        public float ProjectileSpeedOverride;

        [Tooltip("타깃 교체 주기(초) — 에임이 한 곳에 고정되지 않고 화면을 훑는다")]
        public float RetargetInterval = 2.5f;

        [Tooltip("조준 흔들림 진폭(도) — 목표각에 더해지는 유계 오프셋. 살아있는 에임 연출")]
        public float AimWander = 1.5f;

        public WeaponSlots Slots;

        private float _elapsed;
        private float _nextRetargetTime;
        private EnemyBrain _target;
        private Vector3 _targetLastPosition;
        private Vector3 _targetVelocity;

        private void Update()
        {
            if (Input == null || Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            _elapsed += Time.deltaTime;

            // 좌측 스트레이프 + 전진을 섞은 궤도 선회.
            // 빔 발사 중에는 정지 사격 — 광선을 목표에 유지한다.
            Vector2 move;
            float vertical;
            if (IsBeamActive())
            {
                move = Vector2.zero;
                vertical = 0f;
            }
            else
            {
                move = new Vector2(-0.8f, Mathf.PingPong(_elapsed * 0.35f, 1f));
                vertical = Mathf.Sin(_elapsed * 0.4f) * 0.6f;
            }

            Vector2 look = ComputeLook();

            // 단발 무기(미사일/빔/그래비티)는 '누른 순간'에만 발사되므로
            // 홀드가 아니라 클릭 연타를 흉내 낸다 (0.25초 간격 펄스 — 쿨만 돌면 발사).
            bool pulse = Mathf.FloorToInt(_elapsed / 0.25f) % 2 == 0;

            Input.InjectFrame(new MechaInputFrame(
                move, vertical, look,
                fire1Held: true,
                fire2Held: pulse,
                fire3Held: pulse,
                fire4Held: pulse,
                cameraTogglePressed: false,
                pausePressed: false));
        }

        /// <summary>
        /// 타깃(고정 추적)을 향한 요/피치 델타. 이동 타깃은 탄속 기반 리드 샷으로 예측 조준한다
        /// — 리드가 없으면 탄이 이동 방향 뒤로 빗나간다 (검증됨: 정지 표적 명중률 100%).
        /// </summary>
        private Vector2 ComputeLook()
        {
            float sensitivity = Mathf.Max(LookSensitivity, 0.001f);

            UpdateTarget();
            if (Camera == null || _target == null)
            {
                return new Vector2(IdleYawDegreesPerSecond * Time.deltaTime / sensitivity, 0f);
            }

            Vector3 aimPoint = _target.transform.position + Vector3.up * 0.8f;

            // 리드 샷: 탄속과 타깃 속도로 요격 방향을 푼다 (Ballistics 재사용).
            // 단, 빔(히트스캔) 발사 중에는 리드가 오히려 빗나가게 하므로 직접 조준한다.
            float projectileSpeed = ProjectileSpeedOverride;
            if (projectileSpeed <= 0f && Slots != null)
            {
                Weapon primary = Slots.GetWeapon(0);
                if (primary != null && primary.Data != null)
                {
                    projectileSpeed = primary.Data.ProjectileSpeed;
                }
            }

            Vector3 direction;
            if (projectileSpeed > 0f && !IsBeamActive())
            {
                Ballistics.TryPredictInterceptDirection(
                    Camera.AimOrigin, aimPoint, _targetVelocity, projectileSpeed, out direction);
            }
            else
            {
                direction = (aimPoint - Camera.AimOrigin).normalized;
            }

            float desiredYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float desiredPitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;

            // 살아있는 에임: 목표각에 유계 사인 오프셋(±AimWander도) — 델타에 더하면
            // 프레임마다 적분되어 거대한 스윙이 되므로 반드시 목표각에 더한다.
            // 빔 발사 중에는 흔들림도 끈다 — 광선이 목표에 머물러야 한다.
            if (!IsBeamActive())
            {
                desiredYaw += Mathf.Sin(_elapsed * 1.6f) * AimWander;
                desiredPitch += Mathf.Sin(_elapsed * 2.3f + 1.3f) * AimWander * 0.5f;
            }

            // '현재 각'은 CameraDirector의 Yaw/Pitch가 아니라 실제 조준 레이(AimDirection)에서
            // 역산한다 — CameraDynamics의 피치 오프셋/뱅킹이 조준 레이에 얹히므로,
            // 내부 각 기준으로 조향하면 연출 오프셋만큼 항상 빗나간다 (검증됨: 상승/하강 중 ~5° 편차).
            Vector3 currentDirection = Camera.AimDirection;
            float currentYaw = Mathf.Atan2(currentDirection.x, currentDirection.z) * Mathf.Rad2Deg;
            float currentPitch = -Mathf.Asin(Mathf.Clamp(currentDirection.y, -1f, 1f)) * Mathf.Rad2Deg;

            float gain = Mathf.Min(AimGain * Time.deltaTime, 1f);
            float yawDelta = Mathf.DeltaAngle(currentYaw, desiredYaw) * gain;
            float pitchDelta = (desiredPitch - currentPitch) * gain;

            // CameraDirector: Yaw += x·감도, Pitch -= y·감도.
            return new Vector2(yawDelta / sensitivity, -pitchDelta / sensitivity);
        }

        /// <summary>
        /// 타깃 추적: 죽거나 멀어지면 즉시 교체, 살아 있어도 RetargetInterval마다
        /// 다른 적으로 갈아탄다 — 에임이 계속 움직이며 화면을 훑는다.
        /// </summary>
        private void UpdateTarget()
        {
            bool targetValid = _target != null && _target.gameObject.activeInHierarchy &&
                               (_target.transform.position - transform.position).sqrMagnitude < 100f * 100f;

            // 빔 발사 중에는 타깃을 갈아타지 않는다 — 광선을 한 목표에 유지.
            if (targetValid && IsBeamActive())
            {
                _nextRetargetTime = Time.time + RetargetInterval;
            }

            if (!targetValid || Time.time >= _nextRetargetTime)
            {
                _target = targetValid ? PickRandomTarget() : FindNearestEnemy();
                _nextRetargetTime = Time.time + RetargetInterval;
                if (_target != null)
                {
                    _targetLastPosition = _target.transform.position;
                    _targetVelocity = Vector3.zero;
                }

                return;
            }

            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                Vector3 position = _target.transform.position;
                _targetVelocity = (position - _targetLastPosition) / dt;
                _targetLastPosition = position;
            }
        }

        /// <summary>장착 무기 중 빔이 발사 중인지 — 발사 중엔 직접 조준(히트스캔)으로 전환.</summary>
        private bool IsBeamActive()
        {
            if (Slots == null)
            {
                return false;
            }

            for (int i = 0; i < WeaponSlots.MaxSlots; i++)
            {
                if (Slots.GetWeapon(i) is BeamWeapon beam && beam.IsBeaming)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>90m 내 임의의 적 — 타깃 교체용 (근접 5기 중 랜덤 대신 단순 랜덤 시도).</summary>
        private EnemyBrain PickRandomTarget()
        {
            var enemies = EnemyBrain.ActiveEnemies;
            if (enemies.Count == 0)
            {
                return null;
            }

            for (int attempt = 0; attempt < 5; attempt++)
            {
                EnemyBrain candidate = enemies[Random.Range(0, enemies.Count)];
                if (candidate != _target &&
                    (candidate.transform.position - transform.position).sqrMagnitude < 90f * 90f)
                {
                    return candidate;
                }
            }

            return FindNearestEnemy();
        }

        private EnemyBrain FindNearestEnemy()
        {
            var enemies = EnemyBrain.ActiveEnemies;
            EnemyBrain nearest = null;
            float nearestSqr = float.MaxValue;
            Vector3 self = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                float sqr = (enemies[i].transform.position - self).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemies[i];
                }
            }

            return nearest;
        }
    }
}
