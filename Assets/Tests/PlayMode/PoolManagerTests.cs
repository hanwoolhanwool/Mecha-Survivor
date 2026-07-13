using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Tests.PlayMode
{
    /// <summary>
    /// PoolManager의 전역 스폰/회수 동작 검증. 특히 씬 재시작 시 이전 런의
    /// 활성 인스턴스(적/투사체)가 잔존하지 않도록 하는 DespawnAllActive를 다룬다.
    /// 싱글턴 생성/파괴가 필요해 PlayMode에 둔다.
    /// </summary>
    public sealed class PoolManagerTests
    {
        private sealed class PoolDummy : MonoBehaviour, IPoolable
        {
            public int ReturnCount;
            public void OnSpawnedFromPool() { }
            public void OnReturnedToPool() => ReturnCount++;
        }

        [UnityTest]
        public IEnumerator DespawnAllActive_ReturnsEveryActiveInstanceToPool()
        {
            PoolDummy prefabA = new GameObject("PrefabA").AddComponent<PoolDummy>();
            PoolDummy prefabB = new GameObject("PrefabB").AddComponent<PoolDummy>();
            prefabA.gameObject.SetActive(false);
            prefabB.gameObject.SetActive(false);

            PoolManager pm = PoolManager.Instance;

            var a1 = (PoolDummy)pm.Spawn(prefabA, Vector3.zero, Quaternion.identity);
            var a2 = (PoolDummy)pm.Spawn(prefabA, Vector3.one, Quaternion.identity);
            var b1 = (PoolDummy)pm.Spawn(prefabB, Vector3.zero, Quaternion.identity);

            pm.DespawnAllActive();

            Assert.IsFalse(a1.gameObject.activeSelf, "회수된 인스턴스는 비활성이어야 한다.");
            Assert.IsFalse(a2.gameObject.activeSelf, "회수된 인스턴스는 비활성이어야 한다.");
            Assert.IsFalse(b1.gameObject.activeSelf, "회수된 인스턴스는 비활성이어야 한다.");
            Assert.AreEqual(1, a1.ReturnCount, "OnReturnedToPool이 정확히 1회 호출되어야 한다.");
            Assert.AreEqual(1, b1.ReturnCount, "OnReturnedToPool이 정확히 1회 호출되어야 한다.");

            // 회수된 인스턴스는 새로 만들지 않고 재사용되어야 한다(풀 복귀 확인).
            var reused = (PoolDummy)pm.Spawn(prefabB, Vector3.zero, Quaternion.identity);
            Assert.AreSame(b1, reused, "회수된 인스턴스가 풀에 복귀해 재사용되어야 한다.");

            // 이미 회수된 것을 다시 전량 회수해도 이중 반환 경고가 나면 안 된다.
            pm.DespawnAllActive();
            Assert.IsFalse(reused.gameObject.activeSelf);
            Assert.AreEqual(2, b1.ReturnCount);

            Object.Destroy(pm.gameObject);
            Object.Destroy(prefabA.gameObject);
            Object.Destroy(prefabB.gameObject);
            yield return null; // 싱글턴 OnDestroy가 돌아 전역 상태가 정리되게 한다.
        }

        [UnityTest]
        public IEnumerator DespawnAllActive_WithNoActiveInstances_IsNoOp()
        {
            PoolManager pm = PoolManager.Instance;

            Assert.DoesNotThrow(() => pm.DespawnAllActive());

            Object.Destroy(pm.gameObject);
            yield return null;
        }
    }
}
