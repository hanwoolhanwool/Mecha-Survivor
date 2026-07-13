using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Gameplay
{
    /// <summary>레벨업 요구 경험치 곡선 순수 계산.</summary>
    public static class ExperienceCurve
    {
        /// <summary>level → level+1에 필요한 경험치. 선형 증가 (밸런싱은 파라미터로).</summary>
        public static int RequiredFor(int level, int baseRequirement, int growthPerLevel)
        {
            return baseRequirement + growthPerLevel * Mathf.Max(0, level - 1);
        }
    }

    /// <summary>
    /// 경험치 누적 → 레벨업 (GDD 4.1). 젬이 올리는 ExperienceGainedEvent만 구독하며,
    /// 레벨업 시 PlayerLeveledUpEvent를 올린다 — 3택 UI가 이를 받아 게임을 멈춘다.
    /// </summary>
    public sealed class PlayerExperience : MonoBehaviour
    {
        [SerializeField] private int _baseRequirement = 5;
        [SerializeField] private int _growthPerLevel = 4;

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int RequiredXp => ExperienceCurve.RequiredFor(Level, _baseRequirement, _growthPerLevel);

        private void OnEnable()
        {
            EventBus<ExperienceGainedEvent>.Subscribe(OnExperienceGained);
        }

        private void OnDisable()
        {
            EventBus<ExperienceGainedEvent>.Unsubscribe(OnExperienceGained);
        }

        private void OnExperienceGained(ExperienceGainedEvent evt)
        {
            CurrentXp += evt.Amount;

            while (CurrentXp >= RequiredXp)
            {
                CurrentXp -= RequiredXp;
                Level++;
                EventBus<PlayerLeveledUpEvent>.Raise(new PlayerLeveledUpEvent(Level));
            }
        }
    }
}
