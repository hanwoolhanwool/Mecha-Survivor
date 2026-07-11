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
        // 이미 반환된(비활성) 인스턴스 집합. 동일 객체의 이중 Release를 O(1)로 차단한다.
        private readonly HashSet<T> _inactive = new();

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
                _inactive.Add(obj);
            }
        }

        private T CreateInstance() => Object.Instantiate(_prefab, _parent);

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = _available.Count > 0 ? _available.Pop() : CreateInstance();
            _inactive.Remove(obj);

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
            if (obj == null)
            {
                return;
            }

            // 이미 반환된 객체의 재반환은 CountActive를 오염시키므로 무시한다.
            if (!_inactive.Add(obj))
            {
                Debug.LogWarning($"[ComponentPool] 이미 풀에 반환된 인스턴스를 다시 Release 시도: {obj.name}", obj);
                return;
            }

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
                // 풀 상한 초과분은 보관하지 않고 파괴 — 추적 집합에서도 제거.
                _inactive.Remove(obj);
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

            _inactive.Clear();
            CountActive = 0;
        }
    }
}
