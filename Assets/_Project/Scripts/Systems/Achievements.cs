using System.Collections.Generic;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>업적 달성 여부 저장. 판정은 AchievementEvaluator — 이 클래스는 기록만 한다.</summary>
    public sealed class AchievementLog
    {
        private readonly IKeyValueStore _store;
        private readonly HashSet<string> _unlocked = new();

        public AchievementLog(IKeyValueStore store)
        {
            _store = store;
            string index = store.GetString("achieve.ids", string.Empty);
            if (string.IsNullOrEmpty(index))
            {
                return;
            }

            foreach (string id in index.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _unlocked.Add(id);
                }
            }
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public int UnlockedCount => _unlocked.Count;

        /// <summary>처음 달성이면 true를 반환하고 저장한다. 이미 달성이면 false.</summary>
        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || !_unlocked.Add(id))
            {
                return false;
            }

            _store.SetString("achieve.ids", string.Join(",", _unlocked));
            _store.Save();
            return true;
        }

        /// <summary>전역 인스턴스 — GameSettings.Resolve와 같은 lazy 등록 구조.</summary>
        public static AchievementLog Resolve()
        {
            if (!ServiceLocator.TryGet(out AchievementLog log))
            {
                log = new AchievementLog(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(log);
            }

            return log;
        }
    }

    /// <summary>
    /// 업적 판정 — 런 종료 시점의 통계와 평생 전적만으로 평가한다 (런 중 추적 상태 없음).
    /// 순수 정적 함수 — EditMode 테스트 대상.
    /// </summary>
    public static class AchievementEvaluator
    {
        /// <summary>정의 하나가 이번 런/누적 전적으로 달성됐는지 판정한다.</summary>
        public static bool IsSatisfied(AchievementData data, RunStatistics run, PlayerRecords records)
        {
            if (data == null || run == null)
            {
                return false;
            }

            switch (data.Metric)
            {
                case AchievementMetric.SurvivalSeconds:
                    return run.DurationSeconds >= data.Threshold;
                case AchievementMetric.KillsInRun:
                    return run.TotalKills >= data.Threshold;
                case AchievementMetric.TotalKills:
                    return records != null && records.TotalKills >= data.Threshold;
                case AchievementMetric.ReachLevel:
                    return run.FinalLevel >= data.Threshold;
                case AchievementMetric.MaxSingleHit:
                    return run.MaxSingleHit >= data.Threshold;
                case AchievementMetric.ClearRun:
                    return run.Victory;
                case AchievementMetric.TotalVictories:
                    return records != null && records.TotalVictories >= data.Threshold;
                case AchievementMetric.WeaponKillsInRun:
                    return WeaponKills(run, data.WeaponId) >= data.Threshold;
                default:
                    return false;
            }
        }

        private static int WeaponKills(RunStatistics run, string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                return 0;
            }

            IReadOnlyList<RunStatistics.WeaponStat> weapons = run.Weapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].WeaponId == weaponId)
                {
                    return weapons[i].Kills;
                }
            }

            return 0;
        }
    }
}
