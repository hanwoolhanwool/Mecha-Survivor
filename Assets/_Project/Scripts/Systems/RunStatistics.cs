using System.Collections.Generic;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 한 판의 통계 집계 (GDD 6장). 순수 C# 클래스 — EditMode 테스트 대상.
    /// 핵심은 "무기별 기여도" — 플레이어가 다음 판에 무엇을 다르게 할지 배우는 유일한 창구.
    /// </summary>
    public sealed class RunStatistics
    {
        public sealed class WeaponStat
        {
            public string WeaponId;
            public float TotalDamage;
            public int ShotsFired;
            public int Kills;
            public float MaxSingleHit;

            // 가동률 샘플 — "쿨 대기로 못 쏜 시간" 비율의 근거 (GDD 6장)
            public int ReadySamples;
            public int TotalSamples;

            /// <summary>쿨 대기로 못 쏜 시간 비율 (0 = 항상 준비돼 있었음).</summary>
            public float DowntimeRatio =>
                TotalSamples > 0 ? 1f - (float)ReadySamples / TotalSamples : 0f;
        }

        private readonly Dictionary<string, WeaponStat> _weapons = new();
        private readonly List<WeaponStat> _weaponList = new();

        public int TotalKills { get; private set; }
        public float TotalDamage { get; private set; }
        public float MaxSingleHit { get; private set; }
        public string MaxSingleHitWeaponId { get; private set; }
        public int TotalExperience { get; private set; }
        public int FinalLevel { get; private set; } = 1;
        public bool Victory { get; private set; }
        public float DurationSeconds { get; private set; }

        /// <summary>무기별 통계 (기여도 순 정렬은 UI 몫).</summary>
        public IReadOnlyList<WeaponStat> Weapons => _weaponList;

        public void RecordShot(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                return;
            }

            GetOrCreate(weaponId).ShotsFired++;
        }

        public void RecordDamage(string weaponId, float amount, bool killedTarget)
        {
            // 출처 없는 데미지(적 투사체 등)는 무기 기여도에 넣지 않는다.
            if (string.IsNullOrEmpty(weaponId))
            {
                return;
            }

            WeaponStat stat = GetOrCreate(weaponId);
            stat.TotalDamage += amount;
            TotalDamage += amount;

            if (amount > stat.MaxSingleHit)
            {
                stat.MaxSingleHit = amount;
            }

            if (amount > MaxSingleHit)
            {
                MaxSingleHit = amount;
                MaxSingleHitWeaponId = weaponId;
            }

            if (killedTarget)
            {
                stat.Kills++;
            }
        }

        public void RecordKill() => TotalKills++;

        /// <summary>주기 샘플: 무기가 발사 가능 상태였는지 (가동률 집계).</summary>
        public void RecordUptimeSample(string weaponId, bool ready)
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                return;
            }

            WeaponStat stat = GetOrCreate(weaponId);
            stat.TotalSamples++;
            if (ready)
            {
                stat.ReadySamples++;
            }
        }

        private int _groundedSamples;
        private int _altitudeSamples;

        /// <summary>지상 체류 비율 (GDD 2.2의 저울을 어떻게 썼는가).</summary>
        public float GroundedRatio =>
            _altitudeSamples > 0 ? (float)_groundedSamples / _altitudeSamples : 0f;

        /// <summary>주기 샘플: 접지 상태였는지 (지상/공중 체류 비율).</summary>
        public void RecordAltitudeSample(bool grounded)
        {
            _altitudeSamples++;
            if (grounded)
            {
                _groundedSamples++;
            }
        }

        public void RecordExperience(int amount) => TotalExperience += amount;

        public void RecordLevel(int level) => FinalLevel = level;

        public void RecordRunEnd(bool victory, float durationSeconds)
        {
            Victory = victory;
            DurationSeconds = durationSeconds;
        }

        private WeaponStat GetOrCreate(string weaponId)
        {
            if (!_weapons.TryGetValue(weaponId, out WeaponStat stat))
            {
                stat = new WeaponStat { WeaponId = weaponId };
                _weapons[weaponId] = stat;
                _weaponList.Add(stat);
            }

            return stat;
        }
    }
}
