using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>장갑 부착 — 생존 병목을 푼다 (GDD 4.2). 레벨당 최대 HP 증가.</summary>
    [CreateAssetMenu(fileName = "ArmorUpgrade", menuName = "Mecha Survivor/Upgrades/Armor")]
    public sealed class ArmorUpgradeData : UpgradeData
    {
        [Tooltip("레벨당 최대 HP 증가량 (같은 양만큼 즉시 회복)")]
        public float MaxHealthPerLevel = 20f;

        public override void Apply(MechaContext context, int level)
        {
            if (MaxHealthPerLevel > 0f)
            {
                context.Health.AddMaxHealth(MaxHealthPerLevel);
            }
        }
    }
}
