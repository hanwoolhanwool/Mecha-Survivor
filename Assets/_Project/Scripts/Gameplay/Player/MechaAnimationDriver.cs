using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// MechaController 상태를 폴링해 Animator 파라미터로 공급한다 (Docs/05 §6).
    /// 폴링: MoveX/MoveZ/Speed/IsGrounded. 이벤트: Fire(WeaponFired 유지창)/HitTrigger(PlayerDamaged).
    /// 좌표 변환·유지창 판정은 MechaAnimParams(순수 로직)가 전담한다. 대시(P5)는 추후 확장.
    /// </summary>
    [RequireComponent(typeof(MechaController))]
    public sealed class MechaAnimationDriver : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("모델 인스턴스의 Animator. 비우면 자식에서 찾는다")]
        [SerializeField] private Animator _animator;

        [Tooltip("시각 요 기준 — MechaVisuals가 회전시키는 시각 루트(Body). 비우면 Animator의 부모")]
        [SerializeField] private Transform _visualRoot;

        [Header("스무딩")]
        [Tooltip("파라미터 댐핑(초). 관성 0 원칙 — 블렌드 떨림만 잡을 만큼 짧게")]
        [SerializeField] private float _dampTime = 0.08f;

        [Header("사격")]
        [Tooltip("WeaponFiredEvent 후 Fire 파라미터를 유지하는 시간(초) — 자동 연사 사이 끊김 방지")]
        [SerializeField] private float _fireWindow = 0.3f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");

        private MechaController _controller;
        private float _fireUntil;
        private bool _hitPending;

        private void Awake()
        {
            _controller = GetComponent<MechaController>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_visualRoot == null && _animator != null)
            {
                _visualRoot = _animator.transform.parent;
            }
        }

        private void OnEnable()
        {
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
        }

        private void OnDisable()
        {
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
        }

        private void OnWeaponFired(WeaponFiredEvent evt)
        {
            _fireUntil = MechaAnimParams.ExtendFireWindow(Time.time, _fireWindow);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            _hitPending = true;
        }

        private void LateUpdate()
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            Vector3 forward = _visualRoot != null ? _visualRoot.forward : transform.forward;
            float referenceSpeed = _controller.HorizontalSpeed;

            Vector2 move = MechaAnimParams.ComputeMove(_controller.Velocity, forward, referenceSpeed);
            float speed = MechaAnimParams.ComputeSpeed(_controller.Velocity, referenceSpeed);

            float dt = Time.deltaTime;
            _animator.SetFloat(MoveXHash, move.x, _dampTime, dt);
            _animator.SetFloat(MoveZHash, move.y, _dampTime, dt);
            _animator.SetFloat(SpeedHash, speed, _dampTime, dt);
            _animator.SetBool(IsGroundedHash, _controller.IsGrounded);
            _animator.SetBool(FireHash, MechaAnimParams.IsFireActive(Time.time, _fireUntil));

            if (_hitPending)
            {
                _hitPending = false;
                _animator.SetTrigger(HitTriggerHash);
            }
        }
    }
}
