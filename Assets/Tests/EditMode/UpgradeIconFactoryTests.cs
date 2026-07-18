using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;
using MechaSurvivor.UI;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 절차 생성 업그레이드 아이콘 검증 — 생성/캐싱/Id별 고유성.
    /// </summary>
    public sealed class UpgradeIconFactoryTests
    {
        private static readonly string[] AllIds =
        {
            "part_gatling", "part_missile_pod", "part_beam", "part_gravity_well",
            "part_laser_cannon", "part_shotgun_cannon", "part_cluster_bomb", "part_railgun",
            "part_orbital_strike", "part_emp_field", "part_slot_expansion", "support_drones",
            "armor_heavy", "armor_composite",
            "energy_reactor", "energy_missile_loader", "energy_thrusters", "energy_landing_gear",
            "result_heated_gatling", "result_overload_shield", "result_multi_lock",
        };

        [Test]
        public void GetGenerated_ReturnsSpriteOfExpectedSize()
        {
            Sprite sprite = UpgradeIconFactory.GetGenerated("part_gatling", UpgradeCategory.Parts);

            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(UpgradeIconFactory.Size));
            Assert.That(sprite.texture.height, Is.EqualTo(UpgradeIconFactory.Size));
        }

        [Test]
        public void GetGenerated_SameId_ReturnsCachedInstance()
        {
            Sprite first = UpgradeIconFactory.GetGenerated("part_railgun", UpgradeCategory.Parts);
            Sprite second = UpgradeIconFactory.GetGenerated("part_railgun", UpgradeCategory.Parts);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetGenerated_EveryKnownId_DiffersFromFallbackGlyph()
        {
            Color32[] fallback = UpgradeIconFactory
                .GetGenerated("__unknown__", UpgradeCategory.Parts).texture.GetPixels32();

            foreach (string id in AllIds)
            {
                // 카테고리 색 차이가 아닌 글리프 차이를 보기 위해 같은 카테고리로 생성한다.
                Color32[] pixels = UpgradeIconFactory
                    .GetGenerated(id, UpgradeCategory.Parts).texture.GetPixels32();

                Assert.That(CountDifferingPixels(fallback, pixels), Is.GreaterThan(100),
                    $"'{id}' 글리프가 폴백과 구분되지 않는다 — switch 분기 누락 의심");
            }
        }

        [Test]
        public void GetGenerated_DifferentIds_ProduceDifferentPixels()
        {
            Color32[] a = UpgradeIconFactory
                .GetGenerated("part_gatling", UpgradeCategory.Parts).texture.GetPixels32();
            Color32[] b = UpgradeIconFactory
                .GetGenerated("part_gravity_well", UpgradeCategory.Parts).texture.GetPixels32();

            Assert.That(CountDifferingPixels(a, b), Is.GreaterThan(100));
        }

        private static int CountDifferingPixels(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
