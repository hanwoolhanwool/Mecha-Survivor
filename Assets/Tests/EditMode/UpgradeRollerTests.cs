using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>3택 가중치 추첨 검증 — 중복 없음, 0 가중치 제외, 후보 부족 처리.</summary>
    public sealed class UpgradeRollerTests
    {
        private readonly List<UpgradeData> _created = new();

        private UpgradeData NewUpgrade(string id)
        {
            var upgrade = ScriptableObject.CreateInstance<ArmorUpgradeData>();
            upgrade.Id = id;
            _created.Add(upgrade);
            return upgrade;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UpgradeData upgrade in _created)
            {
                Object.DestroyImmediate(upgrade);
            }

            _created.Clear();
        }

        [Test]
        public void RollThree_ReturnsDistinctItems()
        {
            var candidates = new List<UpgradeData>
            {
                NewUpgrade("a"), NewUpgrade("b"), NewUpgrade("c"), NewUpgrade("d"),
            };
            var weights = new List<float> { 1f, 1f, 1f, 1f };
            var result = new List<UpgradeData>();

            UpgradeRoller.RollWithoutReplacement(candidates, weights, 3, new System.Random(7), result);

            Assert.AreEqual(3, result.Count);
            CollectionAssert.AllItemsAreUnique(result, "비복원 추첨은 중복이 없어야 한다.");
        }

        [Test]
        public void RollThree_FewerCandidates_ReturnsAll()
        {
            var candidates = new List<UpgradeData> { NewUpgrade("a"), NewUpgrade("b") };
            var weights = new List<float> { 1f, 1f };
            var result = new List<UpgradeData>();

            UpgradeRoller.RollWithoutReplacement(candidates, weights, 3, new System.Random(7), result);

            Assert.AreEqual(2, result.Count, "후보가 3개 미만이면 있는 만큼만 반환한다.");
        }

        [Test]
        public void ZeroWeightItem_IsNeverPicked()
        {
            UpgradeData never = NewUpgrade("never");
            var rng = new System.Random(123);

            for (int trial = 0; trial < 200; trial++)
            {
                var candidates = new List<UpgradeData> { NewUpgrade("a"), never, NewUpgrade("b") };
                var weights = new List<float> { 1f, 0f, 1f };
                var result = new List<UpgradeData>();

                UpgradeRoller.RollWithoutReplacement(candidates, weights, 2, rng, result);

                CollectionAssert.DoesNotContain(result, never, "가중치 0은 절대 뽑히면 안 된다.");
            }
        }

        [Test]
        public void PickWeightedIndex_AllZeroWeights_ReturnsMinusOne()
        {
            int index = UpgradeRoller.PickWeightedIndex(
                new List<float> { 0f, 0f }, new System.Random(1));

            Assert.AreEqual(-1, index);
        }

        [Test]
        public void PickWeightedIndex_HeavyWeight_DominatesDistribution()
        {
            var weights = new List<float> { 1f, 99f };
            var rng = new System.Random(42);
            int heavyPicks = 0;

            for (int i = 0; i < 1000; i++)
            {
                if (UpgradeRoller.PickWeightedIndex(weights, rng) == 1)
                {
                    heavyPicks++;
                }
            }

            Assert.Greater(heavyPicks, 950, "가중치 99:1이면 압도적으로 자주 뽑혀야 한다.");
        }
    }
}
