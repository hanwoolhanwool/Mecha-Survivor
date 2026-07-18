using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 무기별 평생 누적 전적 (로비 전적 화면·도감 "사용해본 무기" 판정용).
    /// RecordKeeper가 런 종료 시 RunStatistics를 흘려 넣고, 로비가 읽는다.
    /// 순수 로직 + IKeyValueStore — EditMode 테스트 대상.
    /// </summary>
    public sealed class WeaponArchive
    {
        public sealed class Entry
        {
            public string WeaponId;
            public float TotalDamage;
            public int Kills;
            public int Shots;
            public float BestSingleHit;
            public int RunsUsed;
        }

        private const string IndexKey = "archive.weapon.ids";

        private readonly IKeyValueStore _store;
        private readonly Dictionary<string, Entry> _entries = new();
        private readonly List<Entry> _entryList = new();

        public WeaponArchive(IKeyValueStore store)
        {
            _store = store;
            string index = store.GetString(IndexKey, string.Empty);
            if (string.IsNullOrEmpty(index))
            {
                return;
            }

            foreach (string id in index.Split(','))
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var entry = new Entry
                {
                    WeaponId = id,
                    TotalDamage = store.GetFloat(Key(id, "damage"), 0f),
                    Kills = store.GetInt(Key(id, "kills"), 0),
                    Shots = store.GetInt(Key(id, "shots"), 0),
                    BestSingleHit = store.GetFloat(Key(id, "best_hit"), 0f),
                    RunsUsed = store.GetInt(Key(id, "runs"), 0),
                };
                _entries[id] = entry;
                _entryList.Add(entry);
            }
        }

        /// <summary>알려진 무기 엔트리 (정렬은 UI 몫).</summary>
        public IReadOnlyList<Entry> Entries => _entryList;

        public bool TryGet(string weaponId, out Entry entry) =>
            _entries.TryGetValue(weaponId, out entry);

        /// <summary>이 무기를 써본 적이 있는가 (도감 발견 판정).</summary>
        public bool HasUsed(string weaponId) =>
            _entries.TryGetValue(weaponId, out Entry entry) && entry.Shots > 0;

        /// <summary>런 1회의 무기 통계를 누적하고 저장한다.</summary>
        public void RecordRun(IReadOnlyList<RunStatistics.WeaponStat> weapons)
        {
            if (weapons == null || weapons.Count == 0)
            {
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                RunStatistics.WeaponStat stat = weapons[i];
                if (string.IsNullOrEmpty(stat.WeaponId))
                {
                    continue;
                }

                Entry entry = GetOrCreate(stat.WeaponId);
                entry.TotalDamage += stat.TotalDamage;
                entry.Kills += stat.Kills;
                entry.Shots += stat.ShotsFired;
                entry.RunsUsed++;
                if (stat.MaxSingleHit > entry.BestSingleHit)
                {
                    entry.BestSingleHit = stat.MaxSingleHit;
                }

                _store.SetFloat(Key(entry.WeaponId, "damage"), entry.TotalDamage);
                _store.SetInt(Key(entry.WeaponId, "kills"), entry.Kills);
                _store.SetInt(Key(entry.WeaponId, "shots"), entry.Shots);
                _store.SetFloat(Key(entry.WeaponId, "best_hit"), entry.BestSingleHit);
                _store.SetInt(Key(entry.WeaponId, "runs"), entry.RunsUsed);
            }

            _store.SetString(IndexKey, string.Join(",", CollectIds()));
            _store.Save();
        }

        private string[] CollectIds()
        {
            var ids = new string[_entryList.Count];
            for (int i = 0; i < _entryList.Count; i++)
            {
                ids[i] = _entryList[i].WeaponId;
            }

            return ids;
        }

        private Entry GetOrCreate(string weaponId)
        {
            if (!_entries.TryGetValue(weaponId, out Entry entry))
            {
                entry = new Entry { WeaponId = weaponId };
                _entries[weaponId] = entry;
                _entryList.Add(entry);
            }

            return entry;
        }

        private static string Key(string id, string field) => $"archive.weapon.{id}.{field}";

        /// <summary>전역 인스턴스 — GameSettings.Resolve와 같은 lazy 등록 구조.</summary>
        public static WeaponArchive Resolve()
        {
            if (!ServiceLocator.TryGet(out WeaponArchive archive))
            {
                archive = new WeaponArchive(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(archive);
            }

            return archive;
        }
    }
}
