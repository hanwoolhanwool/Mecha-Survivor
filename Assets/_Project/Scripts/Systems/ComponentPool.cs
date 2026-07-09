using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 단일 프리팹에 대한 컴포넌트 풀. 반복되는 Instantiate/Destroy 비용을 제거해
    /// 대량 스폰(적/투사체/데미지 텍스트)의 프레임 스파이크와 GC를 방지한다.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly Stack<T> _available = new();

        public int CountInactive => _available.Count;
        public int CountActive { get; private set; }

        public ComponentPool(T prefab, int prewarm = 0, int maxSize = 2000, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;

            for (int i = 0; i < prewarm; i++)
            {
                T obj = CreateInstance();
                obj.gameObject.SetActive(false);
                _available.Push(obj);
            }
        }

        private T CreateInstance() => Object.Instantiate(_prefab, _parent);

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = _available.Count > 0 ? _available.Pop() : CreateInstance();

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            CountActive++;

            if (obj is IPoolable poolable)
            {
                poolable.OnSpawnedFromPool();
            }

            return obj;
        }

        public void Release(T obj)
        {
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnedToPool();
            }

            obj.gameObject.SetActive(false);
            CountActive--;

            if (_available.Count < _maxSize)
            {
                _available.Push(obj);
            }
            else
            {
                Object.Destroy(obj.gameObject);
            }
        }

        public void Clear()
        {
            while (_available.Count > 0)
            {
                T obj = _available.Pop();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }

            CountActive = 0;
        }
    }
}
