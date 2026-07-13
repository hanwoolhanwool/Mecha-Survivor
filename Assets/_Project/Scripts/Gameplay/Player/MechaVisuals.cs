using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 표현 전담 레이어 (GDD 2.3). MechaController의 상태를 읽기만 하며,
    /// 게임플레이(판정·이동·조준)에는 0의 지연도 주지 않는다.
    /// 여기서는 얼마든지 보간·지연해도 된다 — 연출 튜닝이 조작감을 절대 망치지 못한다.
    /// </summary>
    public sealed class MechaVisuals : MonoBehaviour
    {
        [Header("참조 (읽기 전용)")]
        [SerializeField] private MechaController _controller;
        [SerializeField] private CameraDirector _camera;

        [Tooltip("연출이 적용되는 시각 루트 (메시). 판정과 무관")]
        [SerializeField] private Transform _visualRoot;

        [Header("호버링 아이들 — 떠 있는 기계")]
        [SerializeField] private float _hoverAmplitude = 0.12f;
        [SerializeField] private float _hoverFrequency = 1.6f;

        [Header("이동 기울임 (기체가 진행 방향으로 숙임)")]
        [SerializeField] private float _maxLeanAngle = 12f;
        [SerializeField] private float _leanResponse = 8f;

        [Header("하체 요 회전 — 이동 방향을 드르륵 따라간다")]
        [SerializeField] private float _yawFollowResponse = 10f;

        private Vector3 _basePosition;
        private float _visualYaw;
        private float _leanPitch;
        private float _leanRoll;
        private float _hoverPhase;

        private void Awake()
        {
            if (_visualRoot != null)
            {
                _basePosition = _visualRoot.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (_controller == null || _visualRoot == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            Vector3 velocity = _controller.Velocity;
            Vector2 planar = new(velocity.x, velocity.z);

            // ── 하체 요: 이동 중이면 이동 방향, 정지 중이면 카메라 방향을 천천히 따라간다.
            float targetYaw = _visualYaw;
            if (planar.sqrMagnitude > 0.5f)
            {
                targetYaw = Mathf.Atan2(planar.x, planar.y) * Mathf.Rad2Deg;
            }
            else if (_camera != null)
            {
                targetYaw = _camera.Yaw;
            }

            _visualYaw = Mathf.LerpAngle(_visualYaw, targetYaw,
                1f - Mathf.Exp(-_yawFollowResponse * dt));

            // ── 이동 기울임: 로컬 기준 전후/좌우 속도 비율만큼 숙인다.
            float speedRef = Mathf.Max(_controller.HorizontalSpeed, 0.01f);
            Quaternion yawRotation = Quaternion.Euler(0f, _visualYaw, 0f);
            Vector3 localVel = Quaternion.Inverse(yawRotation) * new Vector3(planar.x, 0f, planar.y);
            float targetPitch = Mathf.Clamp(localVel.z / speedRef, -1f, 1f) * _maxLeanAngle;
            float targetRoll = -Mathf.Clamp(localVel.x / speedRef, -1f, 1f) * _maxLeanAngle;

            float leanT = 1f - Mathf.Exp(-_leanResponse * dt);
            _leanPitch = Mathf.Lerp(_leanPitch, targetPitch, leanT);
            _leanRoll = Mathf.Lerp(_leanRoll, targetRoll, leanT);

            _visualRoot.localRotation = Quaternion.Euler(_leanPitch, _visualYaw, _leanRoll);

            // ── 호버링 아이들: 공중 정지 시 미세 부유. 보행/이동 중엔 잦아든다.
            _hoverPhase += dt * _hoverFrequency * Mathf.PI * 2f;
            float idleness = _controller.IsGrounded
                ? 0f
                : 1f - Mathf.Clamp01(planar.magnitude / speedRef);
            float bob = Mathf.Sin(_hoverPhase) * _hoverAmplitude * idleness;
            _visualRoot.localPosition = _basePosition + new Vector3(0f, bob, 0f);
        }
    }
}
