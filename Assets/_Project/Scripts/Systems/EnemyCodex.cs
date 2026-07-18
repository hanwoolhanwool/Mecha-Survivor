using System.Collections.Generic;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 적 도감 — 적 ID별 평생 처치 수. 처치 1회 이상 = "발견".
    /// RecordKeeper가 런 중 인메모리로 세다가 런 종료 시 한 번에 저장한다
    /// (킬마다 PlayerPrefs 쓰기를 피한다). 순수 로직 — EditMode 테스트 대상.
    /// </summary>
    public sealed class EnemyCodex
    {
        private const string IndexKey = "codex.enemy.ids";

        private readonly IKeyValueStore _store;
        private readonly Dictionary<string, int> _kills = new();
        private readonly List<string> _ids = new();

        public EnemyCodex(IKeyValueStore store)
        {
            _store = store;
            string index = store.GetString(IndexKey, string.Empty);
            if (string.IsNullOrEmpty(index))
            {
                return;
            }

            foreach (string id in index.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _kills[id] = store.GetInt(Key(id), 0);
                    _ids.Add(id);
                }
            }
        }

        /// <summary>발견(처치)한 적 ID 목록.</summary>
        public IReadOnlyList<string> KnownIds => _ids;

        public bool IsDiscovered(string enemyId) =>
            _kills.TryGetValue(enemyId, out int kills) && kills > 0;

        public int GetKills(string enemyId) =>
            _kills.TryGetValue(enemyId, out int kills) ? kills : 0;

        /// <summary>런 1회 분량의 처치 집계를 반영하고 저장한다.</summary>
        public void RecordKills(IReadOnlyDictionary<string, int> killsByEnemyId)
        {
            if (killsByEnemyId == null || killsByEnemyId.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, int> kvp in killsByEnemyId)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0)
                {
                    continue;
                }

                if (!_kills.ContainsKey(kvp.Key))
                {
                    _ids.Add(kvp.Key);
                    _kills[kvp.Key] = 0;
                }

                _kills[kvp.Key] += kvp.Value;
                _store.SetInt(Key(kvp.Key), _kills[kvp.Key]);
            }

            _store.SetString(IndexKey, string.Join(",", _ids));
            _store.Save();
        }

        private static string Key(string id) => $"codex.enemy.{id}.kills";

        /// <summary>전역 인스턴스 — GameSettings.Resolve와 같은 lazy 등록 구조.</summary>
        public static EnemyCodex Resolve()
        {
            if (!ServiceLocator.TryGet(out EnemyCodex codex))
            {
                codex = new EnemyCodex(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(codex);
            }

            return codex;
        }
    }
}
