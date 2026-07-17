using System.Collections;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 레일건 (GDD 3.4 무기 7번 — Finisher). 예열(ChargeTime) → 초고속 발사 → 즉시 명중.
    /// 빔과 달리 한 방, 순간. 관통 수는 레벨로 성장하고(GetProjectileCount를 관통 수로 해석),
    /// Lv.5부터 명중 지점 2차 폭발. "공기가 갈라진 흰 선"은 LineFlashVfx가 만든다.
    /// </summary>
    public sealed class RailgunWeapon : Weapon
    {
        [Header("관통")]
        [Tooltip("Enemy(9), Wall(13), Ground(14)")]
        [SerializeField] private LayerMask _hitMask = (1 << 9) | (1 << 13) | (1 << 14);

        [Header("Lv.5 — 명중 지점 2차 폭발")]
        [SerializeField] private int _explosionUnlockLevel = 5;
        [SerializeField] private float _explosionRadius = 5f;

        [Tooltip("2차 폭발 피해 배율 (본탄 피해 대비)")]
        [SerializeField] private float _explosionDamageScale = 0.6f;

        [Header("시각")]
        [SerializeField] private LineFlashVfx _railVfxPrefab;

        [Tooltip("궤적 기본 폭 — VisualScale을 곱해 최종 폭")]
        [SerializeField] private float _baseWidth = 0.35f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];
        private static readonly float[] DistanceBuffer = new float[64];
        private static readonly int[] IndexBuffer = new int[64];

        private bool _charging;

        protected override void Fire(MechaAimer aimer)
        {
            if (!_charging)
            {
                StartCoroutine(ChargeAndFire(aimer));
            }
        }

        private IEnumerator ChargeAndFire(MechaAimer aimer)
        {
            _charging = true;

            // 예열 — "무언가 온다"의 예고 (GDD 3.6-3의 예고 원칙을 아군 무기에도).
            if (Data.ChargeTime > 0f)
            {
                yield return new WaitForSeconds(Data.ChargeTime);
            }

            // 발사 순간의 조준선으로 판정 — 차징 중 조준 이동을 따라간다.
            Vector3 origin = Muzzle.position;
            Vector3 direction = aimer != null ? aimer.FireDirectionFrom(origin) : Muzzle.forward;

            FireRail(origin, direction);
            _charging = false;
        }

        private void FireRail(Vector3 origin, Vector3 direction)
        {
            // 실제 발사 순간 — 카메라 킥 등 "탄이 나가는 순간" 연출이 여기 반응한다
            // (WeaponFired는 차징 시작 시점이라 킥이 0.6초 빗나간다).
            EventBus<WeaponDischargedEvent>.Raise(new WeaponDischargedEvent(Data.Id, origin));

            if (Data.MuzzleVfxPrefab != null &&
                Data.MuzzleVfxPrefab.TryGetComponent(out PooledVfx muzzleVfx))
            {
                PoolManager.Instance.Spawn(muzzleVfx, origin, Quaternion.LookRotation(direction));
            }

            int count = Physics.RaycastNonAlloc(
                origin, direction, HitBuffer, Data.Range, _hitMask, QueryTriggerInteraction.Ignore);

            // RaycastNonAlloc은 순서를 보장하지 않는다 — 가까운 대상부터 관통해야 한다.
            for (int i = 0; i < count; i++)
            {
                DistanceBuffer[i] = HitBuffer[i].distance;
            }

            HitscanMath.SortIndicesByDistance(DistanceBuffer, count, IndexBuffer);

            float damage = Data.GetDamage(Level);
            int pierceRemaining = Mathf.Max(1, Data.GetProjectileCount(Level));
            float endDistance = Data.Range;
            Vector3 lastHitPoint = origin + direction * Data.Range;
            bool hitAnything = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[IndexBuffer[i]];
                int layer = hit.collider.gameObject.layer;

                // 벽/지면 — 여기서 궤적이 끝난다.
                if (layer == 13 || layer == 14)
                {
                    endDistance = hit.distance;
                    lastHitPoint = hit.point;
                    hitAnything = true;
                    break;
                }

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                damageable.TakeDamage(damage,
                    new DamageInfo(hit.point, direction, false, Data.Id));
                EventBus<DamageDealtEvent>.Raise(
                    new DamageDealtEvent(Data.Id, damage, hit.point, !damageable.IsAlive));

                lastHitPoint = hit.point;
                hitAnything = true;
                pierceRemaining--;

                if (pierceRemaining <= 0)
                {
                    endDistance = hit.distance;
                    break;
                }
            }

            // 궤적 — 관통이 멈춘 지점(또는 최대 사거리)까지의 흰 선.
            if (_railVfxPrefab != null)
            {
                var flash = (LineFlashVfx)PoolManager.Instance.Spawn(
                    _railVfxPrefab, origin, Quaternion.identity);
                flash.Show(origin, origin + direction * endDistance, _baseWidth * Data.GetVisualScale(Level));
            }

            if (hitAnything && Data.ImpactVfxPrefab != null &&
                Data.ImpactVfxPrefab.TryGetComponent(out PooledVfx impact))
            {
                PoolManager.Instance.Spawn(impact, lastHitPoint, Quaternion.LookRotation(-direction));
            }

            // Lv.5 — 마지막 명중 지점에서 2차 폭발.
            if (hitAnything && Level >= _explosionUnlockLevel)
            {
                AreaDamage.Apply(lastHitPoint, _explosionRadius, damage * _explosionDamageScale, Data.Id);
            }
        }

        private void OnDisable()
        {
            _charging = false;
        }
    }
}
