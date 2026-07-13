using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>서포트 드론 등 뒤 호(arc) 대형 검증.</summary>
    public sealed class DroneFormationTests
    {
        [Test]
        public void SingleDrone_SitsDirectlyBehind()
        {
            Vector3 offset = DroneFormation.LocalOffset(0, 1, 2.8f, 1.8f, 140f);

            Assert.Less(offset.z, 0f, "등 뒤(-Z)에 있어야 한다.");
            Assert.AreEqual(0f, offset.x, 1e-3f, "혼자면 정중앙 뒤.");
            Assert.AreEqual(1.8f, offset.y, 1e-4f);
        }

        [Test]
        public void AllDrones_StayBehindWithinArc()
        {
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = DroneFormation.LocalOffset(i, count, 2.8f, 1.8f, 140f);
                Assert.Less(offset.z, 0f, $"드론 {i}는 앞으로 나오면 안 된다 (시야 방해 금지).");
            }
        }

        [Test]
        public void Formation_IsSymmetric()
        {
            Vector3 left = DroneFormation.LocalOffset(0, 3, 2.8f, 1.8f, 140f);
            Vector3 right = DroneFormation.LocalOffset(2, 3, 2.8f, 1.8f, 140f);

            Assert.AreEqual(-left.x, right.x, 1e-3f, "좌우 대칭이어야 한다.");
            Assert.AreEqual(left.z, right.z, 1e-3f);
        }

        [Test]
        public void AllOffsets_KeepConfiguredRadius()
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = DroneFormation.LocalOffset(i, 4, 2.8f, 1.8f, 140f);
                var planar = new Vector2(offset.x, offset.z);
                Assert.AreEqual(2.8f, planar.magnitude, 1e-3f);
            }
        }
    }
}
