using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>개틀링 예열(스핀업) 순수 계산 — "쏠수록 빨라지는 가속 회전" (GDD 3.4 Lv.5 로망).</summary>
    public static class GatlingMath
    {
        /// <summary>발사 1회당 열 축적 (0~1 클램프).</summary>
        public static float AddHeat(float heat, float heatPerShot)
        {
            return Mathf.Clamp01(heat + heatPerShot);
        }

        /// <summary>발사가 멈추면 열이 식는다.</summary>
        public static float DecayHeat(float heat, float decayPerSecond, float deltaTime)
        {
            return Mathf.Max(0f, heat - decayPerSecond * deltaTime);
        }

        /// <summary>열 → 쿨다운 배율. 최대 예열 시 minScale까지 빨라진다 (0 초과 보장).</summary>
        public static float CooldownScale(float heat, float minScale)
        {
            minScale = Mathf.Clamp(minScale, 0.05f, 1f);
            return Mathf.Lerp(1f, minScale, Mathf.Clamp01(heat));
        }
    }
}
