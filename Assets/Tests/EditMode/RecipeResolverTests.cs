using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>조합 판정 검증 (GDD 4.4 — 재료 레벨 충족 시에만 3택 풀 등장).</summary>
    public sealed class RecipeResolverTests
    {
        private ArmorUpgradeData _ingredientA;
        private EnergyUpgradeData _ingredientB;
        private ArmorUpgradeData _result;
        private RecipeData _recipe;

        [SetUp]
        public void SetUp()
        {
            _ingredientA = ScriptableObject.CreateInstance<ArmorUpgradeData>();
            _ingredientB = ScriptableObject.CreateInstance<EnergyUpgradeData>();
            _result = ScriptableObject.CreateInstance<ArmorUpgradeData>();

            _recipe = ScriptableObject.CreateInstance<RecipeData>();
            _recipe.Ingredients = new[]
            {
                new RecipeData.Ingredient { Item = _ingredientA, RequiredLevel = 3 },
                new RecipeData.Ingredient { Item = _ingredientB, RequiredLevel = 2 },
            };
            _recipe.Result = _result;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ingredientA);
            Object.DestroyImmediate(_ingredientB);
            Object.DestroyImmediate(_result);
            Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void AllIngredientsAtRequiredLevel_IsSatisfied()
        {
            var owned = new Dictionary<UpgradeData, int> { [_ingredientA] = 3, [_ingredientB] = 2 };

            Assert.IsTrue(RecipeResolver.IsSatisfied(_recipe, owned));
        }

        [Test]
        public void IngredientBelowRequiredLevel_NotSatisfied()
        {
            var owned = new Dictionary<UpgradeData, int> { [_ingredientA] = 2, [_ingredientB] = 2 };

            Assert.IsFalse(RecipeResolver.IsSatisfied(_recipe, owned));
        }

        [Test]
        public void MissingIngredient_NotSatisfied()
        {
            var owned = new Dictionary<UpgradeData, int> { [_ingredientA] = 5 };

            Assert.IsFalse(RecipeResolver.IsSatisfied(_recipe, owned));
        }

        [Test]
        public void OverLeveledIngredients_StillSatisfied()
        {
            var owned = new Dictionary<UpgradeData, int> { [_ingredientA] = 5, [_ingredientB] = 5 };

            Assert.IsTrue(RecipeResolver.IsSatisfied(_recipe, owned));
        }

        [Test]
        public void ResultAlreadyOwned_NotOfferable()
        {
            var owned = new Dictionary<UpgradeData, int>
            {
                [_ingredientA] = 3, [_ingredientB] = 2, [_result] = 1,
            };

            Assert.IsTrue(RecipeResolver.IsSatisfied(_recipe, owned));
            Assert.IsFalse(RecipeResolver.IsOfferable(_recipe, owned),
                "이미 보유한 조합품은 다시 제안되지 않아야 한다.");
        }

        [Test]
        public void EmptyRecipe_NeverSatisfied()
        {
            var empty = ScriptableObject.CreateInstance<RecipeData>();
            empty.Ingredients = new RecipeData.Ingredient[0];
            empty.Result = _result;

            Assert.IsFalse(RecipeResolver.IsSatisfied(empty, new Dictionary<UpgradeData, int>()));

            Object.DestroyImmediate(empty);
        }
    }
}
