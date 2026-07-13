using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 적 사망 이벤트를 구독해 경험치 젬을 드롭한다.
    /// 전투 코드(EnemyBrain)는 드롭 시스템의 존재를 모른다 — 이벤트만 올린다.
    /// </summary>
    public sealed class GemSpawner : MonoBehaviour
    {
        [SerializeField] private ExperienceGem _gemPrefab;
        [SerializeField] private Transform _player;

        [Tooltip("드롭 위치를 살짝 흩뿌려 젬이 겹쳐 보이지 않게 한다")]
        [SerializeField] private float _scatterRadius = 0.5f;

        private void OnEnable()
        {
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        }

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (_gemPrefab == null || evt.ExpReward <= 0)
            {
                return;
            }

            Vector2 scatter = Random.insideUnitCircle * _scatterRadius;
            Vector3 position = evt.Position + new Vector3(scatter.x, 0.5f, scatter.y);

            var gem = (ExperienceGem)PoolManager.Instance.Spawn(
                _gemPrefab, position, Quaternion.identity);
            gem.Init(evt.ExpReward, _player);
        }
    }
}
