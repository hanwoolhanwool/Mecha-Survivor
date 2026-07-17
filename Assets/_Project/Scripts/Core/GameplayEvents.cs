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

    /// <summary>무기 발사. 통계(발사 횟수·가동률)와 카메라 셰이크가 구독.</summary>
    public readonly struct WeaponFiredEvent : IEvent
    {
        public readonly string WeaponId;

        public WeaponFiredEvent(string weaponId) => WeaponId = weaponId;
    }

    /// <summary>데미지 성립. 통계(무기별 기여도)가 구독 — 전투 코드는 통계의 존재를 모른다.</summary>
    public readonly struct DamageDealtEvent : IEvent
    {
        public readonly string SourceId;
        public readonly float Amount;
        public readonly Vector3 Position;
        public readonly bool KilledTarget;

        public DamageDealtEvent(string sourceId, float amount, Vector3 position, bool killedTarget)
        {
            SourceId = sourceId;
            Amount = amount;
            Position = position;
            KilledTarget = killedTarget;
        }
    }

    /// <summary>런 종료(클리어/사망). 결과 화면이 구독.</summary>
    public readonly struct RunEndedEvent : IEvent
    {
        public readonly bool Victory;
        public readonly float DurationSeconds;

        public RunEndedEvent(bool victory, float durationSeconds)
        {
            Victory = victory;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>플레이어 사망. RunTimer가 구독해 런을 종료한다.</summary>
    public readonly struct PlayerDiedEvent : IEvent
    {
        public readonly Vector3 Position;

        public PlayerDiedEvent(Vector3 position) => Position = position;
    }

    /// <summary>경험치 획득. 레벨 진행/HUD가 구독.</summary>
    public readonly struct ExperienceGainedEvent : IEvent
    {
        public readonly int Amount;

        public ExperienceGainedEvent(int amount) => Amount = amount;
    }

    /// <summary>
    /// 차징 무기의 실제 발사 순간(레일건 등). WeaponFired(방아쇠·쿨다운 시작)와 구별된다.
    /// 카메라 킥처럼 "탄이 나가는 순간"에 맞춰야 하는 연출이 구독한다.
    /// </summary>
    public readonly struct WeaponDischargedEvent : IEvent
    {
        public readonly string WeaponId;
        public readonly Vector3 Position;

        public WeaponDischargedEvent(string weaponId, Vector3 position)
        {
            WeaponId = weaponId;
            Position = position;
        }
    }

    /// <summary>대형 착탄(궤도 폭격 등). 화면 플래시·카메라 셰이크가 구독.</summary>
    public readonly struct HeavyImpactEvent : IEvent
    {
        public readonly Vector3 Position;

        /// <summary>연출 강도 0~1. 플래시 알파·셰이크 진폭에 곱해진다.</summary>
        public readonly float Magnitude;

        public HeavyImpactEvent(Vector3 position, float magnitude)
        {
            Position = position;
            Magnitude = magnitude;
        }
    }
}
