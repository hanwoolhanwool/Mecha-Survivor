using UnityEngine;

namespace MechaSurvivor.Core
{
    /// <summary>적 사망. 픽업 스폰/경험치/스코어 시스템이 구독.</summary>
    public readonly struct EnemyKilledEvent : IEvent
    {
        public readonly Vector3 Position;
        public readonly int ExpReward;

        public EnemyKilledEvent(Vector3 position, int expReward)
        {
            Position = position;
            ExpReward = expReward;
        }
    }

    /// <summary>플레이어 피격. HUD 체력바가 구독.</summary>
    public readonly struct PlayerDamagedEvent : IEvent
    {
        public readonly float RemainingHealth;
        public readonly float MaxHealth;

        public PlayerDamagedEvent(float remainingHealth, float maxHealth)
        {
            RemainingHealth = remainingHealth;
            MaxHealth = maxHealth;
        }
    }

    /// <summary>플레이어 레벨업. 업그레이드 선택 UI가 구독.</summary>
    public readonly struct PlayerLeveledUpEvent : IEvent
    {
        public readonly int NewLevel;

        public PlayerLeveledUpEvent(int newLevel) => NewLevel = newLevel;
    }
}
