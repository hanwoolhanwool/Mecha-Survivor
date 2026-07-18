using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 적 행동: 아키타입별 추적/자폭/포탑 사격 (GDD 5.3).
    /// 겹침 방지는 물리 대신 분리 스티어링으로, 스티어링 재계산은 프레임 분산(time-slicing)으로
    /// 부하를 낮춘다 (GDD 8.4). 풀에서 재사용되므로 상태는 Init/OnReturnedToPool에서 리셋한다.
    /// </summary>
    public sealed class EnemyBrain : MonoBehaviour, IPoolable
    {
        /// <summary>스티어링 이웃 탐색용 활성 적 레지스트리.</summary>
        private static readonly List<EnemyBrain> Active = new();

        public static IReadOnlyList<EnemyBrain> ActiveEnemies => Active;

        private const int SteeringSliceFrames = 6; // 6프레임에 1회 분리 벡터 재계산
        private const float SeparationWeight = 1.2f;

        [SerializeField] private Health _health;

        [Header("장애물 회피 — 건물(Wall)을 뚫지 않고 벽을 타고 돈다")]
        [Tooltip("Wall(13)")]
        [SerializeField] private LayerMask _obstacleMask = 1 << 13;
        [SerializeField] private float _obstacleCheckDistance = 3f;

        public EnemyData Data { get; private set; }

        private Transform _player;
        private IDamageable _playerDamageable;
        private SpawnDirector _director;
        private int _directorEntryIndex = -1;
        private float _nextContactTime;
        private float _nextAttackTime;
        private int _steerPhase;
        private Vector3 _separation;
        private Vector3 _lastPlayerPosition;
        private Vector3 _playerVelocity;
        private bool _released;
        private float _empSlowFactor = 1f;
        private float _empEndTime;

        /// <summary>EMP 감전 중 — 이동 슬로우 + 사격 봉쇄 (GDD 3.4-10). 피격 연출도 읽는다.</summary>
        public bool IsEmpAffected => Time.time < _empEndTime;

        /// <summary>EMP 상태 부여. 필드가 틱마다 갱신 호출한다 — 만료는 시간이 처리.</summary>
        public void ApplyEmp(float slowFactor, float duration)
        {
            _empSlowFactor = Mathf.Clamp(slowFactor, 0.05f, 1f);
            _empEndTime = Mathf.Max(_empEndTime, Time.time + duration);
        }

        /// <summary>스폰 직후 스포너가 호출. 데이터·타깃 주입 + 상태 리셋. healthMultiplier = 난이도 HP 배율.</summary>
        public void Init(EnemyData data, Transform player, IDamageable playerDamageable,
            SpawnDirector director = null, int directorEntryIndex = -1, float healthMultiplier = 1f)
        {
            Data = data;
            _player = player;
            _playerDamageable = playerDamageable;
            _director = director;
            _directorEntryIndex = directorEntryIndex;
            _nextContactTime = 0f;
            _nextAttackTime = Time.time + Random.Range(0f, data.AttackInterval);
            _steerPhase = Random.Range(0, SteeringSliceFrames);
            _separation = Vector3.zero;
            _released = false;
            _empSlowFactor = 1f;
            _empEndTime = 0f;

            if (_health != null)
            {
                _health.Init(data.MaxHealth * healthMultiplier);
            }

            if (player != null)
            {
                _lastPlayerPosition = player.position;
            }
        }

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            Active.Add(this);
            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (Data == null || _player == null || _released)
            {
                return;
            }

            float dt = Time.deltaTime;
            TrackPlayerVelocity(dt);

            switch (Data.Archetype)
            {
                case EnemyArchetype.Ground:
                    ChaseFlat(dt);
                    TryContactDamage();
                    break;

                case EnemyArchetype.Flyer:
                case EnemyArchetype.Elite:
                case EnemyArchetype.Boss:
                    Chase3D(dt);
                    TryContactDamage();
                    break;

                case EnemyArchetype.Turret:
                    TurretBehavior();
                    break;
            }
        }

        private void TrackPlayerVelocity(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            Vector3 playerPosition = _player.position;
            _playerVelocity = (playerPosition - _lastPlayerPosition) / dt;
            _lastPlayerPosition = playerPosition;
        }

        /// <summary>지상형: 수평면 추적. 고도는 스폰 시 지면 높이를 유지한다.</summary>
        private void ChaseFlat(float dt)
        {
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            Move(toPlayer, dt, flattenSeparation: true);
        }

        /// <summary>공중형: 3D 추적 — 공중 안전지대를 박탈한다 (GDD 5.2-3).</summary>
        private void Chase3D(float dt)
        {
            Vector3 toPlayer = _player.position - transform.position;
            Move(toPlayer, dt, flattenSeparation: false);
        }

        private void Move(Vector3 toPlayer, float dt, bool flattenSeparation)
        {
            if (Time.frameCount % SteeringSliceFrames == _steerPhase)
            {
                RecomputeSeparation(flattenSeparation);
            }

            Vector3 chase = toPlayer.sqrMagnitude > 1e-4f ? toPlayer.normalized : Vector3.zero;
            Vector3 direction = Steering.CombineChaseAndSeparation(chase, _separation, SeparationWeight);
            direction = AvoidObstacles(direction, flattenSeparation);

            float speedFactor = IsEmpAffected ? _empSlowFactor : 1f;
            transform.position += direction * (Data.MoveSpeed * speedFactor * dt);

            if (direction.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>진행 방향에 건물이 있으면 벽면을 따라 미끄러진다 (내비메시 없는 경량 회피).</summary>
        private Vector3 AvoidObstacles(Vector3 direction, bool flatten)
        {
            if (direction.sqrMagnitude < 1e-4f)
            {
                return direction;
            }

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, _obstacleCheckDistance,
                    _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return direction;
            }

            Vector3 slide = Vector3.ProjectOnPlane(direction, hit.normal);
            if (flatten)
            {
                slide.y = 0f;
            }

            if (slide.sqrMagnitude > 1e-4f)
            {
                return slide.normalized;
            }

            // 정면 수직 충돌 — 벽과 나란한 방향으로 회피.
            return Vector3.Cross(hit.normal, Vector3.up).normalized;
        }

        private void RecomputeSeparation(bool flatten)
        {
            Vector3 acc = Vector3.zero;
            Vector3 self = transform.position;
            float radius = Data.SeparationRadius;

            for (int i = 0; i < Active.Count; i++)
            {
                EnemyBrain other = Active[i];
                if (other == this)
                {
                    continue;
                }

                acc += Steering.PairSeparation(self, other.transform.position, radius);
            }

            if (flatten)
            {
                acc.y = 0f;
            }

            _separation = acc;
        }

        private void TryContactDamage()
        {
            float contactSqr = Data.ContactRadius * Data.ContactRadius;
            if ((_player.position - transform.position).sqrMagnitude > contactSqr)
            {
                return;
            }

            if (Data.SelfDestructOnContact)
            {
                DealContactDamage();
                // 자폭 — 처치가 아니므로 경험치 이벤트 없이 회수한다.
                Release();
                return;
            }

            if (Time.time >= _nextContactTime)
            {
                DealContactDamage();
                _nextContactTime = Time.time + Data.ContactInterval;
            }
        }

        private void DealContactDamage()
        {
            if (_playerDamageable != null && _playerDamageable.IsAlive)
            {
                Vector3 direction = (_player.position - transform.position).normalized;
                _playerDamageable.TakeDamage(Data.ContactDamage,
                    new DamageInfo(_player.position, direction));
            }
        }

        /// <summary>포탑형: 고정 위치에서 사거리 내 플레이어에게 예측 사격 (GDD 5.3).</summary>
        private void TurretBehavior()
        {
            Vector3 toPlayer = _player.position - transform.position;
            if (toPlayer.sqrMagnitude > Data.AttackRange * Data.AttackRange)
            {
                return;
            }

            if (toPlayer.sqrMagnitude > 1e-4f)
            {
                Vector3 flat = toPlayer;
                flat.y = 0f;
                if (flat.sqrMagnitude > 1e-4f)
                {
                    transform.rotation = Quaternion.LookRotation(flat);
                }
            }

            // EMP 감전 — 사격 봉쇄 (GDD 3.4-10).
            if (Time.time < _nextAttackTime || Data.ProjectilePrefab == null || IsEmpAffected)
            {
                return;
            }

            _nextAttackTime = Time.time + Data.AttackInterval;

            Vector3 muzzle = transform.position + Vector3.up * 1.5f;
            Ballistics.TryPredictInterceptDirection(
                muzzle, _player.position, _playerVelocity, Data.ProjectileSpeed,
                out Vector3 direction);

            var projectile = (Projectile)PoolManager.Instance.Spawn(
                Data.ProjectilePrefab, muzzle, Quaternion.LookRotation(direction));
            projectile.Launch(new ProjectileLaunchData(
                direction, Data.ProjectileSpeed, Data.ProjectileDamage,
                sourceId: null, range: Data.AttackRange * 2f));
        }

        private void OnDied(Health health)
        {
            if (_released || Data == null)
            {
                return;
            }

            EventBus<EnemyKilledEvent>.Raise(
                new EnemyKilledEvent(transform.position, Data.ExpReward, Data.Id));
            Release();
        }

        /// <summary>QA/실험실: 처치 이벤트 없이 즉시 회수. 스포너 생존 카운트는 정상 반환된다.</summary>
        public void ForceRelease() => Release();

        /// <summary>스포너 카운트 반환 + 풀 회수. 사망/자폭 공통 경로.</summary>
        private void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            if (_director != null)
            {
                _director.NotifyReleased(_directorEntryIndex);
            }

            PoolManager.Instance.Despawn(this);
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            Data = null;
            _player = null;
            _playerDamageable = null;
            _director = null;
            _directorEntryIndex = -1;
            _released = true;
            _empSlowFactor = 1f;
            _empEndTime = 0f;
        }
    }
}
