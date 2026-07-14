using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>히트스캔 부채꼴 확산·관통 정렬 검증 (레이저 캐논·레일건).</summary>
    public sealed class HitscanMathTests
    {
        [Test]
        public void FanDirection_SingleLine_IsForward()
        {
            Vector3 dir = HitscanMath.FanDirection(Vector3.forward, Vector3.up, 0, 1, 14f);
            Assert.Less(Vector3.Angle(Vector3.forward, dir), 1e-3f,
                "라인이 1갈래면 조준선 그대로여야 한다.");
        }

        [Test]
        public void FanDirection_ThreeLines_MiddleIsForward()
        {
            Vector3 dir = HitscanMath.FanDirection(Vector3.forward, Vector3.up, 1, 3, 20f);
            Assert.Less(Vector3.Angle(Vector3.forward, dir), 1e-3f,
                "홀수 갈래의 가운데 라인은 조준선과 일치해야 한다.");
        }

        [Test]
        public void FanDirection_EdgeLines_SpreadSymmetrically()
        {
            Vector3 left = HitscanMath.FanDirection(Vector3.forward, Vector3.up, 0, 3, 20f);
            Vector3 right = HitscanMath.FanDirection(Vector3.forward, Vector3.up, 2, 3, 20f);

            Assert.AreEqual(10f, Vector3.Angle(Vector3.forward, left), 1e-3f,
                "양 끝 라인은 전체 각도의 절반만큼 벌어져야 한다.");
            Assert.AreEqual(10f, Vector3.Angle(Vector3.forward, right), 1e-3f);
            Assert.AreEqual(20f, Vector3.Angle(left, right), 1e-3f,
                "양 끝 라인 사이가 전체 확산 각도여야 한다.");
        }

        [Test]
        public void FanDirection_AlwaysUnitLength()
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = HitscanMath.FanDirection(new Vector3(1f, 2f, 3f), Vector3.up, i, 4, 30f);
                Assert.AreEqual(1f, dir.magnitude, 1e-4f, "확산 방향은 단위 벡터여야 한다.");
            }
        }

        [Test]
        public void SortIndicesByDistance_OrdersAscending()
        {
            float[] distances = { 30f, 5f, 18f, 2f };
            int[] indices = new int[4];

            HitscanMath.SortIndicesByDistance(distances, 4, indices);

            Assert.AreEqual(3, indices[0], "가장 가까운 대상이 첫 번째여야 관통 순서가 성립한다.");
            Assert.AreEqual(1, indices[1]);
            Assert.AreEqual(2, indices[2]);
            Assert.AreEqual(0, indices[3]);
        }

        [Test]
        public void SortIndicesByDistance_PartialCount_IgnoresRest()
        {
            float[] distances = { 9f, 1f, 0.5f, 100f };
            int[] indices = new int[4];

            HitscanMath.SortIndicesByDistance(distances, 2, indices);

            Assert.AreEqual(1, indices[0], "count 밖의 요소는 정렬에 참여하면 안 된다.");
            Assert.AreEqual(0, indices[1]);
        }
    }
}
