using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 적 사망 지점에 폭발 VFX를 스폰한다 — 처치가 눈에 보여야 쾌감이 성립한다 (GDD 3.4).
    /// 전투 코드는 이 시스템의 존재를 모른다 — 이벤트만 구독.
    /// </summary>
    public sealed class DeathVfxSpawner : MonoBehaviour
    {
        [SerializeField] private PooledVfx _explosionPrefab;

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
            if (_explosionPrefab != null)
            {
                PoolManager.Instance.Spawn(
                    _explosionPrefab, evt.Position + Vector3.up * 0.8f, Quaternion.identity);
            }
        }
    }
}
