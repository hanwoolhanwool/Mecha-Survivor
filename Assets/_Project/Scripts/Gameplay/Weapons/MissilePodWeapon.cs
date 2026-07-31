using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 미사일 포드 (GDD 3.4 — v1 우선순위 1, Burst).
    /// 발사 수·시차는 WeaponData(ProjectilesPerShot/PerLevel, StaggerInterval)로,
    /// 4단 연출은 MissileProjectile이 담당한다. 스태거 발사가 곧 스태거 착탄("두두두둥")이 된다.
    /// 장전 연출: 기체의 MissilePodRack(포드 안 미사일 모델)에서 한 발씩 소비해 그 위치에서
    /// 발사하고, 쿨다운이 끝나면 레벨별 발사 수만큼 재장전 표시한다.
    /// </summary>
    public sealed class MissilePodWeapon : ProjectileWeapon
    {
        private MissilePodRack _rack;
        private bool _rackSearched;
        private int _lastLoaded = -1;

        private void OnDisable()
        {
            // 풀 회수 대비 — 다음 장착 기체에서 랙을 다시 찾는다.
            _rack = null;
            _rackSearched = false;
            _lastLoaded = -1;
        }

        private void Update()
        {
            if (!_rackSearched)
            {
                // 스폰 직후엔 아직 풀 루트 소속이라, 장착(부모 확정) 후 첫 프레임에 1회 탐색.
                _rack = transform.root.GetComponentInChildren<MissilePodRack>(includeInactive: true);
                _rackSearched = true;
            }

            if (_rack == null || Data == null)
            {
                return;
            }

            if (IsReady)
            {
                // 쿨다운 완료 → 레벨별 발사 수만큼 장전 표시 (레벨업 시 즉시 갱신).
                int expected = Mathf.Min(Mathf.Max(1, Data.GetProjectileCount(Level)), _rack.Capacity);
                if (_lastLoaded != expected)
                {
                    _rack.SetLoaded(expected);
                    _lastLoaded = expected;
                }
            }
            else
            {
                _lastLoaded = -1;   // 발사됨 — 다음 준비 완료 때 재장전
            }
        }

        protected override void FireOne(MechaAimer aimer)
        {
            if (Data.ProjectilePrefab == null)
            {
                return;
            }

            // 포드 안의 장전 미사일에서 발사 — 소비한 모델의 보이는 위치(렌더러 바운즈)가
            // 곧 발사 위치. 랙이 없거나(다른 씬) 초과분(레벨 발사 수 > 슬롯 12)은 총구 폴백.
            Vector3 origin = Muzzle.position;
            if (_rack != null)
            {
                if (_rack.TryConsumeNext(out _, out Vector3 firePosition))
                {
                    origin = firePosition;
                }

                _rack.PlayFire();
            }

            Component spawned = PoolManager.Instance.Spawn(
                Data.ProjectilePrefab, origin, Quaternion.LookRotation(Vector3.up));

            if (spawned is not MissileProjectile missile)
            {
                // 프리팹이 MissileProjectile이 아니면 회수 후 일반 발사로 폴백.
                PoolManager.Instance.Despawn(spawned);
                base.FireOne(aimer);
                return;
            }

            Vector3 aimPoint = aimer != null
                ? aimer.AimPoint
                : origin + Muzzle.forward * Data.Range;

            Transform homingTarget = null;
            if (aimer != null && aimer.HasHit && aimer.HitCollider != null)
            {
                homingTarget = aimer.HitCollider.transform;
            }

            missile.transform.localScale = Vector3.one * Data.GetVisualScale(Level);
            missile.LaunchMissile(new ProjectileLaunchData(
                Vector3.up, Data.ProjectileSpeed, Data.GetDamage(Level), Data.Id, Data.Range,
                Data.GetHomingTurnRate(Level), homingTarget, Data.ImpactVfxPrefab),
                aimPoint);
        }
    }
}
