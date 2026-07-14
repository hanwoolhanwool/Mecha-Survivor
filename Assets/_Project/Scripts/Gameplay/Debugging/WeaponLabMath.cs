using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>무기 실험실 표적 배치 순수 계산 — EditMode 테스트 대상.</summary>
    public static class WeaponLabMath
    {
        /// <summary>중심 둘레 링에 표적을 균등 배치한다 (수평면, index/count 방위각).</summary>
        public static Vector3 RingPosition(Vector3 center, float radius, int index, int count)
        {
            float angle = Mathf.PI * 2f * index / Mathf.Max(1, count);
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        /// <summary>목록 순환 인덱스 (음수 포함). 무기 이전/다음 전환에 쓴다.</summary>
        public static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
