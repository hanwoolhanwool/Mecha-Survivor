using System;

namespace MechaSurvivor.Gameplay
{
    /// <summary>산탄 Lv.5 화염 방사 — 탄 분배 계산 (순수 로직, EditMode 테스트 대상).</summary>
    public static class FlameBurstMath
    {
        /// <summary>
        /// 총 탄수를 웨이브에 균등 분배한다. 나머지는 앞 웨이브부터 1발씩 —
        /// 첫 분사가 가장 두텁고 전체 합은 정확히 total이 되어 DPS가 변하지 않는다.
        /// </summary>
        public static int PelletsInWave(int total, int waves, int waveIndex)
        {
            total = Math.Max(0, total);
            waves = Math.Max(1, waves);
            if (waveIndex < 0 || waveIndex >= waves)
            {
                return 0;
            }

            int baseCount = total / waves;
            int remainder = total % waves;
            return baseCount + (waveIndex < remainder ? 1 : 0);
        }
    }
}
