using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MechaSurvivor.Core;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 런 종료 통계 화면 (GDD 6장). "내 빌드가 무엇을 했는가"를 보여준다 —
    /// 핵심은 무기별 기여도. 다음 판에 무엇을 다르게 할지 배우는 유일한 창구.
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        private GameObject _panelRoot;
        private Text _titleText;
        private Text _statsText;
        private readonly StringBuilder _sb = new(1024);

        private void Awake()
        {
            BuildUi();
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
        }

        private void BuildUi()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 20);

            Image dim = UiFactory.CreatePanel("Dim", transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.82f));
            dim.raycastTarget = true;
            _panelRoot = dim.gameObject;

            _titleText = UiFactory.CreateText("Title", _panelRoot.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                new Vector2(800f, 70f), "RESULT", 52, TextAnchor.MiddleCenter, Color.white);

            _statsText = UiFactory.CreateText("Stats", _panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
                new Vector2(900f, 600f), string.Empty, 24, TextAnchor.UpperCenter, Color.white);

            Button restart = UiFactory.CreateButton("Restart", _panelRoot.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f),
                new Vector2(320f, 70f), new Color(0.2f, 0.35f, 0.2f, 0.95f), out Text label);
            label.text = "다시 출격";
            label.fontSize = 28;
            restart.onClick.AddListener(Restart);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            _panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _titleText.text = evt.Victory ? "생존 성공 — 클리어!" : "기체 파괴 — 격추됨";
            _titleText.color = evt.Victory
                ? new Color(0.5f, 1f, 0.5f)
                : new Color(1f, 0.45f, 0.45f);

            _statsText.text = BuildStatsText(evt);
        }

        private string BuildStatsText(in RunEndedEvent evt)
        {
            _sb.Clear();

            int seconds = Mathf.FloorToInt(evt.DurationSeconds);
            _sb.AppendLine($"생존 시간  {seconds / 60:00}:{seconds % 60:00}");

            if (!ServiceLocator.TryGet(out StatisticsRecorder recorder))
            {
                return _sb.ToString();
            }

            RunStatistics stats = recorder.Current;
            _sb.AppendLine($"총 처치  {stats.TotalKills}    도달 레벨  Lv.{stats.FinalLevel}    획득 경험치  {stats.TotalExperience}");

            if (stats.MaxSingleHit > 0f)
            {
                _sb.AppendLine($"최고 데미지  {stats.MaxSingleHit:F0}  ({stats.MaxSingleHitWeaponId})");
            }

            _sb.AppendLine($"지상 체류  {stats.GroundedRatio:P0}  /  공중 체류  {1f - stats.GroundedRatio:P0}");

            _sb.AppendLine();
            _sb.AppendLine("── 무기별 기여도 ──");

            // 총 데미지 내림차순 정렬 사본 (원본 통계는 건드리지 않는다). 런 종료 1회라 할당 무방.
            var sorted = new System.Collections.Generic.List<RunStatistics.WeaponStat>(stats.Weapons);
            sorted.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));

            for (int i = 0; i < sorted.Count; i++)
            {
                RunStatistics.WeaponStat w = sorted[i];
                float share = stats.TotalDamage > 0f ? w.TotalDamage / stats.TotalDamage : 0f;
                int bars = Mathf.RoundToInt(share * 20f);
                _sb.Append(w.WeaponId.PadRight(14));
                _sb.Append(new string('█', bars).PadRight(21));
                _sb.Append($"{w.TotalDamage:F0} dmg  ({share:P0})  발사 {w.ShotsFired}  처치 {w.Kills}");
                _sb.AppendLine(w.TotalSamples > 0 ? $"  쿨 대기 {w.DowntimeRatio:P0}" : string.Empty);
            }

            return _sb.ToString();
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Boot");
        }
    }
}
