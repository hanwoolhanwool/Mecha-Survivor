using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>투사체 발사 파라미터. Weapon이 채워서 Launch에 넘긴다.</summary>
    public readonly struct ProjectileLaunchData
    {
        public readonly Vector3 Direction;
        public readonly float Speed;
        public readonly float Damage;
        public readonly string SourceId;
        public readonly float Range;
        public readonly float HomingTurnRate;
        public readonly Transform HomingTarget;
        public readonly GameObject ImpactVfxPrefab;

        public ProjectileLaunchData(Vector3 direction, float speed, float damage, string sourceId,
            float range, float homingTurnRate = 0f, Transform homingTarget = null,
            GameObject impactVfxPrefab = null)
        {
            Direction = direction;
            Speed = speed;
            Damage = damage;
            SourceId = sourceId;
            Range = range;
            HomingTurnRate = homingTurnRate;
            HomingTarget = homingTarget;
            ImpactVfxPrefab = impactVfxPrefab;
        }
    }

    /// <summary>
    /// 풀링 투사체. 물리 대신 프레임당 레이캐스트 스테핑으로 명중을 판정한다
    /// (고속 탄 터널링 방지 + 물리 비용 제거). 판정은 코드가, 화려함은 VFX가 (GDD 3.6-7).
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Tooltip("Enemy(9), Wall(13), Ground(14) — 플레이어 투사체 기본값")]
        [SerializeField] private LayerMask _hitMask = (1 << 9) | (1 << 13) | (1 << 14);

        protected Vector3 Velocity;
        private float _damage;
        private string _sourceId;
        private float _distanceRemaining;
        private float _homingTurnRate;
        private Transform _homingTarget;

        protected float Damage => _damage;
        protected string SourceId => _sourceId;
        protected Transform HomingTarget => _homingTarget;
        private GameObject _impactVfxPrefab;
        private bool _live;
        private Vector3 _defaultScale;
        private bool _defaultScaleCached;
        private TrailRenderer _trail;

        private void Awake()
        {
            _defaultScale = transform.localScale;
            _defaultScaleCached = true;
            _trail = GetComponent<TrailRenderer>();
        }

        public virtual void Launch(in ProjectileLaunchData data)
        {
            Velocity = data.Direction.normalized * data.Speed;
            _damage = data.Damage;
            _sourceId = data.SourceId;
            _distanceRemaining = data.Range;
            _homingTurnRate = data.HomingTurnRate;
            _homingTarget = data.HomingTarget;
            _impactVfxPrefab = data.ImpactVfxPrefab;
            _live = true;

            if (Velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(Velocity);
            }
        }

        protected virtual void Update()
        {
            if (!_live)
            {
                return;
            }

            float dt = Time.deltaTime;
            Steer(dt);

            Vector3 position = transform.position;
            float stepLength = Velocity.magnitude * dt;

            if (stepLength > 0f)
            {
                Vector3 direction = Velocity / Velocity.magnitude;
                if (Physics.Raycast(position, direction, out RaycastHit hit, stepLength,
                        _hitMask, QueryTriggerInteraction.Ignore))
                {
                    OnImpact(hit);
                    return;
                }

                transform.position = position + direction * stepLength;
                transform.rotation = Quaternion.LookRotation(direction);
            }

            _distanceRemaining -= stepLength;
            if (_distanceRemaining <= 0f)
            {
                Expire();
            }
        }

        /// <summary>유도 조향. 기본은 타깃을 향해 초당 일정 각도로 선회.</summary>
        protected virtual void Steer(float deltaTime)
        {
            if (_homingTurnRate <= 0f || _homingTarget == null || !_homingTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            Vector3 toTarget = _homingTarget.position - transform.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 desired = toTarget.normalized * Velocity.magnitude;
            Velocity = Vector3.RotateTowards(
                Velocity, desired, _homingTurnRate * Mathf.Deg2Rad * deltaTime, 0f);
        }

        protected void OnImpact(in RaycastHit hit)
        {
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(_damage,
                    new DamageInfo(hit.point, Velocity.normalized, false, _sourceId));
                EventBus<DamageDealtEvent>.Raise(
                    new DamageDealtEvent(_sourceId, _damage, hit.point, !damageable.IsAlive));
            }

            OnImpactVisuals(hit.point, hit.normal);
            Expire();
        }

        protected virtual void OnImpactVisuals(Vector3 point, Vector3 normal)
        {
            if (_impactVfxPrefab != null && _impactVfxPrefab.TryGetComponent(out PooledVfx vfx))
            {
                PoolManager.Instance.Spawn(vfx, point, Quaternion.LookRotation(normal));
            }
        }

        protected void Expire()
        {
            if (!_live)
            {
                return;
            }

            _live = false;
            OnExpire(transform.position);
            PoolManager.Instance.Despawn(this);
        }

        /// <summary>수명 종료(명중/사거리 소진) 지점 훅 — 그래비티 웰 등 필드 스폰용.</summary>
        protected virtual void OnExpire(Vector3 position) { }

        public virtual void OnSpawnedFromPool()
        {
            // 풀 재사용 시 지난 위치에서 이어지는 트레일 줄무늬 방지.
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public virtual void OnReturnedToPool()
        {
            _live = false;
            _homingTarget = null;
            _impactVfxPrefab = null;

            // 무기 레벨에 따라 커진 스케일이 다음 사용자에게 새지 않도록 복원.
            if (_defaultScaleCached)
            {
                transform.localScale = _defaultScale;
            }
        }
    }
}
