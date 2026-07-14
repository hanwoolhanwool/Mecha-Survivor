using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>클러스터 폭탄 분열 방향 순수 계산 — EditMode 테스트 대상.</summary>
    public static class ClusterMath
    {
        /// <summary>
        /// 새끼 폭탄 분열 방향. 연직 아래를 중심으로 coneAngle만큼 기울인 뒤
        /// 방위각을 균등 분배한다 — 융단폭격의 "넓은 지역"을 만든다 (GDD 3.4-5).
        /// 반환은 항상 단위 벡터이며 아래(-Y) 성분을 가진다.
        /// </summary>
        public static Vector3 SplitDirection(int index, int count, float coneAngleDegrees)
        {
            coneAngleDegrees = Mathf.Clamp(coneAngleDegrees, 0f, 89f);
            float azimuthDegrees = 360f * index / Mathf.Max(1, count);

            float cone = coneAngleDegrees * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;

            float horizontal = Mathf.Sin(cone);
            return new Vector3(
                horizontal * Mathf.Cos(azimuth),
                -Mathf.Cos(cone),
                horizontal * Mathf.Sin(azimuth));
        }
    }
}
