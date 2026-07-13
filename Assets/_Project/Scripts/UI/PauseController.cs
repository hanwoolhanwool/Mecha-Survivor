using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.UI
{
    /// <summary>Esc 일시정지 (GDD 2.1). 3택/결과 화면이 이미 시간을 멈춘 상태면 개입하지 않는다.</summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private MechaInput _input;

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
        }
    }
}
