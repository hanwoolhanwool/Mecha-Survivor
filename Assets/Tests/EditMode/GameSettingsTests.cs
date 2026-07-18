using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>환경설정 값의 기본값·클램프·영속화 검증 (InMemory 저장소 — PlayerPrefs 비의존).</summary>
    public sealed class GameSettingsTests
    {
        [Test]
        public void Defaults_WhenStoreIsEmpty()
        {
            var settings = new GameSettings(new InMemoryKeyValueStore());

            Assert.That(settings.MasterVolume, Is.EqualTo(GameSettings.DefaultMasterVolume));
            Assert.That(settings.ScreenShake, Is.EqualTo(GameSettings.DefaultScreenShake));
            Assert.That(settings.Fullscreen, Is.True);
            Assert.That(settings.VSync, Is.True);
        }

        [Test]
        public void MasterVolume_IsClampedToUnitRange()
        {
            var settings = new GameSettings(new InMemoryKeyValueStore());

            settings.MasterVolume = 1.7f;
            Assert.That(settings.MasterVolume, Is.EqualTo(1f));

            settings.MasterVolume = -0.3f;
            Assert.That(settings.MasterVolume, Is.EqualTo(0f));
        }

        [Test]
        public void ScreenShake_IsClampedToUnitRange()
        {
            var settings = new GameSettings(new InMemoryKeyValueStore());

            settings.ScreenShake = 2f;
            Assert.That(settings.ScreenShake, Is.EqualTo(1f));

            settings.ScreenShake = -1f;
            Assert.That(settings.ScreenShake, Is.EqualTo(0f));
        }

        [Test]
        public void Values_PersistAcrossInstances_ThroughSharedStore()
        {
            var store = new InMemoryKeyValueStore();
            var first = new GameSettings(store)
            {
                MasterVolume = 0.35f,
                ScreenShake = 0.5f,
                Fullscreen = false,
                VSync = false,
            };

            var second = new GameSettings(store);

            Assert.That(second.MasterVolume, Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(second.ScreenShake, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(second.Fullscreen, Is.False);
            Assert.That(second.VSync, Is.False);
            Assert.That(first.MasterVolume, Is.EqualTo(second.MasterVolume));
        }

        [Test]
        public void CorruptedStoredVolume_IsClampedOnLoad()
        {
            var store = new InMemoryKeyValueStore();
            store.SetFloat("settings.master_volume", 5f);

            var settings = new GameSettings(store);

            Assert.That(settings.MasterVolume, Is.EqualTo(1f));
        }
    }
}
