using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 플레이어 이동: 입력 → 속도/위치. 지연 0, 관성 0 (GDD 2.3).
    /// 기본 상태는 비행이며, 하강해 접지하면 보행(Grounded)으로 전환된다.
    /// 표현(메시 회전·VFX·사운드)은 MechaVisuals가 이 컴포넌트의 상태를 읽어 처리한다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(MechaInput))]
    public sealed class MechaController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private CameraDirector _camera;

        [Header("속도 — 즉시 도달 (가속 램프 없음)")]
        [SerializeField] private float _horizontalSpeed = 14f;
        [SerializeField] private float _ascendSpeed = 9f;
        [SerializeField] private float _descendSpeed = 12f;

        [Header("고도")]
        [Tooltip("절대 고도 상한(월드 Y). 무한 상승 방지 (GDD 2.2)")]
        [SerializeField] private float _ceilingHeight = 60f;

        [Tooltip("접지 유지용 하향 속도. 보행 중 경사/단차에서 isGrounded가 끊기는 것을 방지")]
        [SerializeField] private float _groundStick = 2f;

        /// <summary>보행 상태 여부. 쿨다운 가속 판정(CooldownModifier)이 읽는다.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>현재 프레임 속도. 카메라 연출(FOV/뱅킹)이 읽는다.</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>수평 이동 입력 원본 (-1~1). 뱅킹 연출이 읽는다.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>수직 입력 원본 (-1~1). 피치 오프셋 연출이 읽는다.</summary>
        public float VerticalInput { get; private set; }

        public float HorizontalSpeed => _horizontalSpeed * _speedMultiplier;
        public float CeilingHeight => _ceilingHeight;

        private float _speedMultiplier = 1f;
        private Vector3 _impulse;

        [Header("반동 임펄스")]
        [Tooltip("외부 임펄스(산탄 반동 등) 감쇠 속도 — 클수록 빨리 멈춘다")]
        [SerializeField] private float _impulseDamping = 5f;

        [Header("대시 (Shift) — 순간 가속, 에어 트레일은 MechaVisuals가 그린다")]
        [SerializeField] private float _dashSpeed = 42f;
        [SerializeField] private float _dashCooldown = 1.6f;

        [Tooltip("대시 연출(트레일·오프셋)이 유지되는 시간(초)")]
        [SerializeField] private float _dashDuration = 0.35f;

        private float _nextDashTime;
        private float _dashEndTime;

        /// <summary>대시 연출 창 — 에어 트레일(MechaVisuals)이 읽는다.</summary>
        public bool IsDashing => Time.time < _dashEndTime;

        /// <summary>에너지 계열 업그레이드(기동력)가 호출. 0.1 = +10%.</summary>
        public void AddMoveSpeedMultiplier(float amount) =>
            _speedMultiplier = Mathf.Max(0.1f, _speedMultiplier + amount);

        /// <summary>순간 임펄스(m/s) — 산탄 캐논 반동이 기체를 뒤로 민다 (GDD 3.4-4).</summary>
        public void AddImpulse(Vector3 impulse) => _impulse += impulse;

        private CharacterController _cc;
        private MechaInput _input;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<MechaInput>();
        }

        private void Update()
        {
            MechaInputFrame frame = _input.Frame;
            float yaw = _camera != null ? _camera.Yaw : transform.eulerAngles.y;

            // 대시 — 향하고 있는 방향(입력 우선)으로 순간 가속 (GDD 2.3: 반응은 즉각적으로).
            if (frame.DashPressed && Time.time >= _nextDashTime)
            {
                Vector3 dashDirection = MechaMotor.DashDirection(frame.Move, yaw, transform.forward);
                AddImpulse(dashDirection * _dashSpeed);
                _nextDashTime = Time.time + _dashCooldown;
                _dashEndTime = Time.time + _dashDuration;
            }

            Vector3 velocity = MechaMotor.ComputeVelocity(
                frame.Move, frame.Vertical, yaw,
                _horizontalSpeed * _speedMultiplier,
                _ascendSpeed * _speedMultiplier,
                _descendSpeed * _speedMultiplier);

            // 반동 임펄스 — 속도에 얹은 뒤 지수 감쇠 (짧게 밀리고 즉시 조작감 복귀).
            velocity += _impulse;
            _impulse = MechaMotor.DecayImpulse(_impulse, _impulseDamping, Time.deltaTime);

            velocity.y = MechaMotor.ClampAscent(
                transform.position.y, velocity.y, Time.deltaTime, _ceilingHeight);

            // 보행 중 수직 입력이 없으면 살짝 눌러 접지를 유지한다 (비행 복귀는 Space 한 번).
            if (IsGrounded && Mathf.Approximately(frame.Vertical, 0f) && _impulse == Vector3.zero)
            {
                velocity.y = -_groundStick;
            }

            _cc.Move(velocity * Time.deltaTime);
            IsGrounded = _cc.isGrounded;

            Velocity = velocity;
            MoveInput = frame.Move;
            VerticalInput = frame.Vertical;
        }
    }
}
