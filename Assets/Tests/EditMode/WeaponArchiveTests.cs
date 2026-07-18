using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>무기별 평생 아카이브의 누적·최고 기록·영속화 검증 (InMemory 저장소).</summary>
    public sealed class WeaponArchiveTests
    {
        private static RunStatistics MakeRun(string weaponId, float damage, int shots, bool kill)
        {
            var stats = new RunStatistics();
            for (int i = 0; i < shots; i++)
            {
                stats.RecordShot(weaponId);
            }

            stats.RecordDamage(weaponId, damage, kill);
            return stats;
        }

        [Test]
        public void RecordRun_AccumulatesAcrossRuns()
        {
            var archive = new WeaponArchive(new InMemoryKeyValueStore());

            archive.RecordRun(MakeRun("gatling", 100f, 10, true).Weapons);
            archive.RecordRun(MakeRun("gatling", 50f, 5, false).Weapons);

            Assert.That(archive.TryGet("gatling", out WeaponArchive.Entry entry), Is.True);
            Assert.That(entry.TotalDamage, Is.EqualTo(150f));
            Assert.That(entry.Shots, Is.EqualTo(15));
            Assert.That(entry.Kills, Is.EqualTo(1));
            Assert.That(entry.RunsUsed, Is.EqualTo(2));
        }

        [Test]
        public void BestSingleHit_KeepsMaximum()
        {
            var archive = new WeaponArchive(new InMemoryKeyValueStore());

            archive.RecordRun(MakeRun("railgun", 300f, 1, true).Weapons);
            archive.RecordRun(MakeRun("railgun", 120f, 1, true).Weapons);

            archive.TryGet("railgun", out WeaponArchive.Entry entry);
            Assert.That(entry.BestSingleHit, Is.EqualTo(300f));
        }

        [Test]
        public void HasUsed_RequiresAtLeastOneShot()
        {
            var archive = new WeaponArchive(new InMemoryKeyValueStore());
            var stats = new RunStatistics();
            stats.RecordDamage("beam", 10f, false); // 발사 기록 없이 데미지만

            archive.RecordRun(stats.Weapons);

            Assert.That(archive.HasUsed("beam"), Is.False);
            Assert.That(archive.HasUsed("unknown"), Is.False);
        }

        [Test]
        public void Entries_PersistAcrossInstances_ThroughSharedStore()
        {
            var store = new InMemoryKeyValueStore();
            var first = new WeaponArchive(store);
            first.RecordRun(MakeRun("missile_pod", 500f, 20, true).Weapons);

            var second = new WeaponArchive(store);

            Assert.That(second.TryGet("missile_pod", out WeaponArchive.Entry entry), Is.True);
            Assert.That(entry.TotalDamage, Is.EqualTo(500f));
            Assert.That(entry.Shots, Is.EqualTo(20));
            Assert.That(second.HasUsed("missile_pod"), Is.True);
        }
    }
}
