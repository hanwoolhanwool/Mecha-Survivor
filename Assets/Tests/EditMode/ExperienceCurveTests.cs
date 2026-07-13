using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>레벨업 요구 경험치 곡선 검증.</summary>
    public sealed class ExperienceCurveTests
    {
        [Test]
        public void Level1_RequiresBaseAmount()
        {
            Assert.AreEqual(5, ExperienceCurve.RequiredFor(1, 5, 4));
        }

        [Test]
        public void RequirementGrowsLinearly()
        {
            Assert.AreEqual(9, ExperienceCurve.RequiredFor(2, 5, 4));
            Assert.AreEqual(13, ExperienceCurve.RequiredFor(3, 5, 4));
        }

        [Test]
        public void RequirementIsMonotonicallyIncreasing()
        {
            int previous = 0;
            for (int level = 1; level <= 50; level++)
            {
                int required = ExperienceCurve.RequiredFor(level, 5, 4);
                Assert.Greater(required, previous, $"레벨 {level} 요구치는 증가해야 한다.");
                previous = required;
            }
        }
    }
}
