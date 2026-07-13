using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 경험치 젬 (GDD 4.1). 자석 반경 안에 플레이어가 들어오면 빨려가고,
    /// 획득 반경에 닿으면 ExperienceGainedEvent를 올리고 풀로 돌아간다.
    /// 물리 충돌 대신 거리 검사 — 수백 개가 떠 있어도 싸다.
    /// </summary>
    public sealed class ExperienceGem : MonoBehaviour, IPoolable
    {
        [Tooltip("이 거리 안이면 플레이어에게 끌려간다")]
        [SerializeField] private float _magnetRadius = 6f;

        [SerializeField] private float _magnetSpeed = 25f;

        [Tooltip("이 거리 안이면 획득")]
        [SerializeField] private float _collectRadius = 1.2f;

        private int _expValue;
        private Transform _player;
        private bool _magnetized;

        public void Init(int expValue, Transform player)
        {
            _expValue = expValue;
            _player = player;
            _magnetized = false;
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            Vector3 toPlayer = _player.position - transform.position;
            float sqrDistance = toPlayer.sqrMagnitude;

            if (!_magnetized)
            {
                if (sqrDistance <= _magnetRadius * _magnetRadius)
                {
                    _magnetized = true;
                }

                return;
            }

            // 한 번 자석에 걸리면 반경을 벗어나도 끝까지 따라간다.
            transform.position += toPlayer.normalized * (_magnetSpeed * Time.deltaTime);

            if (sqrDistance <= _collectRadius * _collectRadius)
            {
                EventBus<ExperienceGainedEvent>.Raise(new ExperienceGainedEvent(_expValue));
                PoolManager.Instance.Despawn(this);
            }
        }

        public void OnSpawnedFromPool() { }

        public void OnReturnedToPool()
        {
            _player = null;
            _magnetized = false;
        }
    }
}
