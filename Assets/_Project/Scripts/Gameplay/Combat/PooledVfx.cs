using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 수명이 있는 풀링 VFX (착탄 플래시 등). 모든 VFX는 풀링한다 (GDD 3.6 규칙 5).
    /// 팽창하며 사라지는 단순 스케일 팝 — 파티클 없이도 타격 지점을 읽게 해준다.
    /// </summary>
    public sealed class PooledVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _lifetime = 0.25f;

        [Tooltip("수명 동안 시작 스케일 → 이 배수까지 팽창")]
        [SerializeField] private float _scalePop = 2.5f;

        private float _endTime;
        private Vector3 _baseScale;
        private bool _baseScaleCached;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _baseScaleCached = true;
        }

        private void OnEnable()
        {
            _endTime = Time.time + _lifetime;
        }

        private void Update()
        {
            float remaining = _endTime - Time.time;
            if (remaining <= 0f)
            {
                PoolManager.Instance.Despawn(this);
                return;
            }

            float t = 1f - remaining / _lifetime;
            transform.localScale = _baseScale * Mathf.Lerp(1f, _scalePop, t);
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            if (_baseScaleCached)
            {
                transform.localScale = _baseScale;
            }
        }
    }
}
