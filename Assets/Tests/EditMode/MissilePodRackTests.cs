using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>미사일 포드 장전 랙 — 발사 순서·장전 표시·소비 검증 (트윈 로켓 캐논 연출).</summary>
    public sealed class MissilePodRackTests
    {
        private GameObject _root;
        private MissilePodRack _rack;

        // 모델과 동일한 12발 구성 (생성 순서는 뒤섞어 정렬을 검증한다)
        private static readonly string[] SlotNames =
        {
            "Missile_L_Outer_2", "Missile_R_Inner_1", "Missile_L_Inner_3",
            "Missile_R_Outer_1", "Missile_L_Inner_1", "Missile_R_Inner_3",
            "Missile_L_Outer_1", "Missile_R_Outer_3", "Missile_L_Inner_2",
            "Missile_R_Inner_2", "Missile_L_Outer_3", "Missile_R_Outer_2",
        };

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Pod");
            var body = new GameObject("Body").transform;
            body.SetParent(_root.transform, false);
            for (int i = 0; i < SlotNames.Length; i++)
            {
                new GameObject(SlotNames[i]).transform.SetParent(body, false);
            }

            _rack = _root.AddComponent<MissilePodRack>();
            _rack.CollectSlots();   // EditMode에서는 Awake가 불리지 않는다
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private int CountVisible()
        {
            int visible = 0;
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Missile_") && t.gameObject.activeSelf)
                {
                    visible++;
                }
            }

            return visible;
        }

        [Test]
        public void Capacity_MatchesModelMissileCount()
        {
            Assert.AreEqual(12, _rack.Capacity);
        }

        [Test]
        public void SetLoaded_ShowsExactlyRequestedCount()
        {
            _rack.SetLoaded(4);
            Assert.AreEqual(4, CountVisible());

            _rack.SetLoaded(7);
            Assert.AreEqual(7, CountVisible(), "레벨업 등 장전 수 변경이 즉시 반영돼야 한다.");
        }

        [Test]
        public void SetLoaded_ClampsToCapacity()
        {
            _rack.SetLoaded(16);   // Lv5 발사 수 > 슬롯 12
            Assert.AreEqual(12, CountVisible());
        }

        [Test]
        public void ConsumeOrder_InnerFirst_RightLeftAlternating()
        {
            _rack.SetLoaded(12);
            string[] expected =
            {
                "Missile_R_Inner_1", "Missile_L_Inner_1",
                "Missile_R_Inner_2", "Missile_L_Inner_2",
                "Missile_R_Inner_3", "Missile_L_Inner_3",
                "Missile_R_Outer_1", "Missile_L_Outer_1",
            };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(_rack.TryConsumeNext(out Transform slot));
                Assert.AreEqual(expected[i], slot.name, $"{i}번째 발사 순서가 틀렸다.");
                Assert.IsFalse(slot.gameObject.activeSelf, "소비된 미사일은 숨겨져야 한다.");
            }
        }

        [Test]
        public void TryConsumeNext_ExhaustsLoadedThenFails()
        {
            _rack.SetLoaded(4);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(_rack.TryConsumeNext(out _));
            }

            Assert.IsFalse(_rack.TryConsumeNext(out _), "장전 수만큼만 소비할 수 있어야 한다.");
            Assert.AreEqual(0, CountVisible(), "쿨타임 중에는 미사일이 없어야 한다.");
        }

        [Test]
        public void SetLoaded_AfterConsume_ReloadsAndResetsOrder()
        {
            _rack.SetLoaded(4);
            _rack.TryConsumeNext(out _);
            _rack.TryConsumeNext(out _);

            _rack.SetLoaded(4);   // 쿨다운 완료 → 재장전

            Assert.AreEqual(4, CountVisible());
            Assert.IsTrue(_rack.TryConsumeNext(out Transform first));
            Assert.AreEqual("Missile_R_Inner_1", first.name, "재장전 후 발사 순서가 처음부터여야 한다.");
        }
    }
}
