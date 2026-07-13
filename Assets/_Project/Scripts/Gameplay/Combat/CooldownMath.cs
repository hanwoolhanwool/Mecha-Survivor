using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 쿨다운 계산 순수 로직 — 나눗셈 모델 (GDD 3.2).
    /// 실제 쿨다운 = 기본 / (1 + 쿨감 합계) / (지상 배율).
    /// 쿨다운은 0에 수렴하되 절대 도달하지 않는다 → 무한 난사(로테이션 붕괴)가 불가능하다.
    /// 뺄셈·퍼센트 합산 모델은 절대 채택하지 않는다.
    /// </summary>
    public static class CooldownMath
    {
        public static float Effective(float baseCooldown, float totalReduction, float groundedMultiplier = 1f)
        {
            totalReduction = Mathf.Max(0f, totalReduction);
            groundedMultiplier = Mathf.Max(1f, groundedMultiplier);
            return baseCooldown / (1f + totalReduction) / groundedMultiplier;
        }
    }
}
