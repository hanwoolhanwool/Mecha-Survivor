using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private readonly List<Component> _despawnBuffer = new();
        private bool _ownsGlobalState;

        // 전역 풀은 씬 전환에도 유지되어야 스폰된 오브젝트/풀 매핑이 끊기지 않는다.
        protected override bool Persistent => true;

        protected override void Awake()
        {
            base.Awake();

            // 중복 인스턴스는 base.Awake에서 파괴 예약됨 — 전역 상태를 건드리면 안 된다.
            if (Instance != this)
            {
                return;
            }

            _ownsGlobalState = true;
            ServiceLocator.Register(this);
            // 풀 자체는 씬 전환에 생존하지만, 활성 인스턴스(적/투사체/픽업)까지
            // 살아남으면 재시작 시 이전 런의 잔존물이 남는다 — 씬이 내려갈 때 전량 회수.
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            DespawnAllActive();
        }

        /// <summary>밖에 나가 있는 모든 활성 인스턴스를 풀로 회수한다. 씬 전환/런 재시작 시 잔존 방지.</summary>
        public void DespawnAllActive()
        {
            if (_instanceToPool.Count == 0)
            {
                return;
            }

            _despawnBuffer.Clear();
            foreach (var kvp in _instanceToPool)
            {
                _despawnBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _despawnBuffer.Count; i++)
            {
                Component instance = _despawnBuffer[i];
                if (instance != null && _instanceToPool.TryGetValue(instance, out var pool))
                {
                    pool.Release(instance);
                }
            }

            _instanceToPool.Clear();
            _despawnBuffer.Clear();
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

            // 파괴된 중복 인스턴스가 진짜 인스턴스의 전역 등록을 지우면 안 된다.
            if (_ownsGlobalState)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                ServiceLocator.Unregister<PoolManager>();
            }

            base.OnDestroy();
        }
    }
}
