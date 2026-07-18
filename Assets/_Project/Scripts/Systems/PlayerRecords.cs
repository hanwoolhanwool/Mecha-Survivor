using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 런을 넘어 살아남는 영구 전적 (로비 표시용). RecordKeeper가 쓰고 로비가 읽는다.
    /// 순수 로직 + IKeyValueStore 저장 — EditMode 테스트 대상.
    /// </summary>
    public sealed class PlayerRecords
    {
        private const string BestSurvivalKey = "records.best_survival_seconds";
        private const string TotalRunsKey = "records.total_runs";
        private const string TotalKillsKey = "records.total_kills";
        private const string TotalVictoriesKey = "records.total_victories";

        private readonly IKeyValueStore _store;

        public PlayerRecords(IKeyValueStore store)
        {
            _store = store;
            BestSurvivalSeconds = Mathf.Max(0f, store.GetFloat(BestSurvivalKey, 0f));
            TotalRuns = store.GetInt(TotalRunsKey, 0);
            TotalKills = store.GetInt(TotalKillsKey, 0);
            TotalVictories = store.GetInt(TotalVictoriesKey, 0);
        }

        public float BestSurvivalSeconds { get; private set; }
        public int TotalRuns { get; private set; }
        public int TotalKills { get; private set; }
        public int TotalVictories { get; private set; }

        public bool HasAnyRun => TotalRuns > 0;

        /// <summary>런 1회 종료를 반영하고 즉시 저장한다.</summary>
        public void RecordRun(bool victory, float durationSeconds, int kills)
        {
            TotalRuns++;
            TotalKills += Mathf.Max(0, kills);

            if (victory)
            {
                TotalVictories++;
            }

            if (durationSeconds > BestSurvivalSeconds)
            {
                BestSurvivalSeconds = durationSeconds;
            }

            _store.SetFloat(BestSurvivalKey, BestSurvivalSeconds);
            _store.SetInt(TotalRunsKey, TotalRuns);
            _store.SetInt(TotalKillsKey, TotalKills);
            _store.SetInt(TotalVictoriesKey, TotalVictories);
            _store.Save();
        }

        /// <summary>전역 인스턴스 조회 — GameSettings.Resolve와 같은 lazy 등록 구조.</summary>
        public static PlayerRecords Resolve()
        {
            if (!ServiceLocator.TryGet(out PlayerRecords records))
            {
                records = new PlayerRecords(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(records);
            }

            return records;
        }
    }
}
