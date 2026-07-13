using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 분리 스티어링 검증 — 적끼리 물리 충돌이 꺼져 있으므로(SETUP 5장)
    /// 겹침 방지는 전적으로 이 계산에 달려 있다.
    /// </summary>
    public sealed class SteeringTests
    {
        [Test]
        public void PairSeparation_PushesAwayFromNeighbor()
        {
            Vector3 push = Steering.PairSeparation(
                self: new Vector3(1f, 0f, 0f), other: Vector3.zero, radius: 2f);

            Assert.Greater(push.x, 0f, "이웃 반대 방향으로 밀어내야 한다.");
            Assert.AreEqual(0f, push.y, 1e-4f);
            Assert.AreEqual(0f, push.z, 1e-4f);
        }

        [Test]
        public void PairSeparation_OutsideRadius_IsZero()
        {
            Vector3 push = Steering.PairSeparation(
                self: new Vector3(5f, 0f, 0f), other: Vector3.zero, radius: 2f);

            Assert.AreEqual(Vector3.zero, push);
        }

        [Test]
        public void PairSeparation_CloserNeighbor_PushesHarder()
        {
            Vector3 near = Steering.PairSeparation(new Vector3(0.5f, 0f, 0f), Vector3.zero, 2f);
            Vector3 far = Steering.PairSeparation(new Vector3(1.5f, 0f, 0f), Vector3.zero, 2f);

            Assert.Greater(near.magnitude, far.magnitude, "가까울수록 강하게 밀어야 한다.");
        }

        [Test]
        public void PairSeparation_FullyOverlapped_StillPushes()
        {
            Vector3 push = Steering.PairSeparation(Vector3.zero, Vector3.zero, 2f);

            Assert.Greater(push.magnitude, 0f, "완전히 겹쳐도 밀어내야 한다 (0 나눗셈 방지).");
        }

        [Test]
        public void CombineChaseAndSeparation_ReturnsNormalized()
        {
            Vector3 result = Steering.CombineChaseAndSeparation(
                Vector3.forward, new Vector3(0.5f, 0f, 0f), 1f);

            Assert.AreEqual(1f, result.magnitude, 1e-3f);
        }

        [Test]
        public void CombineChaseAndSeparation_ZeroSeparation_KeepsChaseDirection()
        {
            Vector3 result = Steering.CombineChaseAndSeparation(Vector3.forward, Vector3.zero, 1f);

            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, result), 1e-3f);
        }
    }
}
