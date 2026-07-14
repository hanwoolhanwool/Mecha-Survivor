using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>궤도 폭격 마커 배치 순수 계산 — EditMode 테스트 대상.</summary>
    public static class OrbitalMath
    {
        /// <summary>
        /// 융단 폭격 마커 위치 (GDD 3.4-8 Lv.5 — 마커 5개 연속).
        /// 조준점에서 시작해 진행 방향으로 spacing 간격의 일직선 — "쓸고 지나가는" 폭격선.
        /// forward는 수평 성분만 사용한다 (마커는 지면 위의 점이므로).
        /// </summary>
        public static Vector3 MarkerPosition(Vector3 center, Vector3 forward, int index, float spacing)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            return center + forward * (spacing * index);
        }
    }
}
