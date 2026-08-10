using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// MechaController 상태를 폴링해 Animator 파라미터로 공급한다 (Docs/05 §6).
    /// 폴링: MoveX/MoveZ/Speed/IsGrounded + IsDashing 상승 엣지(DashTrigger/DashX/DashZ).
    /// 이벤트: Fire(WeaponFired 유지창)/HitTrigger(PlayerDamaged).
    /// 좌표 변환·유지창 판정은 MechaAnimParams(순수 로직)가 전담한다.
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

        [Header("무기 포즈 (Docs/05 §10-B8·B10)")]
        [Tooltip("총 종류 무기 발사 후 전신 포즈를 유지하는 시간(초)")]
        [SerializeField] private float _poseHoldSeconds = 2f;

        [Header("피격")]
        [Tooltip("강피격 판정 임계값 — 대미지가 최대체력의 이 비율 이상이면 강피격")]
        [SerializeField] private float _heavyHitFraction = 0.15f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int DashTriggerHash = Animator.StringToHash("DashTrigger");
        private static readonly int DashXHash = Animator.StringToHash("DashX");
        private static readonly int DashZHash = Animator.StringToHash("DashZ");
        private static readonly int FireTypeHash = Animator.StringToHash("FireType");
        private static readonly int HitHeavyTriggerHash = Animator.StringToHash("HitHeavyTrigger");
        private static readonly int VerticalYHash = Animator.StringToHash("VerticalY");
        private static readonly int PoseTypeHash = Animator.StringToHash("PoseType");
        private static readonly int FireMotionHash = Animator.StringToHash("FireMotion");
        private static readonly int FireMotionTriggerHash = Animator.StringToHash("FireMotionTrigger");

        private MechaController _controller;
        private float _fireUntil;
        private int _fireGroup;
        private float _poseUntil;
        private int _poseType;
        private int _fireMotion;
        private bool _fireMotionPending;
        private bool _hitPending;
        private bool _heavyHitPending;
        private bool _wasDashing;

        /// <summary>
        /// 전신 무기 포즈 유지창이 열려 있는가 — MechaVisuals가 요 고정 판단에 읽는다.
        /// </summary>
        public bool IsWeaponPoseActive => MechaAnimParams.IsFireActive(Time.time, _poseUntil);

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
            float now = Time.time;
            int incoming = MechaAnimParams.GetFireGroup(evt.WeaponId);
            bool windowActive = MechaAnimParams.IsFireActive(now, _fireUntil);
            _fireGroup = MechaAnimParams.ResolveFireGroup(_fireGroup, incoming, windowActive);
            _fireUntil = MechaAnimParams.ExtendFireWindow(now, _fireWindow);

            // 전신 포즈 무기만 포즈 유지창을 갱신한다 — None 무기는 창을 늘리지도 줄이지도 않는다.
            // 포즈 코드는 발사 측이 실어 보낸다 (파지 방식 × 장착 손 — Docs/05 §10-B10).
            int incomingPose = evt.PoseType;
            if (incomingPose != MechaAnimParams.FirePoseNone)
            {
                bool poseWindowActive = MechaAnimParams.IsFireActive(now, _poseUntil);
                _poseType = MechaAnimParams.ResolveFirePose(_poseType, incomingPose, poseWindowActive);
                _poseUntil = MechaAnimParams.ExtendFireWindow(now, _poseHoldSeconds);
            }

            // 발사 모션 — 한 발마다 재생되는 단발 동작 (Docs/05 §10-B11).
            // 경합 규칙(승격 유지)을 쓰지 않는다: 트리거는 순간값이라 "방금 쏜 무기"가 곧 정답이다.
            int motion = MechaAnimParams.GetFireMotion(evt.WeaponId);
            if (motion != MechaAnimParams.FireMotionNone)
            {
                _fireMotion = motion;
                _fireMotionPending = true;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Damage=0은 체력 갱신 알림(업그레이드 등) — 피격 연출 대상 아님
            if (evt.Damage <= 0f)
            {
                return;
            }

            if (MechaAnimParams.IsHeavyHit(evt.Damage, evt.MaxHealth, _heavyHitFraction))
            {
                _heavyHitPending = true;
            }
            else
            {
                _hitPending = true;
            }
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
            _animator.SetFloat(VerticalYHash,
                MechaAnimParams.ComputeVerticalLean(_controller.VerticalInput, _controller.IsGrounded),
                _dampTime, dt);
            _animator.SetBool(FireHash, MechaAnimParams.IsFireActive(Time.time, _fireUntil));
            _animator.SetInteger(FireTypeHash, _fireGroup);

            bool poseActive = MechaAnimParams.IsFireActive(Time.time, _poseUntil);
            if (!poseActive)
            {
                _poseType = MechaAnimParams.FirePoseNone;
            }

            _animator.SetInteger(PoseTypeHash, _poseType);

            // 발사 모션 트리거 — 상태 진입 조건이 PoseType·FireMotion 을 같이 보므로
            // 반드시 두 정수를 먼저 쓴 뒤 트리거를 올린다 (순서가 바뀌면 한 프레임 놓친다).
            if (_fireMotionPending)
            {
                _fireMotionPending = false;
                _animator.SetInteger(FireMotionHash, _fireMotion);
                _animator.SetTrigger(FireMotionTriggerHash);
            }

            if (_heavyHitPending)
            {
                _heavyHitPending = false;
                _hitPending = false;   // 같은 프레임 경합 시 강피격 우선
                _animator.SetTrigger(HitHeavyTriggerHash);
            }
            else if (_hitPending)
            {
                _hitPending = false;
                _animator.SetTrigger(HitTriggerHash);
            }

            // 대쉬 상승 엣지 — 방향은 스냅 공급 (댐핑 없음: 0.35초 순간 연출)
            bool dashing = _controller.IsDashing;
            if (dashing && !_wasDashing)
            {
                Vector2 dashDir = MechaAnimParams.ComputeDashDirection(_controller.Velocity, forward);
                _animator.SetFloat(DashXHash, dashDir.x);
                _animator.SetFloat(DashZHash, dashDir.y);
                _animator.SetTrigger(DashTriggerHash);
            }

            _wasDashing = dashing;
        }
    }
}
