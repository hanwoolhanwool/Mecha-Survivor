using System.Collections.Generic;

namespace MechaSurvivor.Gameplay
{
    /// <summary>조합 가능 여부 순수 판정 (GDD 8.1). 인벤토리 레벨만 보고 결정한다.</summary>
    public static class RecipeResolver
    {
        /// <summary>모든 재료가 요구 레벨 이상 보유 중인지.</summary>
        public static bool IsSatisfied(RecipeData recipe, IReadOnlyDictionary<UpgradeData, int> ownedLevels)
        {
            if (recipe == null || recipe.Result == null ||
                recipe.Ingredients == null || recipe.Ingredients.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                RecipeData.Ingredient ingredient = recipe.Ingredients[i];
                if (ingredient.Item == null)
                {
                    return false;
                }

                if (!ownedLevels.TryGetValue(ingredient.Item, out int level) ||
                    level < ingredient.RequiredLevel)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>조합 결과물이 3택 후보로 올라갈 수 있는지 (레시피 충족 + 결과물 미보유).</summary>
        public static bool IsOfferable(RecipeData recipe, IReadOnlyDictionary<UpgradeData, int> ownedLevels)
        {
            return IsSatisfied(recipe, ownedLevels) && !ownedLevels.ContainsKey(recipe.Result);
        }
    }
}
