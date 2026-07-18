using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 보유 업그레이드와 레벨 (GDD 4.3 — 처음 고르면 획득 Lv.1, 재선택은 강화, 최대 Lv.5).
    /// 조합 시 재료를 소비하고 결과물로 대체한다 (GDD 4.4).
    /// </summary>
    public sealed class UpgradeInventory : MonoBehaviour
    {
        [SerializeField] private MechaContext _context;

        [Tooltip("시작 로드아웃 — 인벤토리를 거쳐 지급해야 강화안·조합 판정과 연동된다")]
        [SerializeField] private UpgradeData[] _initialUpgrades;

        private readonly Dictionary<UpgradeData, int> _levels = new();

        /// <summary>
        /// 초기 지급 목록의 무기 파츠 전부를 주어진 세트로 교체한다
        /// (로비 기체 선택 — StartLoadoutApplier가 Awake에서 호출).
        /// 무기 외 초기 업그레이드(방어구·슬롯 확장 등)는 유지되고, Start의 정상 지급 경로를
        /// 그대로 타므로 강화안·조합 판정 연동이 깨지지 않는다. Start 이후 호출은 효과가 없다.
        /// </summary>
        public void OverrideInitialWeapons(PartUpgradeData[] weaponParts)
        {
            if (weaponParts == null || weaponParts.Length == 0 || _initialUpgrades == null)
            {
                return;
            }

            var kept = new List<UpgradeData>(_initialUpgrades.Length + weaponParts.Length);
            for (int i = 0; i < _initialUpgrades.Length; i++)
            {
                bool isWeaponPart = _initialUpgrades[i] is PartUpgradeData part
                    && part.Weapon != null && !part.UnlocksWeaponSlot;
                if (!isWeaponPart)
                {
                    kept.Add(_initialUpgrades[i]);
                }
            }

            for (int i = 0; i < weaponParts.Length; i++)
            {
                if (weaponParts[i] != null)
                {
                    kept.Add(weaponParts[i]);
                }
            }

            _initialUpgrades = kept.ToArray();
        }

        private void Start()
        {
            if (_initialUpgrades == null)
            {
                return;
            }

            for (int i = 0; i < _initialUpgrades.Length; i++)
            {
                Take(_initialUpgrades[i]);
            }
        }

        /// <summary>보유 레벨 (미보유 = 0).</summary>
        public int GetLevel(UpgradeData upgrade) =>
            upgrade != null && _levels.TryGetValue(upgrade, out int level) ? level : 0;

        public IReadOnlyDictionary<UpgradeData, int> OwnedLevels => _levels;

        public bool CanTake(UpgradeData upgrade) =>
            upgrade != null && GetLevel(upgrade) < upgrade.MaxLevel;

        /// <summary>획득/강화. 새 레벨의 증분 효과를 적용한다.</summary>
        public bool Take(UpgradeData upgrade)
        {
            if (!CanTake(upgrade))
            {
                return false;
            }

            int newLevel = GetLevel(upgrade) + 1;
            _levels[upgrade] = newLevel;
            upgrade.Apply(_context, newLevel);
            return true;
        }

        /// <summary>
        /// 조합 실행: 재료를 인벤토리에서 제거(추가 강화 봉인)하고 결과물을 획득한다.
        /// 재료가 이미 적용한 효과(HP 등)는 유지된다 — 조합은 "합쳐서 더 강해지는" 방향만 있다.
        /// </summary>
        public bool Combine(RecipeData recipe)
        {
            if (!RecipeResolver.IsOfferable(recipe, _levels))
            {
                return false;
            }

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                _levels.Remove(recipe.Ingredients[i].Item);
            }

            int newLevel = 1;
            _levels[recipe.Result] = newLevel;
            recipe.Result.Apply(_context, newLevel);
            return true;
        }
    }
}
