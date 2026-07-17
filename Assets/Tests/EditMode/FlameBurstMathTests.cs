using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 산탄 Lv.5 화염 방사 탄 분배 검증 — 웨이브로 쪼개도 총 탄수(=데미지 총량)가
    /// 보존돼야 형태 변화가 밸런스를 건드리지 않는다.
    /// </summary>
    public sealed class FlameBurstMathTests
    {
        [TestCase(12, 4)]
        [TestCase(10, 4)]   // 나머지 2 → 앞 웨이브부터 +1
        [TestCase(3, 4)]    // 탄수 < 웨이브 수
        [TestCase(1, 1)]
        public void WaveSum_EqualsTotal(int total, int waves)
        {
            int sum = 0;
            for (int w = 0; w < waves; w++)
            {
                sum += FlameBurstMath.PelletsInWave(total, waves, w);
            }

            Assert.AreEqual(total, sum, "웨이브 합이 총 탄수와 다르면 DPS가 변한다");
        }

        [Test]
        public void Remainder_GoesToEarlierWaves()
        {
            // 10발 / 4웨이브 = 3,3,2,2 — 첫 분사가 가장 두텁다.
            Assert.AreEqual(3, FlameBurstMath.PelletsInWave(10, 4, 0));
            Assert.AreEqual(3, FlameBurstMath.PelletsInWave(10, 4, 1));
            Assert.AreEqual(2, FlameBurstMath.PelletsInWave(10, 4, 2));
            Assert.AreEqual(2, FlameBurstMath.PelletsInWave(10, 4, 3));
        }

        [Test]
        public void OutOfRangeWaveIndex_ReturnsZero()
        {
            Assert.AreEqual(0, FlameBurstMath.PelletsInWave(10, 4, -1));
            Assert.AreEqual(0, FlameBurstMath.PelletsInWave(10, 4, 4));
        }

        [Test]
        public void DegenerateInputs_DoNotThrow()
        {
            Assert.AreEqual(0, FlameBurstMath.PelletsInWave(0, 4, 0));
            Assert.AreEqual(5, FlameBurstMath.PelletsInWave(5, 0, 0), "웨이브 0은 1로 보정");
        }
    }
}
