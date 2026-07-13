using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 미사일 포드 (GDD 3.4 — v1 우선순위 1, Burst).
    /// 발사 수·시차는 WeaponData(ProjectilesPerShot/PerLevel, StaggerInterval)로,
    /// 4단 연출은 MissileProjectile이 담당한다. 스태거 발사가 곧 스태거 착탄("두두두둥")이 된다.
    /// </summary>
    public sealed class MissilePodWeapon : ProjectileWeapon
    {
        protected override void FireOne(MechaAimer aimer)
        {
            if (Data.ProjectilePrefab == null)
            {
                return;
            }

            Vector3 origin = Muzzle.position;

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
