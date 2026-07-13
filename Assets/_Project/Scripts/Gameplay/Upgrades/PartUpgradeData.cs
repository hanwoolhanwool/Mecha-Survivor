using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 파츠 조립 — 화력 병목을 푼다 (GDD 4.2).
    /// 무기 장착(최초)/강화(재선택), 또는 무기 슬롯 확장.
    /// </summary>
    [CreateAssetMenu(fileName = "PartUpgrade", menuName = "Mecha Survivor/Upgrades/Part")]
    public sealed class PartUpgradeData : UpgradeData
    {
        [Header("무기 파츠 (장착/강화)")]
        public Weapon WeaponPrefab;
        public WeaponData Weapon;

        [Header("슬롯 확장 파츠")]
        [Tooltip("true면 무기 대신 슬롯을 1개 해금한다 (2 → 최대 4)")]
        public bool UnlocksWeaponSlot;

        public override bool CanOffer(MechaContext context, int currentLevel)
        {
            if (context == null)
            {
                return true;
            }

            // 슬롯 확장: 이미 최대 슬롯이면 무의미.
            if (UnlocksWeaponSlot)
            {
                return context.WeaponSlots.UnlockedSlots < WeaponSlots.MaxSlots;
            }

            // 미보유 무기 파츠: 빈 슬롯이 없으면 선택이 낭비되므로 제안하지 않는다.
            if (Weapon != null && currentLevel == 0)
            {
                return context.WeaponSlots.HasFreeUnlockedSlot;
            }

            return true;
        }

        public override void Apply(MechaContext context, int level)
        {
            if (UnlocksWeaponSlot)
            {
                context.WeaponSlots.UnlockSlot();
                return;
            }

            if (Weapon == null)
            {
                return;
            }

            Weapon equipped = context.FindEquipped(Weapon);
            if (equipped == null)
            {
                context.MountWeapon(WeaponPrefab, Weapon);
            }
            else
            {
                equipped.SetLevel(level);
            }
        }
    }
}
