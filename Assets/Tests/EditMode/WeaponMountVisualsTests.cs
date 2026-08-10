using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>무기 보유 여부 ↔ 마운트 모델 표시 동기화 검증.</summary>
    public sealed class WeaponMountVisualsTests
    {
        private GameObject _root;
        private WeaponSlots _slots;
        private WeaponData _missileData;
        private WeaponData _otherData;
        private GameObject _model;
        private WeaponMountVisuals.MountBinding[] _bindings;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MountTest");
            _slots = _root.AddComponent<WeaponSlots>();
            _missileData = ScriptableObject.CreateInstance<WeaponData>();
            _otherData = ScriptableObject.CreateInstance<WeaponData>();

            _model = new GameObject("Model");
            _model.transform.SetParent(_root.transform);
            _model.SetActive(false);

            _bindings = new[]
            {
                new WeaponMountVisuals.MountBinding { Weapon = _missileData, Model = _model },
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_missileData);
            Object.DestroyImmediate(_otherData);
        }

        private Weapon AddWeapon(WeaponData data)
        {
            var weapon = _root.AddComponent<ProjectileWeapon>();
            weapon.SetData(data);
            return weapon;
        }

        [Test]
        public void Apply_WeaponNotOwned_ModelStaysHidden()
        {
            WeaponMountVisuals.Apply(_slots, _bindings);

            Assert.IsFalse(_model.activeSelf, "무기가 없으면 모델은 꺼져 있어야 한다.");
        }

        [Test]
        public void Apply_WeaponEquipped_ModelShows()
        {
            _slots.Equip(AddWeapon(_missileData));

            WeaponMountVisuals.Apply(_slots, _bindings);

            Assert.IsTrue(_model.activeSelf, "바인딩된 무기를 장착하면 모델이 켜져야 한다.");
        }

        [Test]
        public void Apply_DifferentWeaponEquipped_ModelStaysHidden()
        {
            _slots.Equip(AddWeapon(_otherData));

            WeaponMountVisuals.Apply(_slots, _bindings);

            Assert.IsFalse(_model.activeSelf, "다른 무기 장착은 모델을 켜면 안 된다.");
        }

        [Test]
        public void SetBindings_InjectedBindings_ApplyImmediately()
        {
            var visuals = _root.AddComponent<WeaponMountVisuals>();
            var so = new UnityEditor.SerializedObject(visuals);
            so.FindProperty("_slots").objectReferenceValue = _slots;
            so.ApplyModifiedPropertiesWithoutUndo();
            _slots.Equip(AddWeapon(_missileData));

            visuals.SetBindings(_bindings);

            Assert.IsTrue(_model.activeSelf,
                "RigBuilder가 주입한 바인딩은 다음 프레임을 기다리지 않고 즉시 반영돼야 한다.");
        }

        // ── 좌우 마운트 (Docs/06 §3.4) ──────────────────────────
        //
        // 같은 무기를 좌우 마운트가 각각 지목하므로, 보유만 보면 무기 하나가 양손에 뜬다.

        /// <summary>같은 무기의 오른손/왼손 모델 2개를 건 바인딩.</summary>
        private (GameObject right, GameObject left, WeaponMountVisuals.MountBinding[] bindings)
            MakeHandedBindings()
        {
            var right = new GameObject("RightModel");
            right.transform.SetParent(_root.transform);
            right.SetActive(false);
            var left = new GameObject("LeftModel");
            left.transform.SetParent(_root.transform);
            left.SetActive(false);

            var bindings = new[]
            {
                new WeaponMountVisuals.MountBinding
                    { Weapon = _missileData, Hand = MountHand.Right, Model = right },
                new WeaponMountVisuals.MountBinding
                    { Weapon = _missileData, Hand = MountHand.Left, Model = left },
            };
            return (right, left, bindings);
        }

        [Test]
        public void Apply_HandedBindings_OnlyEquippedHandShows()
        {
            (GameObject right, GameObject left, WeaponMountVisuals.MountBinding[] bindings) =
                MakeHandedBindings();

            _slots.Equip(AddWeapon(_missileData));   // 슬롯 0 = 오른손

            WeaponMountVisuals.Apply(_slots, bindings);

            Assert.IsTrue(right.activeSelf, "슬롯 0(오른손) 무기는 오른손 모델을 켜야 한다.");
            Assert.IsFalse(left.activeSelf, "무기 하나가 양손에 동시에 나타나면 안 된다.");
        }

        [Test]
        public void Apply_SecondSlot_ShowsLeftHandModel()
        {
            (GameObject right, GameObject left, WeaponMountVisuals.MountBinding[] bindings) =
                MakeHandedBindings();

            _slots.Equip(AddWeapon(_otherData));     // 슬롯 0을 채워 둘째 슬롯으로 밀어낸다
            _slots.Equip(AddWeapon(_missileData));   // 슬롯 1 = 왼손

            WeaponMountVisuals.Apply(_slots, bindings);

            Assert.IsFalse(right.activeSelf);
            Assert.IsTrue(left.activeSelf,
                "로드아웃 둘째 무기는 왼손 마운트에 모델이 붙어야 한다 (Docs/05 §10-B10).");
        }

        [Test]
        public void Apply_AnyHandBinding_ShowsInEitherHand()
        {
            // 등 마운트처럼 손 조건이 없는 바인딩은 어느 손에 들어도 표시된다.
            var model = new GameObject("BackModel");
            model.transform.SetParent(_root.transform);
            model.SetActive(false);
            var bindings = new[]
            {
                new WeaponMountVisuals.MountBinding
                    { Weapon = _missileData, Hand = MountHand.Any, Model = model },
            };

            _slots.Equip(AddWeapon(_otherData));
            _slots.Equip(AddWeapon(_missileData));   // 왼손 슬롯

            WeaponMountVisuals.Apply(_slots, bindings);

            Assert.IsTrue(model.activeSelf);
        }

        [Test]
        public void Apply_WeaponRemoved_ModelHidesAgain()
        {
            Weapon weapon = AddWeapon(_missileData);
            _slots.Equip(weapon);
            WeaponMountVisuals.Apply(_slots, _bindings);
            Assert.IsTrue(_model.activeSelf);

            _slots.ReplaceSlot(0, null);

            WeaponMountVisuals.Apply(_slots, _bindings);

            Assert.IsFalse(_model.activeSelf, "무기를 내리면 모델도 꺼져야 한다.");
        }
    }
}
