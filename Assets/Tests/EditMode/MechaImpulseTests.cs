using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>산탄 캐논 반동 임펄스 감쇠 검증 (MechaMotor.DecayImpulse).</summary>
    public sealed class MechaImpulseTests
    {
        [Test]
        public void DecayImpulse_ShrinksOverTime()
        {
            Vector3 impulse = new(10f, 0f, 0f);
            Vector3 next = MechaMotor.DecayImpulse(impulse, 5f, 1f / 60f);

            Assert.Less(next.magnitude, impulse.magnitude, "임펄스는 매 프레임 줄어야 한다.");
            Assert.Greater(next.magnitude, 0f, "한 프레임 만에 사라지면 반동이 안 보인다.");
        }

        [Test]
        public void DecayImpulse_SnapsTinyResidualToZero()
        {
            Vector3 impulse = new(0.05f, 0f, 0f);
            Assert.AreEqual(Vector3.zero, MechaMotor.DecayImpulse(impulse, 5f, 1f / 60f),
                "미세 잔량은 0으로 스냅해야 접지 유지 판정(관성 0 원칙)이 복귀한다.");
        }

        [Test]
        public void DecayImpulse_ConvergesToZero()
        {
            Vector3 impulse = new(16f, 4f, -8f);
            for (int i = 0; i < 600; i++)
            {
                impulse = MechaMotor.DecayImpulse(impulse, 5f, 1f / 60f);
            }

            Assert.AreEqual(Vector3.zero, impulse, "반동은 유한 시간 안에 완전히 사라져야 한다.");
        }

        [Test]
        public void DecayImpulse_PreservesDirection()
        {
            Vector3 impulse = new(3f, 0f, 4f);
            Vector3 next = MechaMotor.DecayImpulse(impulse, 5f, 1f / 60f);
            Assert.Less(Vector3.Angle(impulse, next), 1e-3f, "감쇠는 방향을 바꾸면 안 된다.");
        }
    }
}
