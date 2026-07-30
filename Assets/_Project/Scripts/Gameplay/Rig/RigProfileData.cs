using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 프로필 — 기체/적 모델의 마운트(장착 모델)·총구(투사체 생성 위치) 정의 (Docs/06).
    /// 씬에 손으로 배치하지 않는다: 조정 결과는 전부 여기 저장되고 RigBuilder가 스폰 시 재구성한다.
    /// 수치는 RigLab 씬에서 눈으로 보며 조정·저장한다.
    /// </summary>
    [CreateAssetMenu(fileName = "RigProfileData", menuName = "Mecha Survivor/Rig Profile")]
    public sealed class RigProfileData : ScriptableObject
    {
        [System.Serializable]
        public sealed class MountDef
        {
            [Tooltip("마운트 식별자 (예: BackWeapon, RightHandWeapon)")]
            public string Id = "Mount";

            [Tooltip("Humanoid 본 (아바타가 Humanoid면 이름 독립). LastBone = 사용 안 함")]
            public HumanBodyBones Bone = HumanBodyBones.LastBone;

            [Tooltip("Generic 폴백 — 모델 루트 기준 본 경로 (예: Hips/Spine02/Spine01/Spine). 비우면 모델 루트")]
            public string BonePath = "";

            [Tooltip("본 기준 로컬 값 — RigLab이 저장하는 대상")]
            public Vector3 LocalPosition;
            public Vector3 LocalEulerAngles;
            public Vector3 LocalScale = Vector3.one;

            [Tooltip("마운트에 붙는 무기 모델 (FBX 프리팹). 루트 트랜스폼은 건드리지 않는다 — 크기는 마운트 담당")]
            public GameObject VisualPrefab;

            [Tooltip("이 무기를 보유할 때만 표시 (WeaponMountVisuals 바인딩). 비우면 항상 표시")]
            public WeaponData ShowForWeapon;
        }

        [System.Serializable]
        public sealed class MuzzleDef
        {
            [Tooltip("무기/공격 식별자 — WeaponData.Id 또는 적 공격 이름 (예: laser_cannon, enemy_main)")]
            public string Id = "weapon_id";

            [Tooltip("기준 마운트 Id. 비우면 모델 루트 기준")]
            public string MountId = "";

            [Tooltip("기준 기준 로컬 오프셋 — RigLab이 저장하는 대상")]
            public Vector3 LocalPosition;
            public Vector3 LocalEulerAngles;
        }

        [Tooltip("기체/적 모델 (FBX 프리팹). 씬에 이미 배선된 모델을 쓸 때는 참조만 하고 스폰하지 않는다")]
        public GameObject ModelPrefab;

        [Tooltip("확인용 AnimatorController (적은 없을 수 있음). 모델에 AC가 없을 때만 주입")]
        public RuntimeAnimatorController AnimatorController;

        public MountDef[] Mounts;
        public MuzzleDef[] Muzzles;
    }
}
