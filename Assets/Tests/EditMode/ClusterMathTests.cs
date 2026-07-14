using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>클러스터 폭탄 분열 방향 검증.</summary>
    public sealed class ClusterMathTests
    {
        [Test]
        public void SplitDirection_AlwaysUnitLength()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 dir = ClusterMath.SplitDirection(i, 6, 50f);
                Assert.AreEqual(1f, dir.magnitude, 1e-4f, "분열 방향은 단위 벡터여야 한다.");
            }
        }

        [Test]
        public void SplitDirection_AlwaysPointsDownward()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = ClusterMath.SplitDirection(i, 8, 60f);
                Assert.Less(dir.y, 0f, "새끼 폭탄은 아래로 떨어져야 융단폭격이 성립한다.");
            }
        }

        [Test]
        public void SplitDirection_ConeAngle_MatchesTiltFromVertical()
        {
            Vector3 dir = ClusterMath.SplitDirection(0, 4, 45f);
            Assert.AreEqual(45f, Vector3.Angle(Vector3.down, dir), 1e-3f,
                "연직 아래에서 원뿔 각도만큼 기울어야 한다.");
        }

        [Test]
        public void SplitDirection_DistributesAzimuthsEvenly()
        {
            Vector3 a = ClusterMath.SplitDirection(0, 4, 50f);
            Vector3 b = ClusterMath.SplitDirection(1, 4, 50f);
            Vector3 c = ClusterMath.SplitDirection(2, 4, 50f);

            Vector3 aFlat = new(a.x, 0f, a.z);
            Vector3 bFlat = new(b.x, 0f, b.z);
            Vector3 cFlat = new(c.x, 0f, c.z);

            Assert.AreEqual(90f, Vector3.Angle(aFlat, bFlat), 1e-2f,
                "4갈래면 수평 방위각이 90도 간격이어야 넓은 지역을 덮는다.");
            Assert.AreEqual(180f, Vector3.Angle(aFlat, cFlat), 1e-2f);
        }

        [Test]
        public void SplitDirection_ZeroCone_IsStraightDown()
        {
            Vector3 dir = ClusterMath.SplitDirection(0, 5, 0f);
            Assert.Less(Vector3.Angle(Vector3.down, dir), 1e-3f);
        }
    }
}
