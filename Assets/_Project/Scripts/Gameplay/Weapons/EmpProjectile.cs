using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// EMP 투척체 (GDD 3.4 무기 10번). 기폭 거리 도달/명중 지점에 EMP 필드를 편다 —
    /// 그래비티 웰 투척체와 같은 패턴. 무기 쪽은 ProjectileWeapon 그대로 쓴다.
    /// 체인 감전은 Lv.5 해금이라 ConfigureFromWeapon으로 레벨을 받는다.
    /// </summary>
    public sealed class EmpProjectile : Projectile
    {
        [SerializeField] private EmpField _fieldPrefab;

        [Header("기폭")]
        [Tooltip("이 거리를 날아가면 명중 없이도 기폭한다. 0 이하면 무기 사거리를 그대로 쓴다.")]
        [SerializeField] private float _detonationDistance = 25f;
        [SerializeField] private EmpOrbBurstVfx _burstVfxPrefab;

        [Tooltip("오브 반경(로컬 단위) — 이 굵기로 스윕해 몸통에 닿기만 해도 기폭한다. " +
                 "0이면 중심선 판정. 눈에 보이는 오브 크기(반경 0.5)에 맞춘 값이 기본.")]
        [SerializeField] private float _contactRadius = 0.5f;

        [Header("EMP 필드")]
        [SerializeField] private float _fieldRadius = 9f;
        [SerializeField] private float _fieldDuration = 4f;

        [Header("Lv.5 — 체인 감전")]
        [SerializeField] private int _chainUnlockLevel = 5;

        private bool _chainEnabled;
        private float _chainDamage;

        /// <summary>기폭 사거리 계산 — 무기 사거리와 기폭 거리 중 짧은 쪽. 순수 로직(테스트용).</summary>
        public static float ClampDetonationRange(float weaponRange, float detonationDistance)
        {
            return detonationDistance > 0f
                ? Mathf.Min(weaponRange, detonationDistance)
                : weaponRange;
        }

        /// <summary>
        /// 실제 접촉 반경 — 무기 레벨에 따라 커진 오브 스케일에 비례한다. 순수 로직(테스트용).
        /// </summary>
        public static float ContactRadius(float baseRadius, float visualScale)
        {
            if (baseRadius <= 0f)
            {
                return 0f;
            }

            return baseRadius * Mathf.Max(0.01f, visualScale);
        }

        /// <summary>
        /// 오브 굵기만큼 스윕해서 접촉을 잡는다. 중심선 레이캐스트로는 보이는 것보다
        /// 훨씬 가늘게 판정돼, 오브가 적 몸통을 관통하고도 안 터지는 일이 생긴다.
        /// </summary>
        protected override bool CastStep(Vector3 origin, Vector3 direction, float distance,
            out RaycastHit hit)
        {
            float radius = ContactRadius(_contactRadius, transform.localScale.x);
            if (radius <= 0f)
            {
                return base.CastStep(origin, direction, distance, out hit);
            }

            if (!Physics.SphereCast(origin, radius, direction, out hit, distance,
                    HitMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // 스윕 시작부터 겹쳐 있으면 point/normal이 비어 오므로 보정한다.
            if (hit.distance <= 0f)
            {
                hit.point = origin;
                hit.normal = -direction;
            }

            return true;
        }

        public override void Launch(in ProjectileLaunchData data)
        {
            // 사거리를 기폭 거리로 줄여, 그 지점 도달 시(또는 그 전 명중 시) 터지게 한다.
            base.Launch(new ProjectileLaunchData(
                data.Direction, data.Speed, data.Damage, data.SourceId,
                ClampDetonationRange(data.Range, _detonationDistance),
                data.HomingTurnRate, data.HomingTarget, data.ImpactVfxPrefab));
        }

        public override void ConfigureFromWeapon(WeaponData data, int level)
        {
            _chainEnabled = level >= _chainUnlockLevel;
            _chainDamage = data.GetDamage(level);
        }

        protected override void OnExpire(Vector3 position)
        {
            // 기폭 연출 — 중심 몸체는 남고 주변 파츠가 사방으로 퍼진다.
            if (_burstVfxPrefab != null)
            {
                var burst = (EmpOrbBurstVfx)PoolManager.Instance.Spawn(
                    _burstVfxPrefab, position, transform.rotation);
                burst.transform.localScale = transform.localScale;
            }

            if (_fieldPrefab == null)
            {
                return;
            }

            var field = (EmpField)PoolManager.Instance.Spawn(
                _fieldPrefab, position, Quaternion.identity);

            // 무기 레벨(투척체 스케일)에 비례해 필드 반경 확대 — 그래비티 웰과 같은 성장.
            float scale = Mathf.Max(transform.localScale.x, 0.1f);
            field.Activate(_fieldRadius * scale, _fieldDuration, _chainEnabled, _chainDamage, SourceId);
        }

        public override void OnReturnedToPool()
        {
            base.OnReturnedToPool();
            _chainEnabled = false;
            _chainDamage = 0f;
        }
    }
}
