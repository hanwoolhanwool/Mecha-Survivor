using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 에너지 자원 — "쏘고 싶은데 쿨이 안 돈다"를 푼다 (GDD 4.2).
    /// 전역/무기별 쿨감, 기동력, 착지 쿨감 보너스 강화.
    /// 쿨감은 나눗셈 모델의 분모에 합산되므로 아무리 쌓아도 쿨다운이 0이 되지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnergyUpgrade", menuName = "Mecha Survivor/Upgrades/Energy")]
    public sealed class EnergyUpgradeData : UpgradeData
    {
        [Header("쿨감 (레벨당, 0.25 = +25%)")]
        public float GlobalCooldownReductionPerLevel;

        [Tooltip("특정 무기 전용 쿨감 대상 (비우면 전역만)")]
        public WeaponData TargetWeapon;
        public float WeaponCooldownReductionPerLevel;

        [Header("기동 (레벨당, 0.1 = +10%)")]
        public float MoveSpeedPerLevel;

        [Header("착지 쿨감 보너스 강화 (레벨당 배율 증가)")]
        public float GroundedMultiplierPerLevel;

        public override void Apply(MechaContext context, int level)
        {
            if (GlobalCooldownReductionPerLevel > 0f)
            {
                context.Cooldowns.AddGlobal(GlobalCooldownReductionPerLevel);
            }

            if (TargetWeapon != null && WeaponCooldownReductionPerLevel > 0f)
            {
                context.Cooldowns.AddForWeapon(TargetWeapon.Id, WeaponCooldownReductionPerLevel);
            }

            if (MoveSpeedPerLevel > 0f)
            {
                context.Controller.AddMoveSpeedMultiplier(MoveSpeedPerLevel);
            }

            if (GroundedMultiplierPerLevel > 0f)
            {
                context.Cooldowns.AddGroundedMultiplier(GroundedMultiplierPerLevel);
            }
        }
    }
}
