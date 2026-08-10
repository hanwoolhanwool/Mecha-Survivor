using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// RigProfile_Mecha 실제 에셋의 배선 가드 (Docs/06) — 마운트·총구는 RigLab에서 눈으로 맞추는
    /// 값이라 회귀를 코드로 잡을 수 없다. 무기 모델이 빠지거나 총구 축이 틀어지면 게임 내내
    /// 엉뚱한 지점에서 발사되므로, 배선의 뼈대만 여기서 고정한다.
    /// </summary>
    public sealed class RigProfileAssetTests
    {
        private const string ProfilePath =
            "Assets/_Project/ScriptableObjects/Rig/RigProfile_Mecha.asset";

        private const string BeamMountId = "RightHandBeam";
        private const string BeamMuzzleId = "beam";

        private const string ShotgunMountId = "RightHandShotgun";
        private const string ShotgunMuzzleId = "shotgun_cannon";

        private RigProfileData _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = AssetDatabase.LoadAssetAtPath<RigProfileData>(ProfilePath);
            Assert.IsNotNull(_profile, $"리그 프로필을 찾지 못했다: {ProfilePath}");
        }

        private RigProfileData.MountDef FindMount(string id)
        {
            for (int i = 0; i < _profile.Mounts.Length; i++)
            {
                if (_profile.Mounts[i].Id == id)
                {
                    return _profile.Mounts[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 총구 조회. 좌우 마운트가 같은 무기 Id의 총구를 하나씩 이므로 mountId까지 줘야
        /// 원하는 쪽이 잡힌다 (Docs/06 §3.4) — 비우면 첫 일치.
        /// </summary>
        private RigProfileData.MuzzleDef FindMuzzle(string id, string mountId = null)
        {
            for (int i = 0; i < _profile.Muzzles.Length; i++)
            {
                RigProfileData.MuzzleDef def = _profile.Muzzles[i];
                if (def.Id == id && (mountId == null || def.MountId == mountId))
                {
                    return def;
                }
            }

            return null;
        }

        [Test]
        public void EveryMuzzle_ReferencesExistingMount()
        {
            for (int i = 0; i < _profile.Muzzles.Length; i++)
            {
                RigProfileData.MuzzleDef def = _profile.Muzzles[i];
                if (string.IsNullOrEmpty(def.MountId))
                {
                    continue; // 모델 루트 기준 총구 (적 등)
                }

                Assert.IsNotNull(FindMount(def.MountId),
                    $"총구 '{def.Id}'가 존재하지 않는 마운트 '{def.MountId}'를 가리킨다.");
            }
        }

        [Test]
        public void EveryBoundMount_HasMuzzleForItsWeapon()
        {
            for (int i = 0; i < _profile.Mounts.Length; i++)
            {
                RigProfileData.MountDef def = _profile.Mounts[i];
                if (def.ShowForWeapon == null)
                {
                    continue; // 항상 표시되는 장식 마운트
                }

                // 그 마운트 위의 총구여야 한다 — 반대 손 총구가 있다고 통과시키면
                // 왼손 무기가 오른손 총구에서 나가는 것을 못 잡는다.
                Assert.IsNotNull(FindMuzzle(def.ShowForWeapon.Id, def.Id),
                    $"마운트 '{def.Id}'는 {def.ShowForWeapon.Id} 무기용인데 이 마운트 위의 총구가 없다 " +
                    "— 투사체가 엉뚱한 곳에서 나간다.");
            }
        }

        [Test]
        public void BeamMount_ShowsAzureCoreCannonForBeamWeapon()
        {
            RigProfileData.MountDef mount = FindMount(BeamMountId);
            Assert.IsNotNull(mount, $"대출력 빔 마운트 '{BeamMountId}'가 없다.");
            Assert.IsNotNull(mount.VisualPrefab, "빔 마운트에 무기 모델이 비어 있다.");
            Assert.AreEqual("AzureCoreCannon", mount.VisualPrefab.name);
            Assert.IsNotNull(mount.ShowForWeapon, "빔 마운트에 ShowForWeapon이 비어 있다.");
            Assert.AreEqual(BeamMuzzleId, mount.ShowForWeapon.Id,
                "빔 마운트는 대출력 빔(WeaponData_Beam)에만 표시돼야 한다.");
        }

        [Test]
        public void BeamMuzzle_PointsAlongModelBarrelAxis()
        {
            RigProfileData.MuzzleDef muzzle = FindMuzzle(BeamMuzzleId, BeamMountId);
            Assert.IsNotNull(muzzle, $"총구 '{BeamMuzzleId}'가 없다.");
            Assert.AreEqual(BeamMountId, muzzle.MountId);

            // AzureCoreCannon은 포신이 로컬 +X를 향한다 (그립이 −X). forward가 +X여야 총구 방향과 일치.
            Vector3 forward = Quaternion.Euler(muzzle.LocalEulerAngles) * Vector3.forward;
            Assert.Greater(Vector3.Dot(forward, Vector3.right), 0.99f,
                $"빔 총구 forward가 모델 포신축(+X)을 벗어났다: {forward:F3}");
        }

        [Test]
        public void BeamMuzzle_SitsAtBarrelTip()
        {
            RigProfileData.MountDef mount = FindMount(BeamMountId);
            RigProfileData.MuzzleDef muzzle = FindMuzzle(BeamMuzzleId, BeamMountId);
            Assert.IsNotNull(mount);
            Assert.IsNotNull(muzzle);

            var filter = mount.VisualPrefab.GetComponentInChildren<MeshFilter>();
            Assert.IsNotNull(filter, "무기 모델에 MeshFilter가 없다.");

            // 모델 루트는 스케일·회전 보정이 없으므로 메시 로컬 = 마운트 로컬.
            float tipX = filter.sharedMesh.bounds.max.x;
            Assert.AreEqual(tipX, muzzle.LocalPosition.x, 0.1f,
                $"빔 총구가 포신 끝(x={tipX:F3})에서 떨어져 있다 — 빔이 모델 속에서 시작한다.");
        }

        [Test]
        public void ShotgunMount_ShowsShotgunCannonForShotgunWeapon()
        {
            RigProfileData.MountDef mount = FindMount(ShotgunMountId);
            Assert.IsNotNull(mount, $"산탄 캐논 마운트 '{ShotgunMountId}'가 없다.");
            Assert.IsNotNull(mount.VisualPrefab, "산탄 캐논 마운트에 무기 모델이 비어 있다.");
            Assert.AreEqual("ShotgunCannon", mount.VisualPrefab.name);
            Assert.IsNotNull(mount.ShowForWeapon, "산탄 캐논 마운트에 ShowForWeapon이 비어 있다.");
            Assert.AreEqual(ShotgunMuzzleId, mount.ShowForWeapon.Id,
                "산탄 캐논 마운트는 산탄 캐논(WeaponData_ShotgunCannon)에만 표시돼야 한다.");
        }

        [Test]
        public void ShotgunMuzzle_PointsAlongModelBarrelAxis()
        {
            RigProfileData.MuzzleDef muzzle = FindMuzzle(ShotgunMuzzleId, ShotgunMountId);
            Assert.IsNotNull(muzzle, $"총구 '{ShotgunMuzzleId}'가 없다.");

            // 이 프로젝트 무기 모델 규약: 포신 +X (Docs/06 §3.4).
            Vector3 forward = Quaternion.Euler(muzzle.LocalEulerAngles) * Vector3.forward;
            Assert.Greater(Vector3.Dot(forward, Vector3.right), 0.99f,
                $"산탄 캐논 총구 forward가 모델 포신축(+X)을 벗어났다: {forward:F3}");
        }

        [Test]
        public void ShotgunMuzzle_SitsAtBarrelTip()
        {
            RigProfileData.MountDef mount = FindMount(ShotgunMountId);
            RigProfileData.MuzzleDef muzzle = FindMuzzle(ShotgunMuzzleId, ShotgunMountId);
            Assert.IsNotNull(mount);
            Assert.IsNotNull(muzzle);

            // 산탄 캐논은 파츠가 13개로 쪼개져 있다 — 첫 MeshFilter만 보면 포신 끝이 아니라
            // 중간 파츠 끝이 잡힌다. 모델 전체의 최대 x를 포신 끝으로 삼는다.
            MeshFilter[] filters = mount.VisualPrefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.Greater(filters.Length, 0, "무기 모델에 MeshFilter가 없다.");

            float tipX = float.NegativeInfinity;
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null)
                {
                    continue;
                }

                float x = filters[i].transform.localToWorldMatrix
                    .MultiplyPoint3x4(filters[i].sharedMesh.bounds.max).x;
                if (x > tipX)
                {
                    tipX = x;
                }
            }

            Assert.AreEqual(tipX, muzzle.LocalPosition.x, 0.1f,
                $"산탄 캐논 총구가 포신 끝(x={tipX:F3})에서 떨어져 있다 — 탄이 모델 속에서 시작한다.");
        }

        // ── 좌우 손 마운트 (Docs/06 §3.4) ───────────────────────
        //
        // 왼손 마운트 값은 오른손 값을 기체 좌우로 미러해 자동 산출한 것이다. 손으로 만지면
        // 좌우가 어긋나는데 눈으로는 잘 안 보이므로, 짝이 맞는지 여기서 고정한다.

        [Test]
        public void EveryHandMount_DeclaresItsHand()
        {
            for (int i = 0; i < _profile.Mounts.Length; i++)
            {
                RigProfileData.MountDef def = _profile.Mounts[i];
                if (def.Bone == HumanBodyBones.RightHand)
                {
                    Assert.AreEqual(MountHand.Right, def.Hand,
                        $"마운트 '{def.Id}'가 오른손 본에 붙었는데 Hand가 Right가 아니다 " +
                        "— Any면 왼손에 들어도 표시돼 무기가 양손에 뜬다.");
                }
                else if (def.Bone == HumanBodyBones.LeftHand)
                {
                    Assert.AreEqual(MountHand.Left, def.Hand,
                        $"마운트 '{def.Id}'가 왼손 본에 붙었는데 Hand가 Left가 아니다.");
                }
            }
        }

        [Test]
        public void EveryRightHandMount_HasLeftCounterpartWithSameWeaponAndModel()
        {
            int pairs = 0;
            for (int i = 0; i < _profile.Mounts.Length; i++)
            {
                RigProfileData.MountDef right = _profile.Mounts[i];
                if (right.Bone != HumanBodyBones.RightHand)
                {
                    continue;
                }

                RigProfileData.MountDef left = FindMount(right.Id.Replace("RightHand", "LeftHand"));
                Assert.IsNotNull(left,
                    $"오른손 마운트 '{right.Id}'의 왼손 짝이 없다 — 그 무기는 왼손 슬롯에서 모델이 안 보인다.");
                Assert.AreEqual(HumanBodyBones.LeftHand, left.Bone);
                Assert.AreEqual(right.VisualPrefab, left.VisualPrefab, $"'{left.Id}' 모델이 다르다.");
                Assert.AreEqual(right.ShowForWeapon, left.ShowForWeapon,
                    $"'{left.Id}'가 다른 무기를 지목한다 — 좌우 짝은 같은 무기여야 한다.");
                Assert.AreEqual(right.LocalScale, left.LocalScale, $"'{left.Id}' 스케일이 다르다.");
                Assert.IsNotNull(FindMuzzle(left.ShowForWeapon.Id, left.Id), $"'{left.Id}' 위의 총구가 없다.");
                pairs++;
            }

            Assert.Greater(pairs, 0, "손 마운트 짝을 하나도 못 찾았다 — 테스트가 무의미하다.");
        }

        /// <summary>
        /// 좌우 마운트가 **실제로 미러인지** 바인드 포즈에서 확인한다 (Docs/06 §3.4).
        /// 로컬 수치만 봐서는 못 잡는다 — 2026-08-08에 축을 잘못 뒤집어 무기를 거꾸로 든 채
        /// 위치·발사 방향 검사는 전부 통과한 전례가 있다. 판정 대상은 **모델이 놓이는 축**:
        /// 포신(마운트 로컬 +X)과 위(+Y)가 미러여야 하고, 좌우 대칭면 법선(+Z)만 뒤집힌다.
        /// </summary>
        [Test]
        public void HandMountPairs_MirrorBarrelAndUpAxes()
        {
            var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_profile.ModelPrefab);
            try
            {
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                var animator = instance.GetComponentInChildren<Animator>();
                Assert.IsNotNull(animator, "모델 프리팹에 Animator가 없다.");

                var mounts = new Dictionary<string, Transform>();
                var muzzles = new Dictionary<string, Transform>();
                var spawnedVisuals = new List<Transform>();
                RigBuilder.BuildInto(_profile, animator, animator.transform,
                    spawn: null, mounts, muzzles, spawnedVisuals);

                int pairs = 0;
                for (int i = 0; i < _profile.Mounts.Length; i++)
                {
                    RigProfileData.MountDef right = _profile.Mounts[i];
                    if (right.Bone != HumanBodyBones.RightHand)
                    {
                        continue;
                    }

                    string leftId = right.Id.Replace("RightHand", "LeftHand");
                    Assert.IsTrue(mounts.TryGetValue(right.Id, out Transform r), $"'{right.Id}' 앵커 없음");
                    Assert.IsTrue(mounts.TryGetValue(leftId, out Transform l), $"'{leftId}' 앵커 없음");

                    Assert.AreEqual(0f, Vector3.Distance(l.position, Mirror(r.position)), 1e-3f,
                        $"'{leftId}' 위치가 미러가 아니다.");
                    Assert.Greater(Vector3.Dot(l.right, Mirror(r.right)), 0.999f,
                        $"'{leftId}' 포신축이 미러가 아니다 — 무기를 거꾸로 든다.");
                    Assert.Greater(Vector3.Dot(l.up, Mirror(r.up)), 0.999f,
                        $"'{leftId}' 위쪽이 미러가 아니다 — 무기가 뒤집힌다.");
                    pairs++;
                }

                Assert.Greater(pairs, 0, "손 마운트 짝을 하나도 못 찾았다.");

                // 총구는 발사 방향(로컬 +Z)이 의미축이라 뒤집는 축이 다르다 — 방향까지 미러여야 한다.
                foreach (KeyValuePair<string, Transform> entry in muzzles)
                {
                    if (!entry.Key.EndsWith("@R"))
                    {
                        continue;
                    }

                    string leftKey = entry.Key.Substring(0, entry.Key.Length - 2) + "@L";
                    Assert.IsTrue(muzzles.TryGetValue(leftKey, out Transform lm), $"'{leftKey}' 총구 없음");
                    Assert.AreEqual(0f, Vector3.Distance(lm.position, Mirror(entry.Value.position)), 1e-3f,
                        $"'{leftKey}' 총구 위치가 미러가 아니다.");
                    Assert.Greater(Vector3.Dot(lm.forward, Mirror(entry.Value.forward)), 0.999f,
                        $"'{leftKey}' 발사 방향이 미러가 아니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>기체 좌우(캐릭터 로컬 X=0) 미러. 모델을 원점·무회전으로 두므로 월드 = 캐릭터 공간.</summary>
        private static Vector3 Mirror(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        [Test]
        public void MuzzleKeys_AreUniquePerHand()
        {
            // RigBuilder는 (무기 Id, 손)으로 총구를 사전에 넣는다. 키가 겹치면 나중 것이
            // 앞 것을 덮어써 한쪽 손의 총구가 통째로 사라진다.
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < _profile.Muzzles.Length; i++)
            {
                RigProfileData.MuzzleDef def = _profile.Muzzles[i];
                string key = RigProfileMath.MuzzleKey(
                    def.Id, RigProfileMath.ResolveMuzzleHand(_profile, def.MountId));
                Assert.IsTrue(seen.Add(key), $"총구 키 '{key}'가 중복이다 (총구 '{def.Id}' @ '{def.MountId}').");
            }
        }
    }
}
