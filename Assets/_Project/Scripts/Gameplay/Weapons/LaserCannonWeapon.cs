using System.Collections;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 레이저 캐논 (GDD 3.4 무기 2번 — Sustain). 짧은 펄스를 "따다다닥" 연발하는 히트스캔 —
    /// 즉시 명중이라 조준이 편하다. 강화 시 동시 발사 라인이 부채꼴로 확산된다 (2→4갈래).
    /// Damage는 펄스 1발·라인 1갈래당 피해, StaggerInterval은 펄스 간격으로 해석한다.
    /// </summary>
    public sealed class LaserCannonWeapon : Weapon
    {
        [Header("펄스 연발 — 따다다닥")]
        [Tooltip("발사 1회당 펄스 수")]
        [SerializeField] private int _pulsesPerBurst = 4;

        [Header("부채꼴 확산 (강화 성장)")]
        [Tooltip("라인이 2갈래 이상일 때 전체 확산 각도(도)")]
        [SerializeField] private float _fanAngleDegrees = 14f;

        [Header("시각")]
        [SerializeField] private LineFlashVfx _pulseVfxPrefab;

        [Tooltip("펄스 라인 기본 폭 — VisualScale을 곱해 최종 폭")]
        [SerializeField] private float _baseWidth = 0.18f;

        [Tooltip("Enemy(9), Wall(13), Ground(14)")]
        [SerializeField] private LayerMask _hitMask = (1 << 9) | (1 << 13) | (1 << 14);

        protected override void Fire(MechaAimer aimer)
        {
            if (Data.StaggerInterval > 0f && _pulsesPerBurst > 1)
            {
                StartCoroutine(PulseBurst(aimer));
                return;
            }

            for (int i = 0; i < Mathf.Max(1, _pulsesPerBurst); i++)
            {
                FirePulse(aimer);
            }
        }

        private IEnumerator PulseBurst(MechaAimer aimer)
        {
            var wait = new WaitForSeconds(Data.StaggerInterval);
            for (int i = 0; i < _pulsesPerBurst; i++)
            {
                FirePulse(aimer);
                if (i < _pulsesPerBurst - 1)
                {
                    yield return wait;
                }
            }
        }

        /// <summary>펄스 1발 — 라인 수만큼 부채꼴 히트스캔. 판정과 시각을 함께 처리한다.</summary>
        private void FirePulse(MechaAimer aimer)
        {
            Vector3 origin = Muzzle.position;
            Vector3 forward = aimer != null ? aimer.FireDirectionFrom(origin) : Muzzle.forward;

            // 부채꼴 회전축: 수직으로 쏠 때도 수평 확산을 유지하도록 시선과 직교하는 위 방향.
            Vector3 axis = Vector3.Cross(forward, Vector3.Cross(Vector3.up, forward));
            if (axis.sqrMagnitude < 1e-4f)
            {
                axis = Vector3.up;
            }
            else
            {
                axis.Normalize();
            }

            int lines = Mathf.Max(1, Data.GetProjectileCount(Level));
            float damage = Data.GetDamage(Level);
            float width = _baseWidth * Data.GetVisualScale(Level);

            for (int i = 0; i < lines; i++)
            {
                Vector3 direction = HitscanMath.FanDirection(forward, axis, i, lines, _fanAngleDegrees);
                Vector3 end = origin + direction * Data.Range;

                if (Physics.Raycast(origin, direction, out RaycastHit hit, Data.Range,
                        _hitMask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;

                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null && damageable.IsAlive)
                    {
                        damageable.TakeDamage(damage,
                            new DamageInfo(hit.point, direction, false, Data.Id));
                        EventBus<DamageDealtEvent>.Raise(
                            new DamageDealtEvent(Data.Id, damage, hit.point, !damageable.IsAlive));
                    }

                    // 맞은 자리의 작은 스파크 (GDD 3.4-2).
                    if (Data.ImpactVfxPrefab != null &&
                        Data.ImpactVfxPrefab.TryGetComponent(out PooledVfx impact))
                    {
                        PoolManager.Instance.Spawn(impact, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }

                if (_pulseVfxPrefab != null)
                {
                    var flash = (LineFlashVfx)PoolManager.Instance.Spawn(
                        _pulseVfxPrefab, origin, Quaternion.identity);
                    flash.Show(origin, end, width);
                }
            }
        }
    }
}
