using System.Collections.Generic;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 3택 후보 가중치 추첨 순수 로직. RNG를 주입받아 EditMode에서 결정적으로 검증한다.
    /// </summary>
    public static class UpgradeRoller
    {
        /// <summary>
        /// 가중치 비복원 추첨. candidates/weights는 병렬 리스트이며 호출 중 파괴적으로 소비된다
        /// (swap-remove). 결과는 result에 추가된다.
        /// </summary>
        public static void RollWithoutReplacement(
            List<UpgradeData> candidates, List<float> weights, int count,
            System.Random rng, List<UpgradeData> result)
        {
            count = System.Math.Min(count, candidates.Count);

            for (int picked = 0; picked < count; picked++)
            {
                int index = PickWeightedIndex(weights, rng);
                if (index < 0)
                {
                    break;
                }

                result.Add(candidates[index]);

                // swap-remove — 순서는 중요하지 않다.
                int last = candidates.Count - 1;
                candidates[index] = candidates[last];
                weights[index] = weights[last];
                candidates.RemoveAt(last);
                weights.RemoveAt(last);
            }
        }

        /// <summary>가중치 인덱스 추첨. 유효 가중치 합이 0이면 -1.</summary>
        public static int PickWeightedIndex(IReadOnlyList<float> weights, System.Random rng)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0f)
                {
                    total += weights[i];
                }
            }

            if (total <= 0f)
            {
                return -1;
            }

            float roll = (float)rng.NextDouble() * total;
            float cumulative = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                cumulative += weights[i];
                if (roll < cumulative)
                {
                    return i;
                }
            }

            // 부동소수 경계 — 마지막 유효 항목.
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
