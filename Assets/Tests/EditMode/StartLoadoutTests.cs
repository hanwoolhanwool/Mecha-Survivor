using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>출격 로드아웃 선택 상태의 기본값·영속화 검증 (InMemory 저장소).</summary>
    public sealed class StartLoadoutTests
    {
        [Test]
        public void Default_IsNoSelection()
        {
            var loadout = new StartLoadout(new InMemoryKeyValueStore());

            Assert.That(loadout.HasSelection, Is.False);
            Assert.That(loadout.SelectedId, Is.Empty);
        }

        [Test]
        public void Selection_PersistsAcrossInstances()
        {
            var store = new InMemoryKeyValueStore();
            var first = new StartLoadout(store) { SelectedId = "loadout_railgun" };

            var second = new StartLoadout(store);

            Assert.That(second.HasSelection, Is.True);
            Assert.That(second.SelectedId, Is.EqualTo("loadout_railgun"));
            Assert.That(first.SelectedId, Is.EqualTo(second.SelectedId));
        }

        [Test]
        public void Null_ClearsSelection()
        {
            var store = new InMemoryKeyValueStore();
            var loadout = new StartLoadout(store) { SelectedId = "loadout_beam" };

            loadout.SelectedId = null;

            Assert.That(loadout.HasSelection, Is.False);
            Assert.That(new StartLoadout(store).HasSelection, Is.False);
        }
    }
}
