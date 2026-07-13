using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>자율 드론 파츠 (GDD 3.4 무기 11번) — 레벨당 드론 수 증가.</summary>
    [CreateAssetMenu(fileName = "DroneUpgrade", menuName = "Mecha Survivor/Upgrades/Drone")]
    public sealed class DroneUpgradeData : UpgradeData
    {
        [Tooltip("레벨당 추가 드론 수")]
        public int DronesPerLevel = 1;

        public override void Apply(MechaContext context, int level)
        {
            if (context.DroneRig != null)
            {
                context.DroneRig.AddDrones(DronesPerLevel);
            }
        }
    }
}
