using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 3인칭(어깨 뒤) 리그 — 이 게임의 주력 시점 (GDD 2.4).
    /// 스프링 암 지연·충돌 회피를 담당하고, FOV/뱅킹/피치/셰이크는 CameraDynamics에 위임한다.
    /// 회전은 즉각적이며 위치만 지연된다 — 조작은 절대 느려지지 않는다.
    /// </summary>
    public sealed class ThirdPersonRig : CameraRig
    {
        [Header("스프링 암")]
        [SerializeField] private float _armLength = 7f;
        [Tooltip("기체 기준 피벗(어깨 오버) 오프셋. 요 회전을 따라 돈다")]
        [SerializeField] private Vector3 _pivotOffset = new(0.7f, 1.9f, 0f);

        [Header("충돌 회피 — 지면/벽에 카메라가 파묻히는 것 방지 (필수)")]
        [SerializeField] private float _collisionRadius = 0.25f;
        [Tooltip("Wall(13), Ground(14) 레이어")]
        [SerializeField] private LayerMask _collisionMask = (1 << 13) | (1 << 14);

        [Header("역동 연출 (강도는 CameraDynamics 인스펙터에서)")]
        [SerializeField] private CameraDynamics _dynamics;
        [SerializeField] private MechaController _mecha;

        private Vector3 _smoothedPivot;
        private bool _hasSmoothedPivot;

        public CameraDynamics Dynamics => _dynamics;

        public override void OnActivated(Camera camera, Transform followTarget)
        {
            // 전환 순간 지연 위치가 옛 좌표에서 끌려오지 않도록 스냅.
            _hasSmoothedPivot = false;
        }

        public override void Tick(Camera camera, Transform followTarget, float yaw, float pitch, float deltaTime)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pivot = followTarget.position + yawRotation * _pivotOffset;

            // 스프링 암 지연 — 위치만. 기체는 이미 즉시 움직였다.
            if (!_hasSmoothedPivot)
            {
                _smoothedPivot = pivot;
                _hasSmoothedPivot = true;
            }
            else
            {
                float response = _dynamics != null ? _dynamics.EffectiveLagResponse : 1000f;
                float t = 1f - Mathf.Exp(-response * deltaTime);
                _smoothedPivot = Vector3.Lerp(_smoothedPivot, pivot, t);
            }

            // 역동 연출 상태 갱신.
            float pitchOffset = 0f;
            float bank = 0f;
            if (_dynamics != null)
            {
                float speed01 = 0f;
                float lateral = 0f;
                float verticalInput = 0f;
                if (_mecha != null)
                {
                    Vector3 v = _mecha.Velocity;
                    speed01 = _mecha.HorizontalSpeed > 0f
                        ? new Vector2(v.x, v.z).magnitude / _mecha.HorizontalSpeed
                        : 0f;
                    lateral = _mecha.MoveInput.x;
                    verticalInput = _mecha.VerticalInput;
                }

                _dynamics.Tick(speed01, lateral, verticalInput, deltaTime);
                camera.fieldOfView = _dynamics.CurrentFov;
                pitchOffset = _dynamics.CurrentPitchOffset;
                bank = _dynamics.CurrentBank;
            }

            Quaternion rotation = Quaternion.Euler(pitch + pitchOffset, yaw, bank);
            Vector3 boomDirection = rotation * Vector3.back;
            Vector3 desired = _smoothedPivot + boomDirection * _armLength;

            // 피벗 → 카메라 스피어캐스트로 지형 끼임 방지.
            float distance = _armLength;
            if (Physics.SphereCast(_smoothedPivot, _collisionRadius, boomDirection,
                    out RaycastHit hit, _armLength, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
            }

            Vector3 finalPosition = _smoothedPivot + boomDirection * distance;
            if (_dynamics != null)
            {
                finalPosition += rotation * _dynamics.EvaluateShakeOffset();
            }

            camera.transform.SetPositionAndRotation(finalPosition, rotation);
        }
    }
}
