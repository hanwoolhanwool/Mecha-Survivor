using System.Collections;
using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 표준 투사체 무기. 발사 수·퍼짐·시차(스태거)를 WeaponData로 조절한다.
    /// 시차 발사는 "펑" 한 번 대신 "두두두둥"을 만든다 (GDD 3.4 미사일 트릭 ④).
    /// </summary>
    public class ProjectileWeapon : Weapon
    {
        protected override void Fire(MechaAimer aimer)
        {
            int count = Mathf.Max(1, Data.GetProjectileCount(Level));

            if (Data.StaggerInterval > 0f && count > 1)
            {
                StartCoroutine(FireStaggered(aimer, count));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    FireOne(aimer);
                }
            }
        }

        private IEnumerator FireStaggered(MechaAimer aimer, int count)
        {
            var wait = new WaitForSeconds(Data.StaggerInterval);
            for (int i = 0; i < count; i++)
            {
                FireOne(aimer);
                if (i < count - 1)
                {
                    yield return wait;
                }
            }
        }

        /// <summary>탄 1발 스폰. 발사 방향은 언제나 조준선(크로스헤어) 기준 (GDD 2.3).</summary>
        protected virtual void FireOne(MechaAimer aimer)
        {
            if (Data.ProjectilePrefab == null)
            {
                return;
            }

            Vector3 origin = Muzzle.position;
            Vector3 direction = aimer != null
                ? aimer.FireDirectionFrom(origin)
                : Muzzle.forward;

            // 총구 화염 — 발사가 눈에 보이게 (풀 경유).
            if (Data.MuzzleVfxPrefab != null &&
                Data.MuzzleVfxPrefab.TryGetComponent(out PooledVfx muzzleVfx))
            {
                PoolManager.Instance.Spawn(muzzleVfx, origin, Quaternion.LookRotation(direction));
            }

            direction = ApplySpread(direction, Data.SpreadAngle);

            var projectile = (Projectile)PoolManager.Instance.Spawn(
                Data.ProjectilePrefab, origin, Quaternion.LookRotation(direction));

            float homingTurnRate = Data.GetHomingTurnRate(Level);
            Transform homingTarget = null;
            if (homingTurnRate > 0f && aimer != null && aimer.HasHit && aimer.HitCollider != null)
            {
                homingTarget = aimer.HitCollider.transform;
            }

            projectile.transform.localScale = Vector3.one * Data.GetVisualScale(Level);
            projectile.Launch(new ProjectileLaunchData(
                direction, Data.ProjectileSpeed, Data.GetDamage(Level), Data.Id, Data.Range,
                homingTurnRate, homingTarget, Data.ImpactVfxPrefab));
            projectile.ConfigureFromWeapon(Data, Level);
        }

        protected static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f)
            {
                return direction;
            }

            Quaternion spread = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle) * 0.5f,
                Random.Range(-spreadAngle, spreadAngle) * 0.5f,
                0f);
            return Quaternion.LookRotation(direction) * spread * Vector3.forward;
        }
    }
}
