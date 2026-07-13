using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 업그레이드 적용 대상의 집합(플레이어 루트). UpgradeData.Apply가 받는 유일한 창구 —
    /// 업그레이드는 플레이어 내부 구조를 모르고 이 컨텍스트만 안다.
    /// </summary>
    public sealed class MechaContext : MonoBehaviour
    {
        [SerializeField] private MechaController _controller;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private WeaponSlots _weaponSlots;
        [SerializeField] private CooldownModifier _cooldowns;
        [SerializeField] private MechaAimer _aimer;
        [SerializeField] private SupportDroneRig _droneRig;

        [Tooltip("장착 무기가 붙는 마운트 (비우면 자기 트랜스폼)")]
        [SerializeField] private Transform _weaponMount;

        public MechaController Controller => _controller;
        public PlayerHealth Health => _health;
        public WeaponSlots WeaponSlots => _weaponSlots;
        public CooldownModifier Cooldowns => _cooldowns;
        public MechaAimer Aimer => _aimer;
        public SupportDroneRig DroneRig => _droneRig;

        private Transform WeaponMount => _weaponMount != null ? _weaponMount : transform;

        /// <summary>이미 장착된 같은 데이터의 무기를 찾는다 (강화 대상 탐색).</summary>
        public Weapon FindEquipped(WeaponData data)
        {
            for (int i = 0; i < WeaponSlots.MaxSlots; i++)
            {
                Weapon weapon = _weaponSlots.GetWeapon(i);
                if (weapon != null && weapon.Data == data)
                {
                    return weapon;
                }
            }

            return null;
        }

        /// <summary>무기 프리팹을 마운트에 스폰해 빈 슬롯에 장착. 자리가 없으면 null.</summary>
        public Weapon MountWeapon(Weapon weaponPrefab, WeaponData data)
        {
            if (weaponPrefab == null || _weaponSlots == null)
            {
                return null;
            }

            var weapon = (Weapon)PoolManager.Instance.Spawn(
                weaponPrefab, WeaponMount.position, WeaponMount.rotation);
            weapon.transform.SetParent(WeaponMount, worldPositionStays: false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            if (data != null)
            {
                weapon.SetData(data);
            }

            weapon.SetLevel(1);

            if (_weaponSlots.Equip(weapon) < 0)
            {
                // 빈 슬롯 없음 — 회수.
                PoolManager.Instance.Despawn(weapon);
                return null;
            }

            return weapon;
        }
    }
}
