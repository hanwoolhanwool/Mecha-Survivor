using UnityEngine;
using UnityEngine.InputSystem;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 스폰 실험실 (SpawnLab 씬 전용). 본편과 동일한 WaveData/DifficultyData로 스폰을 돌리면서
    /// 시간을 스크럽하고 배속을 바꿔 난이도 곡선을 빠르게 검증한다.
    /// 수치 조정은 코드가 아니라 WaveData/DifficultyData 에셋 인스펙터에서 한다 —
    /// 에디터에서 값을 바꾸면 다음 스폰부터 즉시 반영된다.
    ///
    /// 조작: [ / ] 시간 ∓60초 · ; / ' ∓10초 · T 배속(1→2→4→0.5) · P 스폰 정지 · K 전멸.
    /// (무기 전환 ←/→·숫자, WASD 이동 등은 WeaponLab 조작 그대로)
    /// </summary>
    public sealed class SpawnLabController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private SpawnDirector _director;
        [SerializeField] private RunTimer _timer;

        [Tooltip("HUD 갱신 주기(초) — 매 프레임 문자열 할당을 피한다")]
        [SerializeField] private float _hudRefreshInterval = 0.25f;

        private static readonly float[] TimeScales = { 1f, 2f, 4f, 0.5f };
        private int _timeScaleIndex;
        private float _nextHudRefresh;
        private string _hudText = string.Empty;
        private GUIStyle _hudStyle;

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            ReadHotkeys();

            if (Time.unscaledTime >= _nextHudRefresh)
            {
                _nextHudRefresh = Time.unscaledTime + _hudRefreshInterval;
                RefreshHud();
            }
        }

        private void ReadHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.leftBracketKey.wasPressedThisFrame)
            {
                SkipTime(-60f);
            }

            if (kb.rightBracketKey.wasPressedThisFrame)
            {
                SkipTime(60f);
            }

            if (kb.semicolonKey.wasPressedThisFrame)
            {
                SkipTime(-10f);
            }

            if (kb.quoteKey.wasPressedThisFrame)
            {
                SkipTime(10f);
            }

            if (kb.tKey.wasPressedThisFrame)
            {
                _timeScaleIndex = (_timeScaleIndex + 1) % TimeScales.Length;
                Time.timeScale = TimeScales[_timeScaleIndex];
            }

            if (kb.pKey.wasPressedThisFrame && _director != null)
            {
                _director.SpawningPaused = !_director.SpawningPaused;
            }

            if (kb.kKey.wasPressedThisFrame && _director != null)
            {
                _director.DespawnAllAlive();
            }
        }

        private void SkipTime(float seconds)
        {
            if (_timer != null)
            {
                _timer.Skip(seconds);
            }

            if (_director != null)
            {
                _director.ResetSchedule();
            }
        }

        private void RefreshHud()
        {
            float elapsed = _timer != null ? _timer.Elapsed : 0f;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            int alive = EnemyBrain.ActiveEnemies.Count;

            DifficultyData difficulty = _director != null ? _director.Difficulty : null;
            string multipliers = difficulty != null
                ? $"배율  스폰속도 x{difficulty.SpawnRateAt(elapsed):0.00} · " +
                  $"동시수 x{difficulty.MaxAliveMultiplierAt(elapsed):0.00} · " +
                  $"HP x{difficulty.HealthMultiplierAt(elapsed):0.00}"
                : "배율  (DifficultyData 없음 — 전부 x1)";

            string paused = _director != null && _director.SpawningPaused ? "  [스폰 정지]" : string.Empty;

            _hudText =
                $"[스폰 실험실]  {minutes:00}:{seconds:00}  생존 {alive}마리  배속 x{Time.timeScale:0.#}{paused}\n" +
                $"{multipliers}\n" +
                "[ / ] ∓60초   ; / ' ∓10초   T 배속   P 스폰 정지   K 전멸";
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _hudStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
            }

            GUI.Label(new Rect(12f, 110f, 860f, 90f), _hudText, _hudStyle);
        }
    }
}
