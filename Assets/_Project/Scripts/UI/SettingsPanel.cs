using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 환경설정 모달. 값은 GameSettings에 즉시 저장되고 화면 설정은 즉시 반영된다.
    /// 자체 캔버스(order 40)라 로비·일시정지 어디에서든 위에 뜬다.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private static readonly Color BoxColor = new(0.08f, 0.09f, 0.11f, 0.98f);
        private static readonly Color TrackColor = new(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color FillColor = new(0.35f, 0.75f, 1f, 1f);
        private static readonly Color ButtonColor = new(0.2f, 0.24f, 0.3f, 1f);
        private static readonly Color LabelColor = new(0.85f, 0.87f, 0.9f, 1f);

        private GameObject _panelRoot;
        private GameSettings _settings;
        private Slider _volumeSlider;
        private Text _volumeValue;
        private Slider _shakeSlider;
        private Text _shakeValue;
        private Text _screenModeLabel;
        private Text _vsyncLabel;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            _settings = GameSettings.Resolve();
            BuildUi();
            _panelRoot.SetActive(false);
        }

        public void Open()
        {
            SyncFromSettings();
            _panelRoot.SetActive(true);
        }

        public void Close()
        {
            _panelRoot.SetActive(false);
        }

        private void BuildUi()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 40);

            Image dim = UiFactory.CreatePanel("Dim", transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            _panelRoot = dim.gameObject;

            Image box = UiFactory.CreatePanel("Box", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(760f, 540f), BoxColor);
            box.raycastTarget = true;
            Transform boxT = box.transform;

            UiFactory.CreateText("Title", boxT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f),
                new Vector2(400f, 60f), "환경설정", 40, TextAnchor.MiddleCenter, Color.white);

            // ── 마스터 볼륨 ──
            CreateRowLabel("VolumeLabel", boxT, -150f, "마스터 볼륨");
            _volumeSlider = UiFactory.CreateSlider("VolumeSlider", boxT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, -150f),
                new Vector2(300f, 36f), TrackColor, FillColor);
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            _volumeValue = CreateValueLabel("VolumeValue", boxT, -150f);

            // ── 화면 흔들림 ──
            CreateRowLabel("ShakeLabel", boxT, -230f, "화면 흔들림");
            _shakeSlider = UiFactory.CreateSlider("ShakeSlider", boxT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, -230f),
                new Vector2(300f, 36f), TrackColor, FillColor);
            _shakeSlider.onValueChanged.AddListener(OnShakeChanged);
            _shakeValue = CreateValueLabel("ShakeValue", boxT, -230f);

            // ── 화면 모드 ──
            CreateRowLabel("ScreenModeLabel", boxT, -310f, "화면 모드");
            Button screenMode = UiFactory.CreateButton("ScreenModeButton", boxT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, -310f),
                new Vector2(300f, 52f), ButtonColor, out _screenModeLabel);
            screenMode.onClick.AddListener(ToggleScreenMode);

            // ── 수직 동기화 ──
            CreateRowLabel("VSyncLabel", boxT, -390f, "수직 동기화");
            Button vsync = UiFactory.CreateButton("VSyncButton", boxT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, -390f),
                new Vector2(300f, 52f), ButtonColor, out _vsyncLabel);
            vsync.onClick.AddListener(ToggleVSync);

            Button close = UiFactory.CreateButton("CloseButton", boxT,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f),
                new Vector2(240f, 56f), new Color(0.32f, 0.2f, 0.2f, 1f), out Text closeLabel);
            closeLabel.text = "닫기";
            closeLabel.fontSize = 24;
            close.onClick.AddListener(Close);
        }

        private static void CreateRowLabel(string name, Transform parent, float y, string text)
        {
            UiFactory.CreateText(name, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, y),
                new Vector2(220f, 40f), text, 26, TextAnchor.MiddleLeft, LabelColor);
        }

        private static Text CreateValueLabel(string name, Transform parent, float y)
        {
            return UiFactory.CreateText(name, parent,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, y),
                new Vector2(90f, 40f), string.Empty, 24, TextAnchor.MiddleCenter, Color.white);
        }

        private void SyncFromSettings()
        {
            _volumeSlider.SetValueWithoutNotify(_settings.MasterVolume);
            _shakeSlider.SetValueWithoutNotify(_settings.ScreenShake);
            _volumeValue.text = FormatPercent(_settings.MasterVolume);
            _shakeValue.text = FormatPercent(_settings.ScreenShake);
            UpdateToggleLabels();
        }

        private void OnVolumeChanged(float value)
        {
            _settings.MasterVolume = value;
            _volumeValue.text = FormatPercent(_settings.MasterVolume);
        }

        private void OnShakeChanged(float value)
        {
            _settings.ScreenShake = value;
            _shakeValue.text = FormatPercent(_settings.ScreenShake);
        }

        private void ToggleScreenMode()
        {
            _settings.Fullscreen = !_settings.Fullscreen;
            _settings.ApplyDisplay();
            UpdateToggleLabels();
        }

        private void ToggleVSync()
        {
            _settings.VSync = !_settings.VSync;
            _settings.ApplyDisplay();
            UpdateToggleLabels();
        }

        private void UpdateToggleLabels()
        {
            _screenModeLabel.text = _settings.Fullscreen ? "전체화면" : "창 모드";
            _screenModeLabel.fontSize = 24;
            _vsyncLabel.text = _settings.VSync ? "켬" : "끔";
            _vsyncLabel.fontSize = 24;
        }

        private static string FormatPercent(float value01) =>
            Mathf.RoundToInt(value01 * 100f) + "%";
    }
}
