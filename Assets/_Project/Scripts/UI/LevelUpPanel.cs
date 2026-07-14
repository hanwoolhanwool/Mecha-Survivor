using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Core;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 레벨업 3택 화면 (GDD 4.1). 열려 있는 동안 게임 시간은 정지한다 (싱글 전용이라 가능).
    /// 연속 레벨업은 대기열로 쌓여 연속으로 뜬다.
    /// </summary>
    public sealed class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private UpgradeService _upgradeService;

        private GameObject _panelRoot;
        private readonly Button[] _buttons = new Button[3];
        private readonly Text[] _buttonLabels = new Text[3];
        private Text _title;
        private Button _rerollButton;
        private Text _rerollLabel;

        private readonly List<UpgradeOffer> _offers = new(3);
        private readonly RerollBudget _rerolls = new(1); // GDD §9-3 결정: 레벨업당 1회
        private int _pendingPicks;
        private bool _isOpen;
        private bool _runEnded;

        private void Awake()
        {
            BuildUi();
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<PlayerLeveledUpEvent>.Subscribe(OnLeveledUp);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<PlayerLeveledUpEvent>.Unsubscribe(OnLeveledUp);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
        }

        private void BuildUi()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 10);

            Image dim = UiFactory.CreatePanel("Dim", transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            _panelRoot = dim.gameObject;

            _title = UiFactory.CreateText("Title", _panelRoot.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f),
                new Vector2(600f, 60f), "LEVEL UP", 44, TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.4f));

            const float buttonWidth = 380f;
            const float buttonHeight = 180f;
            const float gap = 40f;
            float totalWidth = 3 * buttonWidth + 2 * gap;

            for (int i = 0; i < 3; i++)
            {
                float x = -totalWidth * 0.5f + buttonWidth * 0.5f + i * (buttonWidth + gap);
                int index = i;

                _buttons[i] = UiFactory.CreateButton($"Choice{i}", _panelRoot.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f),
                    new Vector2(buttonWidth, buttonHeight),
                    new Color(0.15f, 0.2f, 0.3f, 0.95f), out _buttonLabels[i]);
                _buttons[i].onClick.AddListener(() => OnChoiceClicked(index));
            }

            // 리롤 — 최악의 3택 회피용, 레벨업당 1회 (GDD §9-3).
            _rerollButton = UiFactory.CreateButton("Reroll", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f),
                new Vector2(260f, 56f), new Color(0.3f, 0.22f, 0.1f, 0.95f), out _rerollLabel);
            _rerollButton.onClick.AddListener(OnRerollClicked);
        }

        private void OnLeveledUp(PlayerLeveledUpEvent evt)
        {
            _pendingPicks++;
            TryOpen(evt.NewLevel);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            _runEnded = true;
            if (_isOpen)
            {
                CloseImmediate();
            }
        }

        private void TryOpen(int level = -1)
        {
            if (_isOpen || _runEnded || _pendingPicks <= 0 || _upgradeService == null)
            {
                return;
            }

            _upgradeService.RollThree(_offers);
            if (_offers.Count == 0)
            {
                // 뽑을 것이 없다 (전부 만렙) — 대기열만 소모.
                _pendingPicks = 0;
                return;
            }

            _pendingPicks--;
            _isOpen = true;
            _panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (level > 0)
            {
                _title.text = $"LEVEL UP!  Lv.{level}";
            }

            _rerolls.Reset();
            RefreshOffers();
        }

        private void RefreshOffers()
        {
            for (int i = 0; i < 3; i++)
            {
                bool hasOffer = i < _offers.Count;
                _buttons[i].gameObject.SetActive(hasOffer);
                if (hasOffer)
                {
                    _buttonLabels[i].text = DescribeOffer(_offers[i]);
                }
            }

            bool canReroll = _rerolls.Remaining > 0;
            _rerollButton.interactable = canReroll;
            _rerollLabel.text = canReroll ? $"다시 뽑기 ({_rerolls.Remaining}회)" : "다시 뽑기 소진";
        }

        private void OnRerollClicked()
        {
            if (!_isOpen || !_rerolls.TryConsume())
            {
                return;
            }

            _upgradeService.RollThree(_offers);
            RefreshOffers();
        }

        private string DescribeOffer(in UpgradeOffer offer)
        {
            UpgradeData upgrade = offer.Upgrade;
            int currentLevel = _upgradeService.Inventory.GetLevel(upgrade);

            string header = offer.IsCombination
                ? "★ 조합 ★"
                : currentLevel == 0 ? "신규 획득" : $"강화 Lv.{currentLevel} → {currentLevel + 1}";

            string category = upgrade.Category switch
            {
                UpgradeCategory.Parts => "[파츠]",
                UpgradeCategory.Armor => "[장갑]",
                UpgradeCategory.Energy => "[에너지]",
                _ => string.Empty,
            };

            return $"{header}\n\n{category} {upgrade.DisplayName}\n\n{upgrade.Description}";
        }

        private void OnChoiceClicked(int index)
        {
            if (!_isOpen || index >= _offers.Count)
            {
                return;
            }

            _upgradeService.Take(_offers[index]);
            CloseImmediate();

            // 연속 레벨업이 쌓여 있으면 바로 다음 3택.
            if (_pendingPicks > 0)
            {
                TryOpen();
            }
        }

        private void CloseImmediate()
        {
            _isOpen = false;
            _panelRoot.SetActive(false);

            if (!_runEnded)
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
