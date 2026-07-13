using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>3택 선택지 하나. Recipe가 있으면 조합 오퍼(재료 소비)다.</summary>
    public readonly struct UpgradeOffer
    {
        public readonly UpgradeData Upgrade;
        public readonly RecipeData Recipe;

        public UpgradeOffer(UpgradeData upgrade, RecipeData recipe = null)
        {
            Upgrade = upgrade;
            Recipe = recipe;
        }

        public bool IsCombination => Recipe != null;
        public bool IsValid => Upgrade != null;
    }

    /// <summary>
    /// 3택 후보 추출·적용 (GDD 4.2, 8.1).
    /// 후보 = 미보유 획득안 + 보유 강화안(레벨 미달) + 조건 충족 조합안.
    /// 조합안은 등장 시 가중치 보너스를 줘 "보상감"을 만든다.
    /// </summary>
    public sealed class UpgradeService : MonoBehaviour
    {
        [Header("풀")]
        [SerializeField] private UpgradeData[] _upgradePool;
        [SerializeField] private RecipeData[] _recipes;

        [Header("참조")]
        [SerializeField] private UpgradeInventory _inventory;
        [SerializeField] private MechaContext _context;

        [Tooltip("조합안 가중치 배수 — 어렵게 모은 조합이 묻히지 않게")]
        [SerializeField] private float _combinationWeightBonus = 3f;

        // 재사용 버퍼 — 3택마다의 힙 할당 방지.
        private readonly List<UpgradeData> _candidates = new(32);
        private readonly List<float> _weights = new(32);
        private readonly List<RecipeData> _candidateRecipes = new(32);
        private readonly System.Random _rng = new();

        public UpgradeInventory Inventory => _inventory;

        /// <summary>3택 후보를 추출한다 (최대 3개 — 후보가 부족하면 그만큼만).</summary>
        public void RollThree(List<UpgradeOffer> result)
        {
            result.Clear();
            BuildCandidates();

            // 조합안 우선 확정 노출: 가중치로 뽑되, 여기서는 통합 추첨으로 단순화.
            var picked = new List<UpgradeData>(3);
            UpgradeRoller.RollWithoutReplacement(_candidates, _weights, 3, _rng, picked);

            for (int i = 0; i < picked.Count; i++)
            {
                result.Add(new UpgradeOffer(picked[i], FindRecipeFor(picked[i])));
            }
        }

        private void BuildCandidates()
        {
            _candidates.Clear();
            _weights.Clear();
            _candidateRecipes.Clear();

            if (_upgradePool != null)
            {
                for (int i = 0; i < _upgradePool.Length; i++)
                {
                    UpgradeData upgrade = _upgradePool[i];
                    if (upgrade == null || !_inventory.CanTake(upgrade))
                    {
                        continue;
                    }

                    // 선택해도 효과가 없는 상황(빈 슬롯 없는 무기 등)은 제안하지 않는다.
                    if (!upgrade.CanOffer(_context, _inventory.GetLevel(upgrade)))
                    {
                        continue;
                    }

                    _candidates.Add(upgrade);
                    _weights.Add(Mathf.Max(0.01f, upgrade.Rarity));
                    _candidateRecipes.Add(null);
                }
            }

            if (_recipes != null)
            {
                for (int i = 0; i < _recipes.Length; i++)
                {
                    RecipeData recipe = _recipes[i];
                    if (recipe == null ||
                        !RecipeResolver.IsOfferable(recipe, _inventory.OwnedLevels))
                    {
                        continue;
                    }

                    _candidates.Add(recipe.Result);
                    _weights.Add(Mathf.Max(0.01f, recipe.Result.Rarity) * _combinationWeightBonus);
                    _candidateRecipes.Add(recipe);
                }
            }
        }

        /// <summary>추첨 결과가 조합 후보였는지 역추적 (BuildCandidates와 같은 프레임에서만 유효).</summary>
        private RecipeData FindRecipeFor(UpgradeData upgrade)
        {
            if (_recipes == null)
            {
                return null;
            }

            for (int i = 0; i < _recipes.Length; i++)
            {
                RecipeData recipe = _recipes[i];
                if (recipe != null && recipe.Result == upgrade &&
                    RecipeResolver.IsOfferable(recipe, _inventory.OwnedLevels))
                {
                    return recipe;
                }
            }

            return null;
        }

        /// <summary>선택 확정 — 획득/강화 또는 조합.</summary>
        public bool Take(in UpgradeOffer offer)
        {
            if (!offer.IsValid)
            {
                return false;
            }

            return offer.IsCombination
                ? _inventory.Combine(offer.Recipe)
                : _inventory.Take(offer.Upgrade);
        }
    }
}
