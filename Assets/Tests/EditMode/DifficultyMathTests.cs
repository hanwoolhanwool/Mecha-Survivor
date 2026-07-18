using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>난이도 배율 적용 계산 검증 — 간격 단축, 상한 확장, 무리 수 제한.</summary>
    public sealed class DifficultyMathTests
    {
        [Test]
        public void EffectiveInterval_RateTwo_HalvesInterval()
        {
            Assert.AreEqual(1.25f, DifficultyMath.EffectiveInterval(2.5f, 2f), 1e-4f);
        }

        [Test]
        public void EffectiveInterval_RateOne_Unchanged()
        {
            Assert.AreEqual(0.8f, DifficultyMath.EffectiveInterval(0.8f, 1f), 1e-4f);
        }

        [Test]
        public void EffectiveInterval_HugeRate_ClampedToMinInterval()
        {
            Assert.AreEqual(DifficultyMath.MinInterval,
                DifficultyMath.EffectiveInterval(0.5f, 1000f), 1e-4f);
        }

        [Test]
        public void EffectiveInterval_ZeroOrNegativeRate_DoesNotDivideByZero()
        {
            Assert.Greater(DifficultyMath.EffectiveInterval(1f, 0f), 1f);
            Assert.Greater(DifficultyMath.EffectiveInterval(1f, -5f), 1f);
        }

        [Test]
        public void EffectiveMaxAlive_Scales_WithRounding()
        {
            Assert.AreEqual(15, DifficultyMath.EffectiveMaxAlive(10, 1.5f));
            Assert.AreEqual(13, DifficultyMath.EffectiveMaxAlive(10, 1.25f)); // 12.5 → 사사오입 13
            Assert.AreEqual(12, DifficultyMath.EffectiveMaxAlive(10, 1.24f));
        }

        [Test]
        public void EffectiveMaxAlive_ZeroMultiplier_MeansNoSpawns()
        {
            Assert.AreEqual(0, DifficultyMath.EffectiveMaxAlive(40, 0f));
            Assert.AreEqual(0, DifficultyMath.EffectiveMaxAlive(40, -1f));
        }

        [Test]
        public void BurstToSpawn_LegacyZero_TreatedAsSingle()
        {
            Assert.AreEqual(1, DifficultyMath.BurstToSpawn(0, 0, 10));
        }

        [Test]
        public void BurstToSpawn_CappedByRemainingSlots()
        {
            Assert.AreEqual(2, DifficultyMath.BurstToSpawn(5, 8, 10));
        }

        [Test]
        public void BurstToSpawn_FullAlive_ReturnsZero()
        {
            Assert.AreEqual(0, DifficultyMath.BurstToSpawn(3, 10, 10));
            Assert.AreEqual(0, DifficultyMath.BurstToSpawn(3, 12, 10));
        }

        [Test]
        public void BurstToSpawn_RoomAvailable_ReturnsFullBurst()
        {
            Assert.AreEqual(4, DifficultyMath.BurstToSpawn(4, 0, 10));
        }

        [Test]
        public void SafeHealthMultiplier_ClampsFloor()
        {
            Assert.AreEqual(0.1f, DifficultyMath.SafeHealthMultiplier(0f), 1e-4f);
            Assert.AreEqual(0.1f, DifficultyMath.SafeHealthMultiplier(-3f), 1e-4f);
            Assert.AreEqual(2.5f, DifficultyMath.SafeHealthMultiplier(2.5f), 1e-4f);
        }

        [Test]
        public void DifficultyData_EmptyCurve_DefaultsToOne()
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<DifficultyData>();
            data.SpawnRateByMinute = new UnityEngine.AnimationCurve(); // 키 없음
            Assert.AreEqual(1f, data.SpawnRateAt(600f), 1e-4f);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void DifficultyData_CurveEvaluatesInMinutes()
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<DifficultyData>();
            data.SpawnRateByMinute = UnityEngine.AnimationCurve.Linear(0f, 1f, 20f, 3f);
            // 10분(600초) 지점 = 중간값 2
            Assert.AreEqual(2f, data.SpawnRateAt(600f), 1e-3f);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }
}
