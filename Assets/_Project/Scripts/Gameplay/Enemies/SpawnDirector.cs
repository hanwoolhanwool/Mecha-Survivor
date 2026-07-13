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

        private void Awake()
        {
            int count = _waveData != null ? _waveData.Spawns.Count : 0;
            _nextSpawnTimes = new float[count];
            _aliveCounts = new int[count];
        }

        private void Update()
        {
            if (_waveData == null || _player == null)
            {
                return;
            }

            float elapsed = GetElapsed();

            for (int i = 0; i < _waveData.Spawns.Count; i++)
            {
                WaveData.Spawn entry = _waveData.Spawns[i];
                if (entry.Enemy == null || entry.Enemy.Prefab == null)
                {
                    continue;
                }

                if (!WaveData.IsActiveAt(entry, elapsed))
                {
                    continue;
                }

                if (elapsed < _nextSpawnTimes[i] || _aliveCounts[i] >= entry.MaxAlive)
                {
                    continue;
                }

                SpawnOne(entry.Enemy, i);
                _nextSpawnTimes[i] = elapsed + Mathf.Max(0.05f, entry.SpawnInterval);
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

        private void SpawnOne(EnemyData data, int entryIndex)
        {
            Vector3 position = PickSpawnPosition(data);

            var brain = (EnemyBrain)PoolManager.Instance.Spawn(
                data.Prefab, position, Quaternion.identity);
            brain.Init(data, _player, _playerHealth, this, entryIndex);
            _aliveCounts[entryIndex]++;
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

            bool airborne = data.Archetype == EnemyArchetype.Flyer
                            || data.Archetype == EnemyArchetype.Elite
                            || data.Archetype == EnemyArchetype.Boss;

            if (airborne)
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
