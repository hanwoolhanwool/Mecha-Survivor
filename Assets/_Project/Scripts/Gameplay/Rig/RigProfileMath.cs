using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 프로필의 본 해석·오브젝트 규약 — MonoBehaviour 밖의 순수 로직 (EditMode 테스트 대상).
    /// </summary>
    public static class RigProfileMath
    {
        /// <summary>적 주 공격 총구의 프로필 내 식별자 규약 (Docs/06 §3.1).</summary>
        public const string EnemyMainMuzzleId = "enemy_main";

        /// <summary>빌더가 만드는 마운트 앵커 이름 규약. 씬의 기존 수동 배치와 같은 이름을 쓴다.</summary>
        public static string MountObjectName(string id) => "Mount_" + id;

        /// <summary>
        /// 총구 앵커 이름 접두사. 총구는 마운트의 자식으로 붙으므로, 마운트 자식을 켜고 끌 때
        /// 이 접두사로 총구를 걸러내야 한다 — 총구까지 끄면 발사 지점이 사라진다.
        /// </summary>
        public const string MuzzleNamePrefix = "Muzzle_";

        /// <summary>빌더가 만드는 총구 앵커 이름 규약.</summary>
        public static string MuzzleObjectName(string id) => MuzzleNamePrefix + id;

        // ── 손 규약 (Docs/06 §3.4) ──────────────────────────────
        //
        // 같은 무기를 좌우 어느 손에도 들 수 있게 되면서 "무기 Id 하나 = 총구 하나"가 깨졌다.
        // 총구는 (무기 Id, 손)으로 식별하고, 손이 Any인 것(등 마운트·적)은 종전대로 Id만 쓴다.

        /// <summary>총구 사전 키 — 손이 Any면 무기 Id 그대로 (기존 프로필·적과 호환).</summary>
        public static string MuzzleKey(string id, MountHand hand)
        {
            switch (hand)
            {
                case MountHand.Right:
                    return id + "@R";
                case MountHand.Left:
                    return id + "@L";
                default:
                    return id;
            }
        }

        /// <summary>
        /// 마운트의 손 조건이 장착 손과 맞는가. 어느 한쪽이 Any면 통과 —
        /// 손 조건 없는 마운트는 항상 표시되고, 손을 특정하지 않은 조회는 좌우를 안 가린다.
        /// </summary>
        public static bool MatchesHand(MountHand mountHand, MountHand hand) =>
            mountHand == MountHand.Any || hand == MountHand.Any || mountHand == hand;

        /// <summary>장착 손 → 마운트 손 조건. 실제 장착은 항상 좌우 중 하나라 Any가 안 나온다.</summary>
        public static MountHand ToMountHand(WeaponHand hand) =>
            hand == WeaponHand.Left ? MountHand.Left : MountHand.Right;

        /// <summary>
        /// 총구가 딸린 마운트의 손. 총구에 손 필드를 따로 두지 않는 이유 — 총구는 마운트의
        /// 자식이라 손이 갈릴 수 없고, 두 곳에 적으면 어긋날 여지만 생긴다.
        /// </summary>
        public static MountHand ResolveMuzzleHand(RigProfileData profile, string mountId)
        {
            if (profile == null || profile.Mounts == null || string.IsNullOrEmpty(mountId))
            {
                return MountHand.Any;
            }

            for (int i = 0; i < profile.Mounts.Length; i++)
            {
                if (profile.Mounts[i].Id == mountId)
                {
                    return profile.Mounts[i].Hand;
                }
            }

            return MountHand.Any;
        }

        /// <summary>
        /// 본 해석: Humanoid 본 우선(아바타가 Humanoid이고 Bone이 유효할 때),
        /// 실패하면 모델 루트 기준 경로 폴백. 둘 다 실패하면 null.
        /// </summary>
        public static Transform ResolveBone(
            Animator animator, HumanBodyBones bone, string bonePath, Transform modelRoot)
        {
            if (animator != null && animator.isHuman
                && bone >= 0 && bone < HumanBodyBones.LastBone)
            {
                Transform result = animator.GetBoneTransform(bone);
                if (result != null)
                {
                    return result;
                }
            }

            return FindByPath(modelRoot, bonePath);
        }

        /// <summary>
        /// 모델 루트 기준 경로 탐색. 빈 경로는 루트 자신을 뜻한다 (총구 MountId "" 규약과 동일).
        /// 경로가 틀리면 null — 호출부가 경고를 낸다.
        /// </summary>
        public static Transform FindByPath(Transform modelRoot, string path)
        {
            if (modelRoot == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(path) ? modelRoot : modelRoot.Find(path);
        }

        /// <summary>프로필의 로컬 값을 트랜스폼에 적용한다 (마운트·총구 공통).</summary>
        public static void ApplyLocal(
            Transform target, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            target.localPosition = localPosition;
            target.localRotation = Quaternion.Euler(localEulerAngles);
            target.localScale = localScale;
        }

        /// <summary>
        /// 적 총구 오프셋 해석 (Docs/06 §3.3): 리그 프로필에 enemy_main 총구(모델 루트 기준)가
        /// 있으면 그 값이 우선, 없으면 EnemyData.MuzzleOffset 폴백.
        /// </summary>
        public static Vector3 ResolveEnemyMuzzleOffset(EnemyData data)
        {
            if (data == null)
            {
                return Vector3.zero;
            }

            RigProfileData profile = data.RigProfile;
            if (profile != null && profile.Muzzles != null)
            {
                for (int i = 0; i < profile.Muzzles.Length; i++)
                {
                    RigProfileData.MuzzleDef def = profile.Muzzles[i];
                    if (def.Id == EnemyMainMuzzleId && string.IsNullOrEmpty(def.MountId))
                    {
                        return def.LocalPosition;
                    }
                }
            }

            return data.MuzzleOffset;
        }

        /// <summary>
        /// 부모 아래 이름으로 앵커를 찾고 없으면 만든다. 풀 재사용된 모델에 잔존 앵커가
        /// 있어도 중복 생성하지 않기 위한 find-or-create.
        /// </summary>
        public static Transform GetOrCreateAnchor(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, worldPositionStays: false);
            return anchor;
        }
    }
}
