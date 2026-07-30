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
