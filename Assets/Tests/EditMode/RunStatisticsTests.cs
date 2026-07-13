using NUnit.Framework;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>런 통계 집계 검증 — 무기별 기여도가 이 화면의 핵심이다 (GDD 6장).</summary>
    public sealed class RunStatisticsTests
    {
        [Test]
        public void RecordDamage_AggregatesPerWeapon()
        {
            var stats = new RunStatistics();
            stats.RecordDamage("gatling", 5f, false);
            stats.RecordDamage("gatling", 5f, false);
            stats.RecordDamage("missile_pod", 30f, true);

            Assert.AreEqual(2, stats.Weapons.Count);
            Assert.AreEqual(40f, stats.TotalDamage, 1e-4f);

            RunStatistics.WeaponStat gatling = stats.Weapons[0];
            Assert.AreEqual("gatling", gatling.WeaponId);
            Assert.AreEqual(10f, gatling.TotalDamage, 1e-4f);
            Assert.AreEqual(0, gatling.Kills);

            RunStatistics.WeaponStat missile = stats.Weapons[1];
            Assert.AreEqual(1, missile.Kills, "처치는 막타 무기에 귀속된다.");
        }

        [Test]
        public void NullSourceDamage_IsIgnored()
        {
            var stats = new RunStatistics();
            stats.RecordDamage(null, 100f, true);

            Assert.AreEqual(0, stats.Weapons.Count, "출처 없는 데미지(적 탄 등)는 집계하지 않는다.");
            Assert.AreEqual(0f, stats.TotalDamage, 1e-4f);
        }

        [Test]
        public void MaxSingleHit_TracksWeaponAndValue()
        {
            var stats = new RunStatistics();
            stats.RecordDamage("gatling", 5f, false);
            stats.RecordDamage("beam", 80f, false);
            stats.RecordDamage("gatling", 7f, false);

            Assert.AreEqual(80f, stats.MaxSingleHit, 1e-4f);
            Assert.AreEqual("beam", stats.MaxSingleHitWeaponId);
        }

        [Test]
        public void UptimeSamples_ComputeDowntimeRatio()
        {
            var stats = new RunStatistics();
            stats.RecordUptimeSample("beam", true);
            stats.RecordUptimeSample("beam", false);
            stats.RecordUptimeSample("beam", false);
            stats.RecordUptimeSample("beam", false);

            Assert.AreEqual(0.75f, stats.Weapons[0].DowntimeRatio, 1e-4f,
                "쿨 대기 비율 = 준비 안 된 샘플 / 전체 샘플.");
        }

        [Test]
        public void AltitudeSamples_ComputeGroundedRatio()
        {
            var stats = new RunStatistics();
            stats.RecordAltitudeSample(true);
            stats.RecordAltitudeSample(false);
            stats.RecordAltitudeSample(false);
            stats.RecordAltitudeSample(false);

            Assert.AreEqual(0.25f, stats.GroundedRatio, 1e-4f);
        }

        [Test]
        public void ShotsAndKillsAndRunEnd_AreRecorded()
        {
            var stats = new RunStatistics();
            stats.RecordShot("beam");
            stats.RecordShot("beam");
            stats.RecordKill();
            stats.RecordExperience(3);
            stats.RecordLevel(7);
            stats.RecordRunEnd(victory: true, durationSeconds: 1200f);

            Assert.AreEqual(2, stats.Weapons[0].ShotsFired);
            Assert.AreEqual(1, stats.TotalKills);
            Assert.AreEqual(3, stats.TotalExperience);
            Assert.AreEqual(7, stats.FinalLevel);
            Assert.IsTrue(stats.Victory);
            Assert.AreEqual(1200f, stats.DurationSeconds, 1e-4f);
        }
    }
}
