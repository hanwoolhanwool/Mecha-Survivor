using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 이벤트 구독만으로 통계를 수집한다 (GDD 8.3 설계 요점 ②).
    /// 전투 코드는 이 시스템의 존재를 모른다 — 나중에 메타 프로그레션을 붙일 때도
    /// 전투 코드를 고치지 않고 같은 이벤트를 구독하는 서비스만 추가하면 된다.
    /// </summary>
    public sealed class StatisticsRecorder : MonoBehaviour
    {
        public RunStatistics Current { get; private set; } = new();

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
            EventBus<DamageDealtEvent>.Subscribe(OnDamageDealt);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<ExperienceGainedEvent>.Subscribe(OnExperienceGained);
            EventBus<PlayerLeveledUpEvent>.Subscribe(OnLeveledUp);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
            EventBus<DamageDealtEvent>.Unsubscribe(OnDamageDealt);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<ExperienceGainedEvent>.Unsubscribe(OnExperienceGained);
            EventBus<PlayerLeveledUpEvent>.Unsubscribe(OnLeveledUp);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<StatisticsRecorder>();
        }

        private void OnWeaponFired(WeaponFiredEvent evt) => Current.RecordShot(evt.WeaponId);

        private void OnDamageDealt(DamageDealtEvent evt) =>
            Current.RecordDamage(evt.SourceId, evt.Amount, evt.KilledTarget);

        private void OnEnemyKilled(EnemyKilledEvent evt) => Current.RecordKill();

        private void OnExperienceGained(ExperienceGainedEvent evt) =>
            Current.RecordExperience(evt.Amount);

        private void OnLeveledUp(PlayerLeveledUpEvent evt) => Current.RecordLevel(evt.NewLevel);

        private void OnRunEnded(RunEndedEvent evt) =>
            Current.RecordRunEnd(evt.Victory, evt.DurationSeconds);
    }
}
