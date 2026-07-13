using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>선택지 카테고리 — 각각 다른 병목을 푼다 (GDD 4.2).</summary>
    public enum UpgradeCategory
    {
        Parts,  // "적을 못 죽인다" — 화력
        Armor,  // "내가 죽는다" — 생존
        Energy, // "쏘고 싶은데 쿨이 안 돈다" — 회전율·기동
    }

    /// <summary>
    /// 업그레이드 공통 베이스 (GDD 8.2). 3택 화면·인벤토리·조합은 이 타입만 다룬다 —
    /// 새 카테고리를 추가해도 기존 코드를 고치지 않는다 (개방-폐쇄).
    /// Apply는 "해당 레벨로 오른 순간의 증분"을 적용한다 (같은 아이템 재선택 = 강화).
    /// </summary>
    public abstract class UpgradeData : ScriptableObject
    {
        [Header("식별")]
        public string Id = "upgrade";
        public string DisplayName = "Upgrade";

        [TextArea]
        public string Description;

        public Sprite Icon;

        [Header("분류")]
        public UpgradeCategory Category;

        [Tooltip("3택 풀 가중치 (클수록 자주 등장)")]
        public float Rarity = 1f;

        [Tooltip("최대 강화 레벨 (GDD 4.3 — 기본 5)")]
        public int MaxLevel = 5;

        /// <summary>level 달성 시의 효과 증분을 적용한다 (1 = 최초 획득).</summary>
        public abstract void Apply(MechaContext context, int level);

        /// <summary>
        /// 현재 상태에서 3택 후보로 제안 가능한지. 레벨 상한은 인벤토리가 따로 검사하므로
        /// 여기서는 "선택해도 효과가 없는 상황"(예: 빈 슬롯 없는 무기 장착)만 거른다.
        /// </summary>
        public virtual bool CanOffer(MechaContext context, int currentLevel) => true;
    }
}
