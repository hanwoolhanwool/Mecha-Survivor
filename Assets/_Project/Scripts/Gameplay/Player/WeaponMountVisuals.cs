using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 장착 무기 외형 — 특정 무기를 보유하면 기체에 붙은 마운트 모델을 켠다
    /// (예: 미사일 포드 → 등의 트윈 로켓 캐논, 레이저 캐논 → 오른손 레일건).
    /// 장착 경로가 여러 곳(초기 자동 장착·업그레이드·무기 실험실 교체)이라
    /// 이벤트 대신 슬롯 상태를 폴링한다 — 슬롯 4개 비교뿐이라 비용은 무시 가능.
    /// </summary>
    public sealed class WeaponMountVisuals : MonoBehaviour
    {
        [System.Serializable]
        public struct MountBinding
        {
            [Tooltip("이 무기를 보유하면")]
            public WeaponData Weapon;

            [Tooltip("이 모델을 켠다 (본에 붙은 외형 오브젝트)")]
            public GameObject Model;
        }

        [SerializeField] private WeaponSlots _slots;
        [SerializeField] private MountBinding[] _bindings;

        private void OnEnable() => Apply(_slots, _bindings);

        private void Update() => Apply(_slots, _bindings);

        /// <summary>보유 여부에 맞춰 모델 활성 상태를 동기화한다. 정적 — EditMode 테스트 대상.</summary>
        public static void Apply(WeaponSlots slots, MountBinding[] bindings)
        {
            if (slots == null || bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                WeaponData weapon = bindings[i].Weapon;
                GameObject model = bindings[i].Model;
                if (weapon == null || model == null)
                {
                    continue;
                }

                bool show = slots.Has(weapon);
                if (model.activeSelf != show)
                {
                    model.SetActive(show);
                }
            }
        }
    }
}
