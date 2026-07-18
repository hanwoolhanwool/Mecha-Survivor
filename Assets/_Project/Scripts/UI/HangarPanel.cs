using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Gameplay;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 격납고 — 출격 로드아웃 선택 모달. 선택은 StartLoadout에 저장되고
    /// Game 씬의 StartLoadoutApplier가 적용한다. v1은 시작 무기 차이지만
    /// 항목 단위는 "기체"다 — 차후 기체 모델 선택으로 확장하는 자리 (LoadoutData 참조).
    /// 이 패널과 StartLoadoutApplier만 제거하면 기능 전체가 사라진다.
    /// </summary>
    public sealed class HangarPanel : MonoBehaviour
    {
        private const string DefaultOptionId = "";

        private static readonly Color NormalColor = new(0.2f, 0.24f, 0.3f, 1f);
        private static readonly Color SelectedColor = new(0.16f, 0.42f, 0.2f, 1f);

        [SerializeField] private LoadoutData _loadouts;

        private GameObject _panelRoot;
        private StartLoadout _selection;
        private Text _descriptionText;
        private readonly List<Image> _optionImages = new();
        private readonly List<string> _optionIds = new();

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            _selection = StartLoadout.Resolve();
            BuildUi();
            _panelRoot.SetActive(false);
        }

        public void Open()
        {
            RefreshHighlight();
            _panelRoot.SetActive(true);
        }

        public void Close()
        {
            _panelRoot.SetActive(false);
        }

        private void BuildUi()
        {
            Transform box = UiFactory.CreateModal(gameObject, sortingOrder: 40,
                "격납고 — 출격 기체", new Vector2(760f, 620f), out _panelRoot);

            AddOption(box, 0, DefaultOptionId, "기본 로드아웃", "씬에 배치된 표준 무장 그대로 출격한다.");

            if (_loadouts != null)
            {
                for (int i = 0; i < _loadouts.Entries.Length; i++)
                {
                    LoadoutData.Entry entry = _loadouts.Entries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    AddOption(box, i + 1, entry.Id,
                        $"{entry.DisplayName}  —  {BuildWeaponNames(entry)}", entry.Description);
                }
            }

            _descriptionText = UiFactory.CreateText("Description", box,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f),
                new Vector2(640f, 60f), string.Empty, 20, TextAnchor.UpperCenter,
                new Color(0.65f, 0.7f, 0.78f));

            UiFactory.CreateCloseButton(box, Close);
        }

        private void AddOption(Transform box, int index, string id, string label, string description)
        {
            Button button = UiFactory.CreateButton($"Option_{index}", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f - index * 72f),
                new Vector2(620f, 60f), NormalColor, out Text text);
            text.text = label;
            text.fontSize = 24;

            _optionImages.Add(button.GetComponent<Image>());
            _optionIds.Add(id);

            string captured = id;
            string capturedDesc = description;
            button.onClick.AddListener(() =>
            {
                _selection.SelectedId = captured;
                _descriptionText.text = capturedDesc;
                RefreshHighlight();
            });
        }

        private static string BuildWeaponNames(LoadoutData.Entry entry)
        {
            if (entry.WeaponParts == null || entry.WeaponParts.Length == 0)
            {
                return "?";
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < entry.WeaponParts.Length; i++)
            {
                PartUpgradeData part = entry.WeaponParts[i];
                if (part == null || part.Weapon == null)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(" · ");
                }

                sb.Append(part.Weapon.DisplayName);
            }

            return sb.Length > 0 ? sb.ToString() : "?";
        }

        private void RefreshHighlight()
        {
            string current = _selection.SelectedId;
            for (int i = 0; i < _optionImages.Count; i++)
            {
                _optionImages[i].color = _optionIds[i] == current ? SelectedColor : NormalColor;
            }
        }
    }
}
