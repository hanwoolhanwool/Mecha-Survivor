using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>업적 모달 — 정의 목록(AchievementData)과 달성 여부(AchievementLog).</summary>
    public sealed class AchievementPanel : MonoBehaviour
    {
        [Tooltip("표시할 업적 정의 (RecordKeeper의 판정 목록과 같은 에셋을 물린다)")]
        [SerializeField] private AchievementData[] _achievements = System.Array.Empty<AchievementData>();

        private GameObject _panelRoot;
        private Text _progressText;
        private Text _listText;
        private readonly StringBuilder _sb = new(1024);

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            Transform box = UiFactory.CreateModal(gameObject, sortingOrder: 40,
                "업적", new Vector2(820f, 640f), out _panelRoot);

            _progressText = UiFactory.CreateText("Progress", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                new Vector2(700f, 36f), string.Empty, 22, TextAnchor.MiddleCenter,
                new Color(0.65f, 0.7f, 0.78f));

            // 오프셋은 pivot(중앙) 기준 — 목록(높이 380)의 상단이 진행도 밑(-140)에 오도록 중심을 -330에 둔다.
            _listText = UiFactory.CreateText("List", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f),
                new Vector2(700f, 380f), string.Empty, 20, TextAnchor.UpperLeft, Color.white);

            UiFactory.CreateCloseButton(box, Close);
            _panelRoot.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            _panelRoot.SetActive(true);
        }

        public void Close()
        {
            _panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            AchievementLog log = AchievementLog.Resolve();

            int unlocked = 0;
            _sb.Clear();
            for (int i = 0; i < _achievements.Length; i++)
            {
                AchievementData data = _achievements[i];
                if (data == null)
                {
                    continue;
                }

                bool done = log.IsUnlocked(data.Id);
                if (done)
                {
                    unlocked++;
                }

                _sb.Append(done ? "■ " : "□ ");
                _sb.Append(data.Title);
                _sb.Append("  —  ");
                _sb.AppendLine(data.Description);
            }

            _listText.text = _sb.ToString();
            _progressText.text = $"달성 {unlocked} / {_achievements.Length}";
        }
    }
}
