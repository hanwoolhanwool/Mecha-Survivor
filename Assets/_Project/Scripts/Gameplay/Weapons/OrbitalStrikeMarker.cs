using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 궤도 폭격 조준 마커 (GDD 3.4-8). 지상에 찍힌 뒤 딜레이가 지나면
    /// 하늘에서 빛기둥이 떨어지며 반경 폭발한다.
    /// 적의 공격 예고처럼 이 마커도 예고다 — 딜레이 동안 링이 수축하며 "온다"를 알린다.
    /// </summary>
    public sealed class OrbitalStrikeMarker : MonoBehaviour, IPoolable
    {
        [Header("시각")]
        [Tooltip("낙하 순간 스폰할 빛기둥 VFX (풀링)")]
        [SerializeField] private PooledVfx _pillarVfxPrefab;

        [Tooltip("착탄 후 피어오르는 버섯구름 VFX (풀링)")]
        [SerializeField] private MushroomCloudVfx _cloudVfxPrefab;

        [Tooltip("HeavyImpact 연출 강도(0~1) — 화면 플래시·셰이크에 곱해진다")]
        [Range(0f, 1f)]
        [SerializeField] private float _impactMagnitude = 1f;

        [Tooltip("마커 링 시각 — 자식 트랜스폼. 딜레이 동안 반경 크기 → 0.3배로 수축")]
        [SerializeField] private Transform _ring;

        private float _strikeTime;
        private float _delay;
        private float _radius;
        private float _damage;
        private string _sourceId;
        private bool _armed;

        /// <summary>마커 가동 — delay 후 반경 damage 폭발.</summary>
        public void Arm(float delay, float radius, float damage, string sourceId)
        {
            _delay = Mathf.Max(0.05f, delay);
            _strikeTime = Time.time + _delay;
            _radius = radius;
            _damage = damage;
            _sourceId = sourceId;
            _armed = true;

            UpdateRing(1f);
        }

        private void Update()
        {
            if (!_armed)
            {
                return;
            }

            float remaining = _strikeTime - Time.time;
            if (remaining > 0f)
            {
                UpdateRing(remaining / _delay);
                return;
            }

            Strike();
        }

        private void Strike()
        {
            _armed = false;

            if (_pillarVfxPrefab != null)
            {
                PoolManager.Instance.Spawn(_pillarVfxPrefab, transform.position, Quaternion.identity);
            }

            if (_cloudVfxPrefab != null)
            {
                PoolManager.Instance.Spawn(_cloudVfxPrefab, transform.position, Quaternion.identity);
            }

            // 화면 플래시·카메라 셰이크는 이 이벤트를 구독한다 — 마커는 그들의 존재를 모른다.
            EventBus<HeavyImpactEvent>.Raise(new HeavyImpactEvent(transform.position, _impactMagnitude));

            AreaDamage.Apply(transform.position, _radius, _damage, _sourceId);
            PoolManager.Instance.Despawn(this);
        }

        /// <summary>t: 1(방금 찍힘) → 0(낙하 직전). 링이 수축하며 긴박감을 만든다.</summary>
        private void UpdateRing(float t)
        {
            if (_ring != null)
            {
                float scale = _radius * 2f * Mathf.Lerp(0.3f, 1f, t);
                _ring.localScale = new Vector3(scale, _ring.localScale.y, scale);
            }
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            _armed = false;
        }
    }
}
