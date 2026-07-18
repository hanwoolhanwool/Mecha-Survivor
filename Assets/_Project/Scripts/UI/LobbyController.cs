using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 로비 화면 (Boot → Lobby → Game). 출격/환경설정/종료와 영구 전적 표시.
    /// UI는 다른 화면과 마찬가지로 UiFactory 코드 구축 — 씬에는 이 컴포넌트와 배경 연출만 둔다.
    /// </summary>
    public sealed class LobbyController : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private SettingsPanel _settingsPanel;
        [SerializeField] private HangarPanel _hangarPanel;
        [SerializeField] private ArchivePanel _archivePanel;
        [SerializeField] private CodexPanel _codexPanel;
        [SerializeField] private AchievementPanel _achievementPanel;

        [Tooltip("천천히 회전시킬 배경 전시물(기체 모형). 없어도 동작한다")]
        [SerializeField] private Transform _showcase;
        [SerializeField] private float _showcaseSpinDegreesPerSecond = 16f;

        private void Awake()
        {
            // 결과 화면(timeScale 0)이나 커서 잠금 상태에서 넘어와도 로비는 항상 정상 상태로.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameSettings.Resolve().ApplyDisplay();
            BuildUi();
        }

        private void Update()
        {
            if (_showcase != null)
            {
                _showcase.Rotate(0f, _showcaseSpinDegreesPerSecond * Time.deltaTime, 0f);
            }
        }

        private void BuildUi()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 10);

            UiFactory.CreateText("Title", transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f),
                new Vector2(1200f, 100f), "MECHA SURVIVOR", 78, TextAnchor.MiddleCenter, Color.white);

            UiFactory.CreateText("Subtitle", transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f),
                new Vector2(900f, 40f), "끝없는 기계 무리 속에서 생존하라", 24,
                TextAnchor.MiddleCenter, new Color(0.65f, 0.7f, 0.78f));

            // 버튼 열 — 화면 좌측. 우측은 기체 전시물 자리.
            Button start = UiFactory.CreateButton("StartButton", transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 170f),
                new Vector2(380f, 84f), new Color(0.16f, 0.42f, 0.2f, 0.95f), out Text startLabel);
            startLabel.text = "출  격";
            startLabel.fontSize = 36;
            start.onClick.AddListener(StartRun);

            CreateMenuButton("HangarButton", 95f, "격납고 — 출격 기체",
                () => OpenPanel(_hangarPanel != null ? (System.Action)_hangarPanel.Open : null));
            CreateMenuButton("ArchiveButton", 25f, "전적",
                () => OpenPanel(_archivePanel != null ? (System.Action)_archivePanel.Open : null));
            CreateMenuButton("CodexButton", -45f, "도감",
                () => OpenPanel(_codexPanel != null ? (System.Action)_codexPanel.Open : null));
            CreateMenuButton("AchievementButton", -115f, "업적",
                () => OpenPanel(_achievementPanel != null ? (System.Action)_achievementPanel.Open : null));
            CreateMenuButton("SettingsButton", -185f, "환경설정",
                () => OpenPanel(_settingsPanel != null ? (System.Action)_settingsPanel.Open : null));

            Button quit = UiFactory.CreateButton("QuitButton", transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -262f),
                new Vector2(380f, 56f), new Color(0.3f, 0.18f, 0.18f, 0.95f), out Text quitLabel);
            quitLabel.text = "게임 종료";
            quitLabel.fontSize = 24;
            quit.onClick.AddListener(Quit);

            UiFactory.CreateText("Records", transform,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(320f, 50f),
                new Vector2(600f, 60f), BuildRecordsText(), 22, TextAnchor.MiddleLeft,
                new Color(0.6f, 0.65f, 0.72f));

            UiFactory.CreateText("Version", transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-80f, 30f),
                new Vector2(200f, 30f), $"v{Application.version}", 18,
                TextAnchor.MiddleRight, new Color(0.4f, 0.45f, 0.5f));
        }

        private static string BuildRecordsText()
        {
            PlayerRecords records = PlayerRecords.Resolve();
            if (!records.HasAnyRun)
            {
                return "첫 출격을 기다리는 중 — 아직 전적이 없다";
            }

            int best = Mathf.FloorToInt(records.BestSurvivalSeconds);
            return $"최고 생존  {best / 60:00}:{best % 60:00}    " +
                   $"출격 {records.TotalRuns}회 (클리어 {records.TotalVictories})    " +
                   $"누적 처치 {records.TotalKills:N0}";
        }

        private Button CreateMenuButton(string name, float y, string label, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.CreateButton(name, transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, y),
                new Vector2(380f, 56f), new Color(0.2f, 0.24f, 0.3f, 0.95f), out Text text);
            text.text = label;
            text.fontSize = 24;
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>모달은 한 번에 하나만 — 다른 패널을 닫고 연다.</summary>
        private void OpenPanel(System.Action open)
        {
            CloseAllPanels();
            open?.Invoke();
        }

        private void CloseAllPanels()
        {
            if (_settingsPanel != null) _settingsPanel.Close();
            if (_hangarPanel != null) _hangarPanel.Close();
            if (_archivePanel != null) _archivePanel.Close();
            if (_codexPanel != null) _codexPanel.Close();
            if (_achievementPanel != null) _achievementPanel.Close();
        }

        private void StartRun()
        {
            SceneManager.LoadScene(_gameSceneName);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
