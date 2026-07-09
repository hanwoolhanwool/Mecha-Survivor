using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Utilities;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 프리팹별 풀을 관리하는 전역 시스템. 적/투사체/픽업/데미지 텍스트 등
    /// 자주 생성·소멸하는 오브젝트는 Instantiate/Destroy 대신 이 시스템을 통한다.
    /// Despawn은 인스턴스만으로 동작하도록 내부에서 인스턴스→풀 매핑을 관리한다.
    /// </summary>
    public sealed class PoolManager : MonoSingleton<PoolManager>
    {
        private readonly Dictionary<Component, ComponentPool<Component>> _prefabToPool = new();
        private readonly Dictionary<Component, ComponentPool<Component>> _instanceToPool = new();

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register(this);
        }

        /// <summary>필요 시 미리 풀을 만들고 prewarm 개수만큼 생성해 둔다.</summary>
        public void Prewarm(Component prefab, int count)
        {
            GetOrCreatePool(prefab, count);
        }

        public Component Spawn(Component prefab, Vector3 position, Quaternion rotation)
        {
            ComponentPool<Component> pool = GetOrCreatePool(prefab, 0);
            Component instance = pool.Get(position, rotation);
            _instanceToPool[instance] = pool;
            return instance;
        }

        public void Despawn(Component instance)
        {
            if (_instanceToPool.TryGetValue(instance, out var pool))
            {
                pool.Release(instance);
                _instanceToPool.Remove(instance);
            }
            else
            {
                // 풀 소속이 아니면 일반 파괴로 폴백.
                Destroy(instance.gameObject);
            }
        }

        private ComponentPool<Component> GetOrCreatePool(Component prefab, int prewarm)
        {
            if (!_prefabToPool.TryGetValue(prefab, out var pool))
            {
                pool = new ComponentPool<Component>(prefab, prewarm, parent: transform);
                _prefabToPool[prefab] = pool;
            }

            return pool;
        }

        protected override void OnDestroy()
        {
            foreach (var pool in _prefabToPool.Values)
            {
                pool.Clear();
            }

            _prefabToPool.Clear();
            _instanceToPool.Clear();
            ServiceLocator.Unregister<PoolManager>();
            base.OnDestroy();
        }
    }
}
