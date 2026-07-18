using System.Collections.Generic;
using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>적 도감의 발견 판정·누적·영속화 검증 (InMemory 저장소).</summary>
    public sealed class EnemyCodexTests
    {
        [Test]
        public void Fresh_NothingDiscovered()
        {
            var codex = new EnemyCodex(new InMemoryKeyValueStore());

            Assert.That(codex.IsDiscovered("walker"), Is.False);
            Assert.That(codex.GetKills("walker"), Is.Zero);
            Assert.That(codex.KnownIds.Count, Is.Zero);
        }

        [Test]
        public void RecordKills_DiscoversAndAccumulates()
        {
            var codex = new EnemyCodex(new InMemoryKeyValueStore());

            codex.RecordKills(new Dictionary<string, int> { ["walker"] = 10, ["drone"] = 3 });
            codex.RecordKills(new Dictionary<string, int> { ["walker"] = 5 });

            Assert.That(codex.IsDiscovered("walker"), Is.True);
            Assert.That(codex.GetKills("walker"), Is.EqualTo(15));
            Assert.That(codex.GetKills("drone"), Is.EqualTo(3));
            Assert.That(codex.KnownIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void RecordKills_IgnoresEmptyAndNonPositive()
        {
            var codex = new EnemyCodex(new InMemoryKeyValueStore());

            codex.RecordKills(new Dictionary<string, int> { [""] = 5, ["turret"] = 0 });

            Assert.That(codex.KnownIds.Count, Is.Zero);
            Assert.That(codex.IsDiscovered("turret"), Is.False);
        }

        [Test]
        public void Kills_PersistAcrossInstances_ThroughSharedStore()
        {
            var store = new InMemoryKeyValueStore();
            var first = new EnemyCodex(store);
            first.RecordKills(new Dictionary<string, int> { ["boss"] = 1 });

            var second = new EnemyCodex(store);

            Assert.That(second.IsDiscovered("boss"), Is.True);
            Assert.That(second.GetKills("boss"), Is.EqualTo(1));
        }
    }
}
