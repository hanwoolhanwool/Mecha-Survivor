using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>EMP Lv.5 체인 감전 경로 검증.</summary>
    public sealed class EmpChainMathTests
    {
        private readonly List<int> _chain = new();

        [Test]
        public void BuildChain_FollowsNearestNeighbors()
        {
            // 일렬로 늘어선 점 — 체인은 순서대로 옮겨붙어야 한다.
            var points = new List<Vector3>
            {
                new(0f, 0f, 0f),
                new(3f, 0f, 0f),
                new(6f, 0f, 0f),
                new(9f, 0f, 0f),
            };

            int length = EmpChainMath.BuildChain(points, 0, 5f, 10, _chain);

            Assert.AreEqual(4, length);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, _chain,
                "체인은 가장 가까운 이웃 순서로 이어져야 한다.");
        }

        [Test]
        public void BuildChain_BreaksWhenLinkTooFar()
        {
            var points = new List<Vector3>
            {
                new(0f, 0f, 0f),
                new(3f, 0f, 0f),
                new(50f, 0f, 0f), // 사거리 밖
            };

            int length = EmpChainMath.BuildChain(points, 0, 5f, 10, _chain);

            Assert.AreEqual(2, length, "고리 간 거리가 한계를 넘으면 체인이 끊겨야 한다.");
            CollectionAssert.DoesNotContain(_chain, 2);
        }

        [Test]
        public void BuildChain_RespectsMaxLinks()
        {
            var points = new List<Vector3>();
            for (int i = 0; i < 10; i++)
            {
                points.Add(new Vector3(i * 2f, 0f, 0f));
            }

            int length = EmpChainMath.BuildChain(points, 0, 5f, 4, _chain);

            Assert.AreEqual(4, length, "체인은 최대 고리 수를 넘으면 안 된다.");
        }

        [Test]
        public void BuildChain_NeverVisitsSamePointTwice()
        {
            var points = new List<Vector3>
            {
                new(0f, 0f, 0f),
                new(1f, 0f, 0f),
                new(2f, 0f, 0f),
            };

            EmpChainMath.BuildChain(points, 1, 10f, 10, _chain);

            var seen = new HashSet<int>();
            foreach (int index in _chain)
            {
                Assert.IsTrue(seen.Add(index), "같은 적이 체인에 두 번 들어가면 피해가 중복된다.");
            }
        }

        [Test]
        public void BuildChain_EmptyOrInvalidInput_ReturnsZero()
        {
            Assert.AreEqual(0, EmpChainMath.BuildChain(new List<Vector3>(), 0, 5f, 5, _chain));
            Assert.AreEqual(0, EmpChainMath.BuildChain(
                new List<Vector3> { Vector3.zero }, 5, 5f, 5, _chain), "시작 인덱스 범위 밖은 0.");
        }
    }
}
