using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 궤도 폭격 (GDD 3.4 무기 8번 — Finisher). 조준 지점의 지면에 마커를 찍으면
    /// 딜레이 뒤 빛기둥이 떨어진다. 마커 수는 레벨 성장(GetProjectileCount) —
    /// Lv.5 융단 폭격은 조준 방향 일직선으로 시차를 두고 연쇄 낙하한다.
    /// </summary>
    public sealed class OrbitalStrikeWeapon : Weapon
    {
        [Header("마커")]
        [SerializeField] private OrbitalStrikeMarker _markerPrefab;

        [Tooltip("마커가 찍힌 뒤 낙하까지의 예고 시간(초) — GDD: 1초")]
        [SerializeField] private float _strikeDelay = 1f;

        [Tooltip("융단 폭격 마커 간 추가 시차(초) — 두두두둥")]
        [SerializeField] private float _carpetInterval = 0.2f;

        [Tooltip("융단 폭격 마커 간격(m)")]
        [SerializeField] private float _markerSpacing = 7f;

        [Header("폭발")]
        [Tooltip("기둥 폭발 반경 — VisualScale을 곱해 최종 반경")]
        [SerializeField] private float _strikeRadius = 6f;

        [Header("지면 탐색")]
        [Tooltip("Wall(13), Ground(14)")]
        [SerializeField] private LayerMask _groundMask = (1 << 13) | (1 << 14);

        protected override void Fire(MechaAimer aimer)
        {
            if (_markerPrefab == null)
            {
                return;
            }

            Vector3 aimPoint = aimer != null
                ? aimer.AimPoint
                : Muzzle.position + Muzzle.forward * 40f;
            Vector3 forward = aimPoint - Muzzle.position;

            int markers = Mathf.Max(1, Data.GetProjectileCount(Level));
            float radius = _strikeRadius * Data.GetVisualScale(Level);
            float damage = Data.GetDamage(Level);

            for (int i = 0; i < markers; i++)
            {
                Vector3 ground = FindGround(OrbitalMath.MarkerPosition(aimPoint, forward, i, _markerSpacing));

                var marker = (OrbitalStrikeMarker)PoolManager.Instance.Spawn(
                    _markerPrefab, ground, Quaternion.identity);
                marker.Arm(_strikeDelay + _carpetInterval * i, radius, damage, Data.Id);
            }
        }

        /// <summary>공중 조준 지점을 지면에 투영한다. 지면이 없으면 y=0 평면으로.</summary>
        private Vector3 FindGround(Vector3 point)
        {
            Vector3 origin = point + Vector3.up * 100f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 300f,
                    _groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            point.y = 0f;
            return point;
        }
    }
}
