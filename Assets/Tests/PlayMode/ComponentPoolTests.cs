using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.PlayMode
{
    /// <summary>
    /// 오브젝트 풀의 재사용/카운트/이중반환 가드/IPoolable 콜백을 검증하는 스모크 테스트.
    /// Instantiate/Destroy가 유효해야 하므로 PlayMode에 둔다.
    /// </summary>
    public sealed class ComponentPoolTests
    {
        private sealed class PoolDummy : MonoBehaviour, IPoolable
        {
            public int SpawnCount;
            public int ReturnCount;
            public void OnSpawnedFromPool() => SpawnCount++;
            public void OnReturnedToPool() => ReturnCount++;
        }

        private static PoolDummy CreatePrefab() => new GameObject("PoolPrefab").AddComponent<PoolDummy>();

        [Test]
        public void GetAndRelease_TracksCountsAndInvokesCallbacks()
        {
            PoolDummy prefab = CreatePrefab();
            var pool = new ComponentPool<PoolDummy>(prefab, prewarm: 0);

            PoolDummy obj = pool.Get(Vector3.one, Quaternion.identity);
            Assert.AreEqual(1, pool.CountActive);
            Assert.IsTrue(obj.gameObject.activeSelf, "Get한 인스턴스는 활성화되어야 한다.");
            Assert.AreEqual(1, obj.SpawnCount, "OnSpawnedFromPool이 호출되어야 한다.");
            Assert.AreEqual(Vector3.one, obj.transform.position);

            pool.Release(obj);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive, "반환된 인스턴스는 재사용 대기열에 쌓여야 한다.");
            Assert.IsFalse(obj.gameObject.activeSelf, "반환된 인스턴스는 비활성화되어야 한다.");
            Assert.AreEqual(1, obj.ReturnCount, "OnReturnedToPool이 호출되어야 한다.");

            pool.Clear();
            Object.Destroy(prefab.gameObject);
        }

        [Test]
        public void Get_ReusesReleasedInstance()
        {
            PoolDummy prefab = CreatePrefab();
            var pool = new ComponentPool<PoolDummy>(prefab, prewarm: 0);

            PoolDummy first = pool.Get(Vector3.zero, Quaternion.identity);
            pool.Release(first);
            PoolDummy second = pool.Get(Vector3.zero, Quaternion.identity);

            Assert.AreSame(first, second, "반환된 인스턴스를 새로 만들지 않고 재사용해야 한다.");
            Assert.AreEqual(0, pool.CountInactive);

            pool.Clear();
            Object.Destroy(prefab.gameObject);
        }

        [Test]
        public void DoubleRelease_IsIgnored()
        {
            PoolDummy prefab = CreatePrefab();
            var pool = new ComponentPool<PoolDummy>(prefab, prewarm: 0);

            PoolDummy obj = pool.Get(Vector3.zero, Quaternion.identity);
            pool.Release(obj);

            LogAssert.Expect(LogType.Warning, new Regex("이미 풀에 반환된"));
            pool.Release(obj); // 두 번째 반환은 무시되어야 한다.

            Assert.AreEqual(0, pool.CountActive, "이중 반환으로 CountActive가 음수가 되면 안 된다.");
            Assert.AreEqual(1, pool.CountInactive, "이중 반환으로 풀에 중복 적재되면 안 된다.");

            pool.Clear();
            Object.Destroy(prefab.gameObject);
        }

        [Test]
        public void Prewarm_CreatesInactiveInstances()
        {
            PoolDummy prefab = CreatePrefab();
            var pool = new ComponentPool<PoolDummy>(prefab, prewarm: 3);

            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);

            pool.Clear();
            Object.Destroy(prefab.gameObject);
        }
    }
}
