using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 상승·팽창하는 풀링 VFX — 궤도 폭격의 버섯구름 (GDD 3.4-8).
    /// PooledVfx(제자리 스케일 팝)와 달리 수명 동안 위로 떠오르며 커진다.
    /// 판정과 무관한 순수 연출 (GDD 3.6 규칙 7).
    /// </summary>
    public sealed class MushroomCloudVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _lifetime = 1.4f;

        [Tooltip("수명 동안 떠오르는 높이(m)")]
        [SerializeField] private float _riseHeight = 12f;

        [Tooltip("수명 동안 시작 스케일 → 이 배수까지 팽창")]
        [SerializeField] private float _scaleGrowth = 2.4f;

        private Vector3 _baseScale;
        private Vector3 _spawnPosition;
        private float _startTime;
        private bool _active;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            float t = (Time.time - _startTime) / _lifetime;
            if (t >= 1f)
            {
                PoolManager.Instance.Despawn(this);
                return;
            }

            // 초반에 빠르게 솟구치고 끝으로 갈수록 느려진다 (폭발의 감속감).
            float rise = 1f - (1f - t) * (1f - t);
            transform.position = _spawnPosition + Vector3.up * (_riseHeight * rise);
            transform.localScale = _baseScale * Mathf.Lerp(1f, _scaleGrowth, t);
        }

        public void OnSpawnedFromPool()
        {
            _spawnPosition = transform.position;
            _startTime = Time.time;
            _active = true;
        }

        public void OnReturnedToPool()
        {
            _active = false;
            transform.localScale = _baseScale;
        }
    }
}
