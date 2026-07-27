using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// Animator 파라미터 순수 계산 — MechaAnimationDriver에서 분리해 EditMode로 검증한다.
    /// 월드 속도를 시각 전방(yaw) 기준 로컬 좌표로 변환·정규화하는 것이 핵심.
    /// </summary>
    public static class MechaAnimParams
    {
        /// <summary>
        /// 월드 속도의 수평 성분을 시각 전방 기준 로컬 (MoveX, MoveZ)로 변환한다.
        /// 기준속도로 나눠 정규화하고 단위원 안으로 클램프한다 (대시/임펄스 초과분 컷).
        /// visualForward는 시각 루트의 forward — 기울어져 있어도 수평 투영으로 요만 취한다.
        /// </summary>
        public static Vector2 ComputeMove(Vector3 worldVelocity, Vector3 visualForward, float referenceSpeed)
        {
            Vector2 forward = new(visualForward.x, visualForward.z);
            if (referenceSpeed <= 0f || forward.sqrMagnitude < 1e-6f)
            {
                return Vector2.zero;
            }

            forward.Normalize();
            Vector2 right = new(forward.y, -forward.x);
            Vector2 planar = new(worldVelocity.x, worldVelocity.z);

            Vector2 move = new(
                Vector2.Dot(planar, right) / referenceSpeed,
                Vector2.Dot(planar, forward) / referenceSpeed);
            return Vector2.ClampMagnitude(move, 1f);
        }

        /// <summary>
        /// 수평 속력 / 기준속도 — Ground 상태의 재생속도 배율(speedParameter)로 쓰인다.
        /// 하한 1: 정지 시에도 Idle이 정상 재생돼야 하므로 1 밑으로 내리지 않는다
        /// (저속 이동의 발 미끄러짐은 QA 허용 범위). 대시 중엔 1을 넘는다.
        /// </summary>
        public static float ComputeSpeed(Vector3 worldVelocity, float referenceSpeed)
        {
            if (referenceSpeed <= 0f)
            {
                return 1f;
            }

            Vector2 planar = new(worldVelocity.x, worldVelocity.z);
            return Mathf.Max(1f, planar.magnitude / referenceSpeed);
        }
    }
}
