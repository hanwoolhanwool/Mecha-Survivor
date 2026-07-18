using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>업적 판정(Evaluator)과 달성 기록(Log)의 검증.</summary>
    public sealed class AchievementTests
    {
        private static AchievementData MakeDef(AchievementMetric metric, float threshold,
            string weaponId = "")
        {
            var data = ScriptableObject.CreateInstance<AchievementData>();
            data.Id = "test";
            data.Metric = metric;
            data.Threshold = threshold;
            data.WeaponId = weaponId;
            return data;
        }

        [Test]
        public void SurvivalSeconds_ComparesRunDuration()
        {
            var run = new RunStatistics();
            run.RecordRunEnd(victory: false, durationSeconds: 600f);

            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.SurvivalSeconds, 600f), run, null), Is.True);
            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.SurvivalSeconds, 601f), run, null), Is.False);
        }

        [Test]
        public void KillsInRun_And_ReachLevel()
        {
            var run = new RunStatistics();
            for (int i = 0; i < 100; i++)
            {
                run.RecordKill();
            }

            run.RecordLevel(8);

            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.KillsInRun, 100f), run, null), Is.True);
            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.ReachLevel, 9f), run, null), Is.False);
        }

        [Test]
        public void ClearRun_RequiresVictory()
        {
            var lost = new RunStatistics();
            lost.RecordRunEnd(false, 100f);
            var won = new RunStatistics();
            won.RecordRunEnd(true, 100f);

            AchievementData def = MakeDef(AchievementMetric.ClearRun, 0f);
            Assert.That(AchievementEvaluator.IsSatisfied(def, lost, null), Is.False);
            Assert.That(AchievementEvaluator.IsSatisfied(def, won, null), Is.True);
        }

        [Test]
        public void TotalKills_ReadsLifetimeRecords()
        {
            var records = new PlayerRecords(new InMemoryKeyValueStore());
            records.RecordRun(false, 60f, 900);
            records.RecordRun(false, 60f, 200);
            var run = new RunStatistics();

            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.TotalKills, 1000f), run, records), Is.True);
            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.TotalKills, 1200f), run, records), Is.False);
        }

        [Test]
        public void WeaponKillsInRun_MatchesWeaponId()
        {
            var run = new RunStatistics();
            for (int i = 0; i < 30; i++)
            {
                run.RecordDamage("railgun", 100f, killedTarget: true);
            }

            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.WeaponKillsInRun, 30f, "railgun"), run, null), Is.True);
            Assert.That(AchievementEvaluator.IsSatisfied(
                MakeDef(AchievementMetric.WeaponKillsInRun, 30f, "beam"), run, null), Is.False);
        }

        [Test]
        public void Log_UnlocksOnce_AndPersists()
        {
            var store = new InMemoryKeyValueStore();
            var log = new AchievementLog(store);

            Assert.That(log.Unlock("survive_10m"), Is.True);
            Assert.That(log.Unlock("survive_10m"), Is.False, "중복 달성은 false");
            Assert.That(log.IsUnlocked("survive_10m"), Is.True);

            var reloaded = new AchievementLog(store);
            Assert.That(reloaded.IsUnlocked("survive_10m"), Is.True);
            Assert.That(reloaded.UnlockedCount, Is.EqualTo(1));
        }
    }
}
