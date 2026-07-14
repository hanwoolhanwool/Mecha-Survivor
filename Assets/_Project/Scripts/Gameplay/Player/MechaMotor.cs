using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 이동 계산 순수 로직. MonoBehaviour 밖에 두어 EditMode 테스트로 검증한다.
    /// 관성·가속 램프 없음 — 입력이 곧 속도다 (GDD 2.3 절대 원칙).
    /// </summary>
    public static class MechaMotor
    {
        /// <summary>
        /// 입력 → 즉시 속도. 수평은 카메라 요(yaw) 기준, 수직은 상승/하강 속도 분리.
        /// </summary>
        public static Vector3 ComputeVelocity(
            Vector2 move, float vertical, float yawDegrees,
            float horizontalSpeed, float ascendSpeed, float descendSpeed)
        {
            move = Vector2.ClampMagnitude(move, 1f);
            Vector3 planar = Quaternion.Euler(0f, yawDegrees, 0f)
                             * new Vector3(move.x, 0f, move.y)
                             * horizontalSpeed;

            vertical = Mathf.Clamp(vertical, -1f, 1f);
            float vy = vertical >= 0f ? vertical * ascendSpeed : vertical * descendSpeed;

            return new Vector3(planar.x, vy, planar.z);
        }

        /// <summary>
        /// 외부 임펄스(산탄 캐논 반동 등)의 지수 감쇠. 관성 0 원칙(GDD 2.3)의 예외지만
        /// 짧게 감쇠해 "밀려남"만 남기고 조작감을 되돌려준다. 미세 잔량은 0으로 스냅.
        /// </summary>
        public static Vector3 DecayImpulse(Vector3 impulse, float dampingPerSecond, float deltaTime)
        {
            float k = Mathf.Exp(-Mathf.Max(0f, dampingPerSecond) * Mathf.Max(0f, deltaTime));
            Vector3 next = impulse * k;
            return next.sqrMagnitude < 0.01f ? Vector3.zero : next;
        }

        /// <summary>
        /// 고도 상한(Ceiling) 클램프. 이번 프레임 이동으로 천장을 넘지 않도록
        /// 상승 속도를 줄여 반환한다. 하강/정지는 그대로 통과.
        /// (GDD 2.2 — 위로 무한정 도망칠 수 없어야 근접 적이 성립한다)
        /// </summary>
        public static float ClampAscent(float currentY, float verticalVelocity, float deltaTime, float ceiling)
        {
            if (verticalVelocity <= 0f)
            {
                return verticalVelocity;
            }

            float headroom = Mathf.Max(0f, ceiling - currentY);
            float desired = verticalVelocity * deltaTime;
            if (desired <= headroom)
            {
                return verticalVelocity;
            }

            return headroom / Mathf.Max(deltaTime, 0.0001f);
        }
    }
}
