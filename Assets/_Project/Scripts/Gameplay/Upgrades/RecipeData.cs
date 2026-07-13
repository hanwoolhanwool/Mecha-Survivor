using System;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 조합 레시피 (GDD 4.4). 재료들이 요구 레벨에 도달하면 3택 풀에 결과물이 등장한다.
    /// 조합은 두 카테고리를 가로지르게 설계한다 — 한 카테고리만 파는 것을 막는다.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe", menuName = "Mecha Survivor/Recipe")]
    public sealed class RecipeData : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            public UpgradeData Item;
            public int RequiredLevel;
        }

        public Ingredient[] Ingredients;

        [Tooltip("재료를 소비하고 이 아이템으로 대체된다")]
        public UpgradeData Result;
    }
}
