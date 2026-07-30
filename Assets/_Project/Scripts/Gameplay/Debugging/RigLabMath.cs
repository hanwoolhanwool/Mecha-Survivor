using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>RigLab 조작의 순수 계산 — EditMode 테스트 대상 (Docs/06 §6).</summary>
    public static class RigLabMath
    {
        public const float MinPlaybackSpeed = 0.1f;
        public const float MaxPlaybackSpeed = 2f;

        /// <summary>
        /// 8방 순환 방향 (0=전방, 시계 방향). 애니메이션 8방 블렌드 확인용 (MoveX, MoveZ).
        /// </summary>
        public static Vector2 DirectionForIndex(int index)
        {
            int i = ((index % 8) + 8) % 8;
            float rad = i * Mathf.PI / 4f;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }

        /// <summary>재생 속도 가감 — 0.1×~2× 클램프 (Docs/06 §4.2-④).</summary>
        public static float StepPlaybackSpeed(float current, float delta)
        {
            return Mathf.Clamp(current + delta, MinPlaybackSpeed, MaxPlaybackSpeed);
        }

        /// <summary>목록 순환 인덱스 (음수 델타 안전).</summary>
        public static int CycleIndex(int current, int delta, int count)
        {
            return count <= 0 ? 0 : (((current + delta) % count) + count) % count;
        }
    }
}
