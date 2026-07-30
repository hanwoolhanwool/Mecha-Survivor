using System;
using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 런타임 리그 빌더 — RigProfileData를 받아 마운트 앵커를 본 아래 생성하고 장착 모델을
    /// 붙인 뒤 WeaponMountVisuals 바인딩을 구성한다 (Docs/06 §3.2). 씬에 수동 배치된
    /// 마운트를 대체해 씬 간 불일치를 원천 제거한다.
    /// </summary>
    public sealed class RigBuilder : MonoBehaviour
    {
        [SerializeField] private RigProfileData _profile;

        [Tooltip("씬에 이미 배선된 모델의 Animator (플레이어). 비우면 프로필의 ModelPrefab을 스폰한다")]
        [SerializeField] private Animator _animator;

        [Tooltip("바인딩 주입 대상 (플레이어 전용, 적은 비움)")]
        [SerializeField] private WeaponMountVisuals _mountVisuals;

        private readonly Dictionary<string, Transform> _mounts = new();
        private readonly Dictionary<string, Transform> _muzzles = new();
        private readonly List<Transform> _spawnedVisuals = new();
        private Transform _spawnedModel;

        public RigProfileData Profile => _profile;

        /// <summary>해석된 모델 루트 (본 경로 폴백의 기준). Build 후 유효.</summary>
        public Transform ModelRoot { get; private set; }

        private void Awake() => Build();

        public bool TryGetMount(string id, out Transform mount) => _mounts.TryGetValue(id, out mount);

        public bool TryGetMuzzle(string id, out Transform muzzle) => _muzzles.TryGetValue(id, out muzzle);

        /// <summary>RigLab 대상 전환용 — 교체 후 Build()를 다시 부른다.</summary>
        public void SetProfile(RigProfileData profile, Animator animator = null)
        {
            _profile = profile;
            _animator = animator;
        }

        /// <summary>기존 생성물을 회수하고 프로필대로 재구성한다. 반복 호출 안전.</summary>
        public void Build()
        {
            Clear();
            if (_profile == null)
            {
                return;
            }

            Animator animator = _animator;
            if (animator == null && _profile.ModelPrefab != null)
            {
                _spawnedModel = SpawnAsChild(_profile.ModelPrefab, transform);
                animator = _spawnedModel.GetComponentInChildren<Animator>();
            }

            ModelRoot = animator != null ? animator.transform
                : _spawnedModel != null ? _spawnedModel : transform;

            // 확인용 AC 주입 — 모델에 AC가 없을 때만 (씬 배선 모델의 AC를 덮지 않는다).
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = _profile.AnimatorController;
            }

            WeaponMountVisuals.MountBinding[] bindings = BuildInto(
                _profile, animator, ModelRoot, SpawnUnparented,
                _mounts, _muzzles, _spawnedVisuals);

            if (_mountVisuals != null)
            {
                _mountVisuals.SetBindings(bindings);
            }
        }

        /// <summary>
        /// 프로필 → 마운트/총구/바인딩 구성의 본체. 스폰을 위임받아 PoolManager 없이도 돌므로
        /// EditMode 테스트 대상 (spawn: 프리팹 → 미부모 인스턴스 트랜스폼).
        /// </summary>
        public static WeaponMountVisuals.MountBinding[] BuildInto(
            RigProfileData profile, Animator animator, Transform modelRoot,
            Func<GameObject, Transform> spawn,
            Dictionary<string, Transform> mounts, Dictionary<string, Transform> muzzles,
            List<Transform> spawnedVisuals)
        {
            var bindings = new List<WeaponMountVisuals.MountBinding>();

            if (profile.Mounts != null)
            {
                for (int i = 0; i < profile.Mounts.Length; i++)
                {
                    RigProfileData.MountDef def = profile.Mounts[i];
                    Transform bone = RigProfileMath.ResolveBone(
                        animator, def.Bone, def.BonePath, modelRoot);
                    if (bone == null)
                    {
                        Debug.LogWarning($"[RigBuilder] 마운트 '{def.Id}' 본 해석 실패 " +
                            $"(Bone={def.Bone}, Path='{def.BonePath}') — 건너뜀");
                        continue;
                    }

                    Transform anchor = RigProfileMath.GetOrCreateAnchor(
                        bone, RigProfileMath.MountObjectName(def.Id));
                    RigProfileMath.ApplyLocal(
                        anchor, def.LocalPosition, def.LocalEulerAngles, def.LocalScale);
                    mounts[def.Id] = anchor;

                    if (def.VisualPrefab == null || spawn == null)
                    {
                        continue;
                    }

                    Transform visual = spawn(def.VisualPrefab);
                    visual.SetParent(anchor, worldPositionStays: false);

                    // 스포너가 월드 포즈를 바꿨을 수 있으니 프리팹 루트의 로컬 값을 복원한다.
                    // FBX 루트 스케일(레일건 ×100 보정)을 덮지 않기 위한 규약 (Docs/06 §7).
                    Transform prefabRoot = def.VisualPrefab.transform;
                    visual.localPosition = prefabRoot.localPosition;
                    visual.localRotation = prefabRoot.localRotation;
                    visual.localScale = prefabRoot.localScale;
                    spawnedVisuals.Add(visual);

                    if (def.ShowForWeapon != null)
                    {
                        // 보유 전에는 숨김 — 표시는 WeaponMountVisuals 폴링이 담당.
                        visual.gameObject.SetActive(false);
                        bindings.Add(new WeaponMountVisuals.MountBinding
                        {
                            Weapon = def.ShowForWeapon,
                            Model = visual.gameObject,
                        });
                    }
                }
            }

            if (profile.Muzzles != null)
            {
                for (int i = 0; i < profile.Muzzles.Length; i++)
                {
                    RigProfileData.MuzzleDef def = profile.Muzzles[i];
                    Transform parent = string.IsNullOrEmpty(def.MountId)
                        ? modelRoot
                        : mounts.GetValueOrDefault(def.MountId);
                    if (parent == null)
                    {
                        Debug.LogWarning($"[RigBuilder] 총구 '{def.Id}' 기준 마운트 " +
                            $"'{def.MountId}' 없음 — 건너뜀");
                        continue;
                    }

                    Transform anchor = RigProfileMath.GetOrCreateAnchor(
                        parent, RigProfileMath.MuzzleObjectName(def.Id));
                    RigProfileMath.ApplyLocal(
                        anchor, def.LocalPosition, def.LocalEulerAngles, Vector3.one);
                    muzzles[def.Id] = anchor;
                }
            }

            return bindings.ToArray();
        }

        private void Clear()
        {
            // 장착 모델을 모델보다 먼저 회수한다 — 모델과 함께 풀로 들어가는 이중 처리 방지.
            for (int i = 0; i < _spawnedVisuals.Count; i++)
            {
                if (_spawnedVisuals[i] != null)
                {
                    PoolManager.Instance.Despawn(_spawnedVisuals[i]);
                }
            }

            _spawnedVisuals.Clear();

            if (_spawnedModel != null)
            {
                PoolManager.Instance.Despawn(_spawnedModel);
                _spawnedModel = null;
            }

            _mounts.Clear();
            _muzzles.Clear();
            ModelRoot = null;
        }

        private static Transform SpawnUnparented(GameObject prefab)
        {
            return (Transform)PoolManager.Instance.Spawn(
                prefab.transform, Vector3.zero, Quaternion.identity);
        }

        private Transform SpawnAsChild(GameObject prefab, Transform parent)
        {
            Transform instance = SpawnUnparented(prefab);
            instance.SetParent(parent, worldPositionStays: false);
            Transform prefabRoot = prefab.transform;
            instance.localPosition = prefabRoot.localPosition;
            instance.localRotation = prefabRoot.localRotation;
            instance.localScale = prefabRoot.localScale;
            return instance;
        }
    }
}
