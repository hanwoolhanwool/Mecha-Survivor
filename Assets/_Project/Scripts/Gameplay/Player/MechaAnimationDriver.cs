using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 표현 전담: MechaController의 상태를 읽어 Animator 파라미터로 변환한다.
    /// MechaVisuals와 같은 계약 — 게임플레이에 영향 0, 읽기만 한다.
    /// 이동은 MechaController가 하므로 루트 모션은 쓰지 않는다.
    /// </summary>
    public sealed class MechaAnimationDriver : MonoBehaviour
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");

        [Header("참조 (읽기 전용)")]
        [SerializeField] private MechaController _controller;

        [Tooltip("리깅된 모델의 Animator. 비워두면 자식에서 찾는다")]
        [SerializeField] private Animator _animator;

        [Header("파라미터 반응")]
        [Tooltip("Speed 파라미터가 목표값을 따라가는 응답 속도")]
        [SerializeField] private float _speedResponse = 8f;

        private float _smoothedSpeed;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        private void LateUpdate()
        {
            if (_controller == null || _animator == null)
            {
                return;
            }

            float target = NormalizedSpeed(_controller.Velocity, _controller.HorizontalSpeed);
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, target,
                1f - Mathf.Exp(-_speedResponse * Time.deltaTime));

            _animator.SetFloat(SpeedParam, _smoothedSpeed);
            _animator.SetBool(GroundedParam, _controller.IsGrounded);
        }

        /// <summary>
        /// 수평 속도를 블렌드 트리 입력(0=idle, 1=최고 보행 속도)으로 정규화한다.
        /// 대시 등으로 기준 속도를 넘으면 1로 클램프.
        /// </summary>
        public static float NormalizedSpeed(Vector3 velocity, float speedReference)
        {
            if (speedReference <= 0f)
            {
                return 0f;
            }

            float planar = new Vector2(velocity.x, velocity.z).magnitude;
            return Mathf.Clamp01(planar / speedReference);
        }
    }
}
