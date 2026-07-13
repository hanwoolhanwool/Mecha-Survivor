using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
    }

    /// <summary>
    /// 카메라 모드 보유·전환(V 키)과 마우스 룩 상태(요/피치)의 단일 소유자.
    /// 활성 리그에 매 프레임 배치를 위임하고, ICameraRig 계약은 카메라 트랜스폼으로 이행한다.
    /// 요/피치를 여기서 공유하므로 시점을 전환해도 조준 방향이 유지된다 (GDD 2.4).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class CameraDirector : MonoBehaviour, ICameraRig
    {
        [Header("참조")]
        [SerializeField] private MechaInput _input;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Camera _camera;
        [SerializeField] private FirstPersonRig _firstPerson;
        [SerializeField] private ThirdPersonRig _thirdPerson;

        [Header("설정")]
        [Tooltip("기본 시점은 3인칭 — 3D 공중전은 주변 파악이 생명 (GDD §9-7)")]
        [SerializeField] private CameraMode _startMode = CameraMode.ThirdPerson;
        [SerializeField] private float _lookSensitivity = 0.12f;
        [SerializeField] private float _pitchMin = -80f;
        [SerializeField] private float _pitchMax = 80f;
        [SerializeField] private bool _lockCursor = true;

        public CameraMode Mode { get; private set; }

        /// <summary>마우스 룩 요(도). MechaController가 수평 이동 기준으로 읽는다.</summary>
        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        // ── ICameraRig — 시선은 언제나 실제 카메라 기준 (시점 종류와 무관) ──
        public Vector3 AimOrigin => _camera != null ? _camera.transform.position : transform.position;
        public Vector3 AimDirection => _camera != null ? _camera.transform.forward : transform.forward;

        public Camera Camera => _camera;

        private CameraRig ActiveRig =>
            Mode == CameraMode.FirstPerson ? _firstPerson : _thirdPerson;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mode = _startMode;
        }

        private void Start()
        {
            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            CameraRig rig = ActiveRig;
            if (rig != null && _camera != null && _followTarget != null)
            {
                rig.OnActivated(_camera, _followTarget);
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _followTarget == null || _input == null)
            {
                return;
            }

            // 3택/결과 화면 등 시간 정지 중에는 시점을 잠근다 (마우스는 UI 몫).
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            MechaInputFrame frame = _input.Frame;

            // 마우스 → 조준 회전. 보간 없음 — 즉시 그곳을 조준한다 (GDD 2.3).
            Yaw += frame.Look.x * _lookSensitivity;
            Pitch = Mathf.Clamp(Pitch - frame.Look.y * _lookSensitivity, _pitchMin, _pitchMax);

            if (frame.CameraTogglePressed)
            {
                Toggle();
            }

            CameraRig rig = ActiveRig;
            if (rig != null)
            {
                rig.Tick(_camera, _followTarget, Yaw, Pitch, Time.deltaTime);
            }
        }

        public void Toggle()
        {
            Mode = Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;

            CameraRig rig = ActiveRig;
            if (rig != null)
            {
                rig.OnActivated(_camera, _followTarget);

                // 1인칭 진입 시 3인칭 연출 FOV가 남지 않도록 기본값으로 복원.
                if (Mode == CameraMode.FirstPerson && _thirdPerson != null &&
                    _thirdPerson.Dynamics != null)
                {
                    _camera.fieldOfView = _thirdPerson.Dynamics.BaseFov;
                }
            }
        }
    }
}
