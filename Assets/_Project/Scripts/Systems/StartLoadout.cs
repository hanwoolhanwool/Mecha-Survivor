using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 출격 시작 로드아웃 선택 상태 (로비 격납고 패널이 쓰고, Game 씬의 StartLoadoutApplier가 읽는다).
    /// 지금은 "시작 무기" 하나지만, 차후 "다른 무기를 장착한 기체 선택"으로 확장하는 자리다 —
    /// 저장되는 것은 로드아웃 ID뿐이라 의미를 기체로 바꿔도 이 클래스는 그대로다.
    /// 선택이 비어 있으면 게임은 씬에 미리 배치된 기본 로드아웃으로 출격한다(기능 제거 시 안전).
    /// </summary>
    public sealed class StartLoadout
    {
        private const string SelectedKey = "loadout.selected";

        private readonly IKeyValueStore _store;
        private string _selectedId;

        public StartLoadout(IKeyValueStore store)
        {
            _store = store;
            _selectedId = store.GetString(SelectedKey, string.Empty);
        }

        /// <summary>선택된 로드아웃 ID. 빈 문자열 = 기본 로드아웃.</summary>
        public string SelectedId
        {
            get => _selectedId;
            set
            {
                _selectedId = value ?? string.Empty;
                _store.SetString(SelectedKey, _selectedId);
                _store.Save();
            }
        }

        public bool HasSelection => !string.IsNullOrEmpty(_selectedId);

        /// <summary>전역 인스턴스 — GameSettings.Resolve와 같은 lazy 등록 구조.</summary>
        public static StartLoadout Resolve()
        {
            if (!ServiceLocator.TryGet(out StartLoadout loadout))
            {
                loadout = new StartLoadout(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(loadout);
            }

            return loadout;
        }
    }
}
