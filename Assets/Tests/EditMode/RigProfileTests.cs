using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>리그 프로필 본 해석 + 빌더 구성 결과 검증 (Docs/06 §6).</summary>
    public sealed class RigProfileTests
    {
        private GameObject _modelRoot;
        private Transform _spine;
        private RigProfileData _profile;
        private GameObject _visualPrefab;
        private WeaponData _weaponData;
        private readonly List<GameObject> _cleanup = new();

        [SetUp]
        public void SetUp()
        {
            // 모델 루트/본 계층: Model/Hips/Spine (Generic 폴백 경로 검증용)
            _modelRoot = Track(new GameObject("Model"));
            var hips = new GameObject("Hips").transform;
            hips.SetParent(_modelRoot.transform, false);
            _spine = new GameObject("Spine").transform;
            _spine.SetParent(hips, false);

            // 장착 모델 프리팹 대역 — FBX 루트 보정을 흉내 낸 루트 회전·스케일
            _visualPrefab = Track(new GameObject("VisualPrefab"));
            _visualPrefab.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            _visualPrefab.transform.localScale = new Vector3(100f, 100f, 100f);

            _weaponData = ScriptableObject.CreateInstance<WeaponData>();
            _profile = ScriptableObject.CreateInstance<RigProfileData>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null)
                {
                    Object.DestroyImmediate(_cleanup[i]);
                }
            }

            _cleanup.Clear();
            Object.DestroyImmediate(_weaponData);
            Object.DestroyImmediate(_profile);
        }

        private GameObject Track(GameObject go)
        {
            _cleanup.Add(go);
            return go;
        }

        // ── 본 해석 ──────────────────────────────────────────────

        [Test]
        public void FindByPath_EmptyPath_ReturnsRoot()
        {
            Assert.AreSame(_modelRoot.transform,
                RigProfileMath.FindByPath(_modelRoot.transform, ""));
        }

        [Test]
        public void FindByPath_ValidPath_FindsBone()
        {
            Assert.AreSame(_spine,
                RigProfileMath.FindByPath(_modelRoot.transform, "Hips/Spine"));
        }

        [Test]
        public void FindByPath_MissingPath_ReturnsNull()
        {
            Assert.IsNull(RigProfileMath.FindByPath(_modelRoot.transform, "Hips/Missing"));
        }

        [Test]
        public void ResolveBone_NoAnimator_FallsBackToPath()
        {
            Assert.AreSame(_spine, RigProfileMath.ResolveBone(
                null, HumanBodyBones.UpperChest, "Hips/Spine", _modelRoot.transform));
        }

        [Test]
        public void ResolveBone_GenericAnimator_FallsBackToPath()
        {
            // Avatar 없는 Animator = Generic — Humanoid 경로를 타면 안 된다.
            Animator animator = _modelRoot.AddComponent<Animator>();
            Assert.IsFalse(animator.isHuman);

            Assert.AreSame(_spine, RigProfileMath.ResolveBone(
                animator, HumanBodyBones.UpperChest, "Hips/Spine", _modelRoot.transform));
        }

        [Test]
        public void GetOrCreateAnchor_SecondCall_ReusesExisting()
        {
            Transform first = RigProfileMath.GetOrCreateAnchor(_spine, "Mount_Test");
            Transform second = RigProfileMath.GetOrCreateAnchor(_spine, "Mount_Test");

            Assert.AreSame(first, second, "같은 이름 앵커는 재사용해야 한다 (풀 재사용 대비).");
            Assert.AreEqual(1, _spine.childCount);
        }

        // ── 빌더 구성 ────────────────────────────────────────────

        private static Transform InstantiateSpawn(GameObject prefab)
        {
            return Object.Instantiate(prefab).transform;
        }

        private WeaponMountVisuals.MountBinding[] Build(
            Dictionary<string, Transform> mounts, Dictionary<string, Transform> muzzles)
        {
            return RigBuilder.BuildInto(
                _profile, null, _modelRoot.transform, InstantiateSpawn,
                mounts, muzzles, new List<Transform>());
        }

        [Test]
        public void BuildInto_Mount_AnchorsUnderBoneWithProfileLocals()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef
                {
                    Id = "BackWeapon",
                    BonePath = "Hips/Spine",
                    LocalPosition = new Vector3(0.1f, 0.2f, 0.3f),
                    LocalEulerAngles = new Vector3(0f, 45f, 0f),
                    LocalScale = new Vector3(0.45f, 0.45f, 0.45f),
                },
            };

            var mounts = new Dictionary<string, Transform>();
            Build(mounts, new Dictionary<string, Transform>());

            Assert.IsTrue(mounts.TryGetValue("BackWeapon", out Transform mount));
            Assert.AreSame(_spine, mount.parent, "마운트는 지정 본의 자식이어야 한다.");
            Assert.AreEqual("Mount_BackWeapon", mount.name);
            Assert.AreEqual(new Vector3(0.1f, 0.2f, 0.3f), mount.localPosition);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), mount.localRotation), 0.01f);
            Assert.AreEqual(new Vector3(0.45f, 0.45f, 0.45f), mount.localScale);
        }

        [Test]
        public void BuildInto_Visual_KeepsPrefabRootTransform()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef
                {
                    Id = "RightHandWeapon",
                    BonePath = "Hips/Spine",
                    LocalScale = Vector3.one,
                    VisualPrefab = _visualPrefab,
                },
            };

            var mounts = new Dictionary<string, Transform>();
            Build(mounts, new Dictionary<string, Transform>());

            Transform visual = mounts["RightHandWeapon"].GetChild(0);
            Track(visual.gameObject);

            // FBX 루트 보정(회전·×100 스케일)이 그대로 남아야 한다 (Docs/06 §7).
            Assert.Less(Quaternion.Angle(Quaternion.Euler(270f, 0f, 0f), visual.localRotation), 0.01f);
            Assert.AreEqual(new Vector3(100f, 100f, 100f), visual.localScale);
            Assert.AreEqual(Vector3.zero, visual.localPosition);
        }

        [Test]
        public void BuildInto_BoundVisual_StartsHiddenWithBinding()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef
                {
                    Id = "BackWeapon",
                    BonePath = "Hips/Spine",
                    LocalScale = Vector3.one,
                    VisualPrefab = _visualPrefab,
                    ShowForWeapon = _weaponData,
                },
            };

            var mounts = new Dictionary<string, Transform>();
            WeaponMountVisuals.MountBinding[] bindings =
                Build(mounts, new Dictionary<string, Transform>());

            Assert.AreEqual(1, bindings.Length);
            Assert.AreSame(_weaponData, bindings[0].Weapon);
            Track(bindings[0].Model);
            Assert.IsFalse(bindings[0].Model.activeSelf, "보유 전에는 숨겨져야 한다.");
        }

        [Test]
        public void BuildInto_UnboundVisual_StaysVisibleWithoutBinding()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef
                {
                    Id = "BackWeapon",
                    BonePath = "Hips/Spine",
                    LocalScale = Vector3.one,
                    VisualPrefab = _visualPrefab,
                },
            };

            var mounts = new Dictionary<string, Transform>();
            WeaponMountVisuals.MountBinding[] bindings =
                Build(mounts, new Dictionary<string, Transform>());

            Assert.AreEqual(0, bindings.Length);
            Transform visual = mounts["BackWeapon"].GetChild(0);
            Track(visual.gameObject);
            Assert.IsTrue(visual.gameObject.activeSelf, "바인딩 없는 모델은 항상 표시.");
        }

        [Test]
        public void BuildInto_MissingBone_SkipsMountWithoutThrow()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef { Id = "Bad", BonePath = "Hips/Missing" },
            };

            var mounts = new Dictionary<string, Transform>();
            Assert.DoesNotThrow(() => Build(mounts, new Dictionary<string, Transform>()));
            Assert.AreEqual(0, mounts.Count);
        }

        [Test]
        public void BuildInto_Muzzle_AnchorsUnderMount()
        {
            _profile.Mounts = new[]
            {
                new RigProfileData.MountDef
                {
                    Id = "RightHandWeapon", BonePath = "Hips/Spine", LocalScale = Vector3.one,
                },
            };
            _profile.Muzzles = new[]
            {
                new RigProfileData.MuzzleDef
                {
                    Id = "laser_cannon",
                    MountId = "RightHandWeapon",
                    LocalPosition = new Vector3(0f, 0f, 1.5f),
                },
            };

            var mounts = new Dictionary<string, Transform>();
            var muzzles = new Dictionary<string, Transform>();
            Build(mounts, muzzles);

            Assert.IsTrue(muzzles.TryGetValue("laser_cannon", out Transform muzzle));
            Assert.AreSame(mounts["RightHandWeapon"], muzzle.parent);
            Assert.AreEqual("Muzzle_laser_cannon", muzzle.name);
            Assert.AreEqual(new Vector3(0f, 0f, 1.5f), muzzle.localPosition);
        }

        [Test]
        public void BuildInto_MuzzleWithoutMountId_AnchorsUnderModelRoot()
        {
            _profile.Muzzles = new[]
            {
                new RigProfileData.MuzzleDef
                {
                    Id = "enemy_main", MountId = "", LocalPosition = new Vector3(0f, 1.5f, 0f),
                },
            };

            var muzzles = new Dictionary<string, Transform>();
            Build(new Dictionary<string, Transform>(), muzzles);

            Assert.AreSame(_modelRoot.transform, muzzles["enemy_main"].parent);
            Assert.AreEqual(new Vector3(0f, 1.5f, 0f), muzzles["enemy_main"].localPosition);
        }

        [Test]
        public void BuildInto_MuzzleWithMissingMount_SkipsWithoutThrow()
        {
            _profile.Muzzles = new[]
            {
                new RigProfileData.MuzzleDef { Id = "orphan", MountId = "NoSuchMount" },
            };

            var muzzles = new Dictionary<string, Transform>();
            Assert.DoesNotThrow(
                () => Build(new Dictionary<string, Transform>(), muzzles));
            Assert.AreEqual(0, muzzles.Count);
        }
    }
}
