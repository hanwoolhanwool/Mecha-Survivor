using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>영구 전적의 누적·최고 기록 갱신·영속화 검증 (InMemory 저장소).</summary>
    public sealed class PlayerRecordsTests
    {
        [Test]
        public void FreshStore_HasNoRuns()
        {
            var records = new PlayerRecords(new InMemoryKeyValueStore());

            Assert.That(records.HasAnyRun, Is.False);
            Assert.That(records.TotalRuns, Is.Zero);
            Assert.That(records.TotalKills, Is.Zero);
            Assert.That(records.TotalVictories, Is.Zero);
            Assert.That(records.BestSurvivalSeconds, Is.Zero);
        }

        [Test]
        public void RecordRun_AccumulatesTotals()
        {
            var records = new PlayerRecords(new InMemoryKeyValueStore());

            records.RecordRun(victory: false, durationSeconds: 120f, kills: 300);
            records.RecordRun(victory: true, durationSeconds: 900f, kills: 1200);

            Assert.That(records.TotalRuns, Is.EqualTo(2));
            Assert.That(records.TotalKills, Is.EqualTo(1500));
            Assert.That(records.TotalVictories, Is.EqualTo(1));
            Assert.That(records.HasAnyRun, Is.True);
        }

        [Test]
        public void BestSurvival_KeepsMaximumOnly()
        {
            var records = new PlayerRecords(new InMemoryKeyValueStore());

            records.RecordRun(false, 300f, 0);
            records.RecordRun(false, 150f, 0);
            Assert.That(records.BestSurvivalSeconds, Is.EqualTo(300f));

            records.RecordRun(true, 450f, 0);
            Assert.That(records.BestSurvivalSeconds, Is.EqualTo(450f));
        }

        [Test]
        public void NegativeKills_DoNotCorruptTotals()
        {
            var records = new PlayerRecords(new InMemoryKeyValueStore());

            records.RecordRun(false, 60f, -50);

            Assert.That(records.TotalKills, Is.Zero);
            Assert.That(records.TotalRuns, Is.EqualTo(1));
        }

        [Test]
        public void Records_PersistAcrossInstances_ThroughSharedStore()
        {
            var store = new InMemoryKeyValueStore();
            var first = new PlayerRecords(store);
            first.RecordRun(true, 720f, 850);

            var second = new PlayerRecords(store);

            Assert.That(second.TotalRuns, Is.EqualTo(1));
            Assert.That(second.TotalKills, Is.EqualTo(850));
            Assert.That(second.TotalVictories, Is.EqualTo(1));
            Assert.That(second.BestSurvivalSeconds, Is.EqualTo(720f));
        }
    }
}
