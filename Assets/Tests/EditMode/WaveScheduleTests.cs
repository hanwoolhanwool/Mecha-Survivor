using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>스폰 규칙 활성 시간 창 판정 검증.</summary>
    public sealed class WaveScheduleTests
    {
        private static WaveData.Spawn Rule(float start, float end) =>
            new() { StartTime = start, EndTime = end, SpawnInterval = 1f, MaxAlive = 10 };

        [Test]
        public void BeforeStart_Inactive()
        {
            Assert.IsFalse(WaveData.IsActiveAt(Rule(120f, 300f), 60f));
        }

        [Test]
        public void InsideWindow_Active()
        {
            Assert.IsTrue(WaveData.IsActiveAt(Rule(120f, 300f), 200f));
        }

        [Test]
        public void AfterEnd_Inactive()
        {
            Assert.IsFalse(WaveData.IsActiveAt(Rule(120f, 300f), 300f));
        }

        [Test]
        public void ZeroEndTime_ActiveForever()
        {
            Assert.IsTrue(WaveData.IsActiveAt(Rule(120f, 0f), 9999f));
        }

        [Test]
        public void StartBoundary_Active()
        {
            Assert.IsTrue(WaveData.IsActiveAt(Rule(120f, 300f), 120f));
        }
    }
}
