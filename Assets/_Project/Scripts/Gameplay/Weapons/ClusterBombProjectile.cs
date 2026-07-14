using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 클러스터 폭탄 (GDD 3.4 무기 5번 — Burst). 포물선으로 날아가다 공중에서 분열해
    /// 새끼 폭탄 다수가 지역을 융단폭격한다. Lv.5부터 새끼가 또 터지는 2차 분열.
    /// 모탄/새끼 모두 이 클래스 — 남은 분열 세대가 0이면 착탄 폭발, 남아 있으면 분열한다.
    /// 무기 쪽은 커스텀 클래스가 필요 없다 — ProjectileWeapon + ConfigureFromWeapon으로 성립.
    /// </summary>
    public sealed class ClusterBombProjectile : Projectile
    {
        [Header("포물선")]
        [SerializeField] private float _gravity = 22f;

        [Header("분열")]
        [Tooltip("새끼 폭탄 프리팹 — 보통 자기 자신의 축소판")]
        [SerializeField] private ClusterBombProjectile _bombletPrefab;

        [Tooltip("발사 후 이 시간이 지나면 공중 분열(초)")]
        [SerializeField] private float _fuseTime = 0.55f;

        [Tooltip("분열 원뿔 각도(도) — 클수록 넓게 흩어진다")]
        [SerializeField] private float _splitConeAngle = 50f;

        [SerializeField] private float _bombletSpeed = 15f;

        [Tooltip("새끼 폭탄 피해 배율 (모탄 피해 대비)")]
        [SerializeField] private float _bombletDamageScale = 0.5f;

        [Header("폭발")]
        [SerializeField] private float _explosionRadius = 3.2f;

        [Header("Lv.5 — 2차 분열")]
        [SerializeField] private int _secondSplitUnlockLevel = 5;

        private int _bombletCount = 5;
        private int _generationsRemaining;
        private float _age;

        public override void Launch(in ProjectileLaunchData data)
        {
            base.Launch(data);
            _age = 0f;
            _generationsRemaining = 0; // 새끼 기본값 — 모탄은 ConfigureFromWeapon이 올린다.
        }

        /// <summary>모탄 세팅 — 분열 수는 레벨 성장, 2차 분열은 Lv.5 해금.</summary>
        public override void ConfigureFromWeapon(WeaponData data, int level)
        {
            _bombletCount = Mathf.Max(2, data.GetProjectileCount(level));
            _generationsRemaining = level >= _secondSplitUnlockLevel ? 2 : 1;
        }

        protected override void Steer(float deltaTime)
        {
            Velocity += Vector3.down * (_gravity * deltaTime);

            _age += deltaTime;
            if (_age >= _fuseTime && _generationsRemaining > 0)
            {
                Expire(); // OnExpire가 분열을 처리한다.
            }
        }

        /// <summary>분열 세대가 남았으면 공중 분열, 아니면 반경 폭발 (착탄/사거리 공통).</summary>
        protected override void OnExpire(Vector3 position)
        {
            if (_generationsRemaining > 0 && _bombletPrefab != null)
            {
                SpawnBomblets(position);
                return;
            }

            float radius = _explosionRadius * Mathf.Max(transform.localScale.x, 0.1f);
            AreaDamage.Apply(position, radius, Damage, SourceId);
        }

        private void SpawnBomblets(Vector3 position)
        {
            int generation = _generationsRemaining - 1;
            float damage = Damage * _bombletDamageScale;

            for (int i = 0; i < _bombletCount; i++)
            {
                Vector3 direction = ClusterMath.SplitDirection(i, _bombletCount, _splitConeAngle);

                var bomblet = (ClusterBombProjectile)PoolManager.Instance.Spawn(
                    _bombletPrefab, position, Quaternion.LookRotation(direction));
                bomblet.Launch(new ProjectileLaunchData(
                    direction, _bombletSpeed, damage, SourceId, range: 120f));
                bomblet._generationsRemaining = generation;
                bomblet._bombletCount = Mathf.Max(2, _bombletCount / 2);
            }
        }

        public override void OnReturnedToPool()
        {
            base.OnReturnedToPool();
            _generationsRemaining = 0;
            _age = 0f;
        }
    }
}
