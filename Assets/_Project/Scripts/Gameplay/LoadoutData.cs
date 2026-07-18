using System;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 출격 로드아웃 선택지 목록 (로비 격납고 패널·StartLoadoutApplier가 읽는다).
    /// v1은 "시작 무기"만 다르지만, 항목이 로드아웃(기체) 단위인 이유는 차후
    /// "다른 무기를 장착한 기체 선택"으로 확장하기 위해서다 — 그때는 Entry에
    /// 기체 모델/스탯 필드를 더하면 되고, 선택 저장(StartLoadout.SelectedId)은 그대로다.
    /// 이 기능을 제거하려면: 이 에셋 참조와 StartLoadoutApplier·HangarPanel만 걷어내면
    /// 게임은 씬에 미리 배치된 기본 로드아웃으로 되돌아간다.
    /// </summary>
    [CreateAssetMenu(fileName = "LoadoutData", menuName = "Mecha Survivor/Loadout Data")]
    public sealed class LoadoutData : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("저장 키로 쓰이는 고유 ID (예: loadout_gatling). 출시 후 변경 금지")]
            public string Id = "loadout";

            public string DisplayName = "기체";

            [TextArea]
            public string Description = "";

            [Header("시작 무장 세트 (차후: 기체 모델·스탯 필드가 여기에 추가된다)")]
            [Tooltip("UpgradeInventory 초기 무기 지급을 이 세트로 교체한다 — 인벤토리를 거쳐야 강화/조합과 연동된다")]
            public PartUpgradeData[] WeaponParts = Array.Empty<PartUpgradeData>();
        }

        public Entry[] Entries = Array.Empty<Entry>();

        public Entry Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i] != null && Entries[i].Id == id)
                {
                    return Entries[i];
                }
            }

            return null;
        }
    }
}
