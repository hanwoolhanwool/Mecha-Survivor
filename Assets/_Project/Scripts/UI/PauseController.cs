using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.UI
{
    /// <summary>Esc 일시정지 (GDD 2.1). 3택/결과 화면이 이미 시간을 멈춘 상태면 개입하지 않는다.</summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private MechaInput _input;

        [Tooltip("일시정지 중 열 수 있는 환경설정 패널 (씬 배치, 없어도 동작)")]
        [SerializeField] private SettingsPanel _settingsPanel;

        private GameObject _panelRoot;
        private bool _paused;

        private void Awake()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 30);

            Image dim = UiFactory.CreatePanel("Dim", transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.7f));
            dim.raycastTarget = true;
            _panelRoot = dim.gameObject;

            UiFactory.CreateText("Title", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f),
                new Vector2(600f, 80f), "일시정지", 48, TextAnchor.MiddleCenter, Color.white);
            UiFactory.CreateText("Hint", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f),
                new Vector2(600f, 40f), "Esc — 계속하기", 24, TextAnchor.MiddleCenter,
                new Color(0.8f, 0.8f, 0.8f));

            Button settings = UiFactory.CreateButton("SettingsButton", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f),
                new Vector2(300f, 56f), new Color(0.2f, 0.24f, 0.3f, 0.95f), out Text settingsLabel);
            settingsLabel.text = "환경설정";
            settingsLabel.fontSize = 24;
            settings.onClick.AddListener(OpenSettings);

            Button lobby = UiFactory.CreateButton("LobbyButton", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -185f),
                new Vector2(300f, 56f), new Color(0.22f, 0.26f, 0.32f, 0.95f), out Text lobbyLabel);
            lobbyLabel.text = "로비로 나가기";
            lobbyLabel.fontSize = 24;
            lobby.onClick.AddListener(GoToLobby);

            _panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (_input == null || !_input.Frame.PausePressed)
            {
                return;
            }

            // 다른 UI(3택/결과)가 시간을 멈춘 상태면 무시.
            if (!_paused && Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            Toggle();
        }

        private void Toggle()
        {
            _paused = !_paused;
            _panelRoot.SetActive(_paused);
            Time.timeScale = _paused ? 0f : 1f;
            Cursor.lockState = _paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _paused;

            // 재개 시 환경설정이 열린 채 남지 않게 닫는다.
            if (!_paused && _settingsPanel != null)
            {
                _settingsPanel.Close();
            }
        }

        private void OpenSettings()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.Open();
            }
        }

        private void GoToLobby()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lobby");
        }
    }
}
