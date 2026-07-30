using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 프로필의 본 해석·오브젝트 규약 — MonoBehaviour 밖의 순수 로직 (EditMode 테스트 대상).
    /// </summary>
    public static class RigProfileMath
    {
        /// <summary>빌더가 만드는 마운트 앵커 이름 규약. 씬의 기존 수동 배치와 같은 이름을 쓴다.</summary>
        public static string MountObjectName(string id) => "Mount_" + id;

        /// <summary>빌더가 만드는 총구 앵커 이름 규약.</summary>
        public static string MuzzleObjectName(string id) => "Muzzle_" + id;

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
