using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 시간에 따른 적 스폰 지휘 (GDD 8.1). WaveData 스케줄을 읽어 풀 경유로 스폰한다.
    /// 지상형/포탑형은 지면에 스냅, 공중형은 플레이어 주변 고도에 배치한다.
    /// </summary>
    public sealed class SpawnDirector : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private WaveData _waveData;

        [Tooltip("시간 기반 전역 난이도 곡선. 비워두면 배율 1로 동작")]
        [SerializeField] private DifficultyData _difficulty;

        [Header("참조")]
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("스폰 배치")]
        [Tooltip("플레이어 기준 스폰 거리 범위")]
        [SerializeField] private float _spawnRadiusMin = 35f;
        [SerializeField] private float _spawnRadiusMax = 55f;

        [Tooltip("공중형 스폰 고도: 플레이어 고도 기준 오프셋 — 같은 고도대에서 다가온다")]
        [SerializeField] private float _flyerAltitudeOffsetMin = -10f;
        [SerializeField] private float _flyerAltitudeOffsetMax = 15f;

        [Tooltip("공중형 최저 절대 고도 (지면 파묻힘 방지)")]
        [SerializeField] private float _flyerMinAltitude = 5f;

        [Tooltip("지면 스냅 레이캐스트 마스크 — Ground(14)")]
        [SerializeField] private LayerMask _groundMask = 1 << 14;

        [Tooltip("건물(Wall) 내부 스폰 방지 검사 마스크")]
        [SerializeField] private LayerMask _obstacleMask = 1 << 13;

        private const int SpawnPositionAttempts = 6;

        private float[] _nextSpawnTimes;
        private int[] _aliveCounts;
        private float _fallbackElapsed;

        /// <summary>QA: 켜면 스폰만 멈춘다 (생존 적 행동은 유지).</summary>
        public bool SpawningPaused { get; set; }

        /// <summary>QA/HUD: 현재 적용 중인 난이도 곡선.</summary>
        public DifficultyData Difficulty => _difficulty;

        private void Awake()
        {
            int count = _waveData != null ? _waveData.Spawns.Count : 0;
            _nextSpawnTimes = new float[count];
            _aliveCounts = new int[count];
        }

        private void Update()
        {
            if (_waveData == null || _player == null || SpawningPaused)
            {
                return;
            }

            float elapsed = GetElapsed();
            float rateMul = _difficulty != null ? _difficulty.SpawnRateAt(elapsed) : 1f;
            float aliveMul = _difficulty != null ? _difficulty.MaxAliveMultiplierAt(elapsed) : 1f;
            float healthMul = _difficulty != null ? _difficulty.HealthMultiplierAt(elapsed) : 1f;

            for (int i = 0; i < _waveData.Spawns.Count; i++)
            {
                WaveData.Spawn entry = _waveData.Spawns[i];
                if (entry.Enemy == null || entry.Enemy.Prefab == null)
                {
                    continue;
                }

                if (!WaveData.IsActiveAt(entry, elapsed) || elapsed < _nextSpawnTimes[i])
                {
                    continue;
                }

                int maxAlive = DifficultyMath.EffectiveMaxAlive(entry.MaxAlive, aliveMul);
                int burst = DifficultyMath.BurstToSpawn(entry.BurstCount, _aliveCounts[i], maxAlive);
                if (burst <= 0)
                {
                    // 상한 가득 — 간격을 미루지 않아 슬롯이 비면 즉시 재개된다.
                    continue;
                }

                SpawnBurst(entry.Enemy, i, burst, healthMul);
                _nextSpawnTimes[i] = elapsed + DifficultyMath.EffectiveInterval(
                    Mathf.Max(0.05f, entry.SpawnInterval), rateMul);
            }
        }

        private float GetElapsed()
        {
            if (ServiceLocator.TryGet(out RunTimer timer))
            {
                return timer.Elapsed;
            }

            _fallbackElapsed += Time.deltaTime;
            return _fallbackElapsed;
        }

        /// <summary>무리 단위 스폰 — 기준점 하나를 잡고 주변에 흩뿌려 떼로 몰려오게 한다.</summary>
        private void SpawnBurst(EnemyData data, int entryIndex, int count, float healthMultiplier)
        {
            float spread = _difficulty != null ? _difficulty.BurstSpreadRadius : 5f;
            Vector3 anchor = PickSpawnPosition(data);

            for (int n = 0; n < count; n++)
            {
                Vector3 position = n == 0 ? anchor : ScatterAround(anchor, spread, data);
                var brain = (EnemyBrain)PoolManager.Instance.Spawn(
                    data.Prefab, position, Quaternion.identity);
                brain.Init(data, _player, _playerHealth, this, entryIndex, healthMultiplier);
                _aliveCounts[entryIndex]++;
            }
        }

        /// <summary>무리 기준점 주변으로 흩뿌린 지점. 지상형은 그 지점의 지면 높이로 재스냅한다.</summary>
        private Vector3 ScatterAround(Vector3 anchor, float spread, EnemyData data)
        {
            Vector2 offset = Random.insideUnitCircle * spread;
            Vector3 position = new(anchor.x + offset.x, anchor.y, anchor.z + offset.y);

            if (IsAirborne(data))
            {
                position.y = Mathf.Max(_flyerMinAltitude, position.y + Random.Range(-2f, 2f));
                return position;
            }

            Vector3 rayOrigin = position + Vector3.up * 200f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 400f,
                    _groundMask, QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y;
            }

            return position;
        }

        private Vector3 PickSpawnPosition(EnemyData data)
        {
            Vector3 candidate = Vector3.zero;

            // 건물 내부에 갇히지 않는 지점을 몇 차례 재시도로 찾는다.
            for (int attempt = 0; attempt < SpawnPositionAttempts; attempt++)
            {
                candidate = PickCandidate(data);
                if (!Physics.CheckSphere(candidate + Vector3.up * 1f, 1.2f,
                        _obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    return candidate;
                }
            }

            return candidate;
        }

        private Vector3 PickCandidate(EnemyData data)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(_spawnRadiusMin, _spawnRadiusMax);
            Vector3 playerPosition = _player.position;
            Vector3 flat = new(
                playerPosition.x + Mathf.Cos(angle) * radius,
                0f,
                playerPosition.z + Mathf.Sin(angle) * radius);

            if (IsAirborne(data))
            {
                flat.y = Mathf.Max(
                    _flyerMinAltitude,
                    playerPosition.y + Random.Range(_flyerAltitudeOffsetMin, _flyerAltitudeOffsetMax));
                return flat;
            }

            // 지상형/포탑형: 위에서 아래로 레이캐스트해 지면에 스냅.
            Vector3 rayOrigin = flat + Vector3.up * 200f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 400f,
                    _groundMask, QueryTriggerInteraction.Ignore))
            {
                flat.y = hit.point.y;
            }

            return flat;
        }

        private static bool IsAirborne(EnemyData data) =>
            data.Archetype == EnemyArchetype.Flyer
            || data.Archetype == EnemyArchetype.Elite
            || data.Archetype == EnemyArchetype.Boss;

        /// <summary>QA: 시간 점프 뒤 호출 — 스폰 예약을 즉시로 되돌려 새 시각 기준으로 재개한다.</summary>
        public void ResetSchedule()
        {
            if (_nextSpawnTimes == null)
            {
                return;
            }

            for (int i = 0; i < _nextSpawnTimes.Length; i++)
            {
                _nextSpawnTimes[i] = 0f;
            }
        }

        /// <summary>QA: 생존 적 전원 회수. 처치 이벤트가 없어 경험치는 지급되지 않는다.</summary>
        public void DespawnAllAlive()
        {
            System.Collections.Generic.IReadOnlyList<EnemyBrain> active = EnemyBrain.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].ForceRelease();
            }
        }

        /// <summary>적 사망/자폭 시 EnemyBrain이 호출 — 해당 규칙의 생존 수 반환.</summary>
        public void NotifyReleased(int entryIndex)
        {
            if (_aliveCounts != null && entryIndex >= 0 && entryIndex < _aliveCounts.Length)
            {
                _aliveCounts[entryIndex] = Mathf.Max(0, _aliveCounts[entryIndex] - 1);
            }
        }
    }
}
