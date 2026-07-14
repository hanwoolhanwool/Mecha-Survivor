using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 순간 라인 섬광 (레이저 펄스·레일건 궤적·EMP 체인 아크). 풀링 필수 (GDD 3.6-5).
    /// 자기 트랜스폼이 곧 시각(원통 메시, Y축 길이 ±1 규약 — BeamWeapon과 동일).
    /// 수명 동안 폭이 줄며 사라진다 — "공기가 갈라진 흰 선"의 잔상.
    /// </summary>
    public sealed class LineFlashVfx : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _lifetime = 0.09f;

        private float _endTime;
        private float _width;
        private float _halfLength;

        /// <summary>시작→끝을 잇는 라인으로 배치하고 수명을 시작한다.</summary>
        public void Show(Vector3 start, Vector3 end, float width)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                PoolManager.Instance.Despawn(this);
                return;
            }

            _width = width;
            _halfLength = length * 0.5f;
            _endTime = Time.time + _lifetime;

            transform.SetPositionAndRotation(
                start + delta * 0.5f,
                Quaternion.LookRotation(delta / length) * Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = new Vector3(width, _halfLength, width);
        }

        private void Update()
        {
            float remaining = _endTime - Time.time;
            if (remaining <= 0f)
            {
                PoolManager.Instance.Despawn(this);
                return;
            }

            // 폭만 얇아진다 — 길이가 줄면 명중 지점이 거짓말을 한다.
            float w = _width * (remaining / _lifetime);
            transform.localScale = new Vector3(w, _halfLength, w);
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            transform.localScale = Vector3.one;
        }
    }
}
