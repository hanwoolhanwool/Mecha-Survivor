using System;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 난이도 배율 적용 순수 계산 (EditMode 테스트 대상).
    /// DifficultyData 곡선이 뱉은 배율을 스폰 규칙에 안전하게 곱하는 책임만 진다.
    /// </summary>
    public static class DifficultyMath
    {
        /// <summary>스폰 간격 하한(초) — 배율이 폭주해도 프레임당 스폰 폭탄을 막는다.</summary>
        public const float MinInterval = 0.05f;

        private const float MinRate = 0.01f;
        private const float MinHealthMultiplier = 0.1f;

        /// <summary>배율 적용 스폰 간격 — rate가 클수록 짧아진다 (간격 ÷ 배율).</summary>
        public static float EffectiveInterval(float baseInterval, float rateMultiplier)
        {
            float rate = rateMultiplier < MinRate ? MinRate : rateMultiplier;
            float interval = baseInterval / rate;
            return interval < MinInterval ? MinInterval : interval;
        }

        /// <summary>배율 적용 동시 생존 상한. 반올림(사사오입), 음수 배율은 0으로 취급.</summary>
        public static int EffectiveMaxAlive(int baseMaxAlive, float multiplier)
        {
            if (baseMaxAlive <= 0 || multiplier <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(baseMaxAlive * (double)multiplier,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 이번 틱에 실제 스폰할 무리 수 — 남은 생존 슬롯으로 제한.
        /// 직렬화 기본값 0(구 에셋)은 단일 스폰(1)으로 취급한다.
        /// </summary>
        public static int BurstToSpawn(int burstCount, int aliveCount, int maxAlive)
        {
            int burst = burstCount < 1 ? 1 : burstCount;
            int slots = maxAlive - aliveCount;
            if (slots <= 0)
            {
                return 0;
            }

            return burst < slots ? burst : slots;
        }

        /// <summary>적 HP 배율 안전 하한 — 0/음수 곡선 값으로 무적·즉사 데이터가 나오지 않게.</summary>
        public static float SafeHealthMultiplier(float raw) =>
            raw < MinHealthMultiplier ? MinHealthMultiplier : raw;
    }
}
