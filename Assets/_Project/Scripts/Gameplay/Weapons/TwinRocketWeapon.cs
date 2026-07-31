using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 트윈 로켓 — 미사일 포드의 자매 무기. 포드 좌/우 입구(MissilePodRack.LaunchPorts)에서
    /// 교대로 직사출한 뒤 곡선 유도로 꺾인다. 미사일 포드(수직 팝업·장전 소비)는 그대로 두고
    /// 새 발사 방식으로 추가한 무기다. 랙이 없는 씬에서는 기존 수직 팝업으로 폴백한다.
    /// </summary>
    public sealed class TwinRocketWeapon : ProjectileWeapon
    {
        private MissilePodRack _rack;
        private bool _rackSearched;
        private int _portIndex;

        private void OnDisable()
        {
            // 풀 회수 대비 — 다음 장착 기체에서 랙을 다시 찾는다.
            _rack = null;
            _rackSearched = false;
            _portIndex = 0;
        }

        protected override void FireOne(MechaAimer aimer)
        {
            if (Data.ProjectilePrefab == null)
            {
                return;
            }

            if (!_rackSearched)
            {
                // 장착(부모 확정) 후 첫 발사 시점에 1회 탐색.
                _rack = transform.root.GetComponentInChildren<MissilePodRack>(includeInactive: true);
                _rackSearched = true;
            }

            Transform port = null;
            if (_rack != null && _rack.TryGetLaunchPort(_portIndex, out port))
            {
                _portIndex++;   // 좌/우 교대
            }

            Vector3 origin = port != null ? port.position : Muzzle.position;

            Component spawned = PoolManager.Instance.Spawn(
                Data.ProjectilePrefab, origin,
                Quaternion.LookRotation(port != null ? port.forward : Vector3.up));

            if (spawned is not MissileProjectile missile)
            {
                // 프리팹이 MissileProjectile이 아니면 회수 후 일반 발사로 폴백.
                PoolManager.Instance.Despawn(spawned);
                base.FireOne(aimer);
                return;
            }

            Vector3 aimPoint = aimer != null
                ? aimer.AimPoint
                : origin + (port != null ? port.forward : Muzzle.forward) * Data.Range;

            Transform homingTarget = null;
            if (aimer != null && aimer.HasHit && aimer.HitCollider != null)
            {
                homingTarget = aimer.HitCollider.transform;
            }

            missile.transform.localScale = Vector3.one * Data.GetVisualScale(Level);
            var launchData = new ProjectileLaunchData(
                Vector3.up, Data.ProjectileSpeed, Data.GetDamage(Level), Data.Id, Data.Range,
                Data.GetHomingTurnRate(Level), homingTarget, Data.ImpactVfxPrefab);

            if (port != null)
            {
                missile.LaunchMissileFrom(launchData, aimPoint, port.forward);
            }
            else
            {
                missile.LaunchMissile(launchData, aimPoint);   // 랙 없는 씬 — 기존 수직 팝업
            }

            if (_rack != null)
            {
                _rack.PlayFire();
            }
        }
    }
}
