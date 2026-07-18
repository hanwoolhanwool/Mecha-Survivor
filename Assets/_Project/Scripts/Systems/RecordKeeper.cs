using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 런 결과를 영구 메타 진행(전적·무기 아카이브·적 도감·업적)에 반영한다.
    /// StatisticsRecorder와 같은 원칙 — 이벤트만 구독하고 전투 코드는 이 시스템의 존재를 모른다
    /// (GDD 8.3 ②). Game 씬에만 배치한다 — 랩 씬의 실험은 메타에 오염을 남기지 않는다.
    /// 적 처치는 런 중 인메모리로 세다가 런 종료 시 한 번에 저장한다 (킬마다 디스크 쓰기 방지).
    /// </summary>
    public sealed class RecordKeeper : MonoBehaviour
    {
        [Tooltip("런 종료 시 판정할 업적 정의 목록")]
        [SerializeField] private AchievementData[] _achievements = System.Array.Empty<AchievementData>();

        private readonly Dictionary<string, int> _runKillsByEnemyId = new();

        private void OnEnable()
        {
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (string.IsNullOrEmpty(evt.EnemyId))
            {
                return;
            }

            _runKillsByEnemyId.TryGetValue(evt.EnemyId, out int kills);
            _runKillsByEnemyId[evt.EnemyId] = kills + 1;
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            RunStatistics stats = ServiceLocator.TryGet(out StatisticsRecorder recorder)
                ? recorder.Current
                : null;

            PlayerRecords records = PlayerRecords.Resolve();
            records.RecordRun(evt.Victory, evt.DurationSeconds, stats?.TotalKills ?? 0);

            EnemyCodex.Resolve().RecordKills(_runKillsByEnemyId);
            _runKillsByEnemyId.Clear();

            if (stats == null)
            {
                return;
            }

            WeaponArchive.Resolve().RecordRun(stats.Weapons);

            AchievementLog log = AchievementLog.Resolve();
            for (int i = 0; i < _achievements.Length; i++)
            {
                AchievementData data = _achievements[i];
                if (data != null && !log.IsUnlocked(data.Id)
                    && AchievementEvaluator.IsSatisfied(data, stats, records))
                {
                    log.Unlock(data.Id);
                }
            }
        }
    }
}
