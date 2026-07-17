using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// SFX id → 클립·재생 파라미터 매핑 (밸런싱/연출 수치는 SO로 — CLAUDE.md §2).
    /// 무기 발사음의 id는 WeaponData.Id, 이벤트음은 AudioDirector의 예약 id를 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Mecha Survivor/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class SfxEntry
        {
            [Tooltip("WeaponData.Id 또는 이벤트 id (enemy_death, level_up 등)")]
            public string Id = "sfx";

            public AudioClip Clip;

            [Range(0f, 1f)] public float Volume = 0.8f;

            [Tooltip("재생마다 피치를 이 범위에서 랜덤 — 반복음의 기계적 단조로움 방지")]
            public float PitchMin = 0.95f;
            public float PitchMax = 1.05f;

            [Tooltip("같은 id 재발음 최소 간격(초). 연사·대량 사망의 소리 도배 방지")]
            public float MinInterval = 0.03f;

            [Tooltip("true면 3D 공간 음향 (월드 위치가 있는 소리)")]
            public bool Spatial;
        }

        [SerializeField] private SfxEntry[] _entries = Array.Empty<SfxEntry>();

        private Dictionary<string, SfxEntry> _lookup;

        public bool TryGet(string id, out SfxEntry entry)
        {
            if (_lookup == null)
            {
                BuildLookup();
            }

            return _lookup.TryGetValue(id, out entry);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, SfxEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Id))
                {
                    _lookup[entry.Id] = entry;
                }
            }
        }

        private void OnEnable() => _lookup = null;   // 에디터에서 수정 시 다음 조회에 재구축
    }
}
