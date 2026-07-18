using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Gameplay;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 전적 모달 — 평생 요약(PlayerRecords) + 무기별 평생 아카이브(WeaponArchive).
    /// ResultPanel이 "이번 판"이라면 여기는 "지금까지 전부"다.
    /// </summary>
    public sealed class ArchivePanel : MonoBehaviour
    {
        [Tooltip("무기 ID → 표시 이름 변환용")]
        [SerializeField] private MetaCatalog _catalog;

        private GameObject _panelRoot;
        private Text _summaryText;
        private Text _weaponsText;
        private readonly StringBuilder _sb = new(1024);

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            Transform box = UiFactory.CreateModal(gameObject, sortingOrder: 40,
                "전적", new Vector2(860f, 640f), out _panelRoot);

            _summaryText = UiFactory.CreateText("Summary", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                new Vector2(760f, 60f), string.Empty, 24, TextAnchor.MiddleCenter, Color.white);

            UiFactory.CreateText("WeaponsHeader", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f),
                new Vector2(760f, 36f), "── 무기별 평생 기록 ──", 22, TextAnchor.MiddleCenter,
                new Color(0.65f, 0.7f, 0.78f));

            // 오프셋은 pivot(중앙) 기준 — 표(높이 330)의 상단이 헤더 밑(-200)에 오도록 중심을 -370에 둔다.
            _weaponsText = UiFactory.CreateText("Weapons", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f),
                new Vector2(760f, 330f), string.Empty, 20, TextAnchor.UpperLeft, Color.white);

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
            PlayerRecords records = PlayerRecords.Resolve();
            int best = Mathf.FloorToInt(records.BestSurvivalSeconds);
            _summaryText.text =
                $"최고 생존  {best / 60:00}:{best % 60:00}    출격 {records.TotalRuns}회    " +
                $"클리어 {records.TotalVictories}회    누적 처치 {records.TotalKills:N0}";

            WeaponArchive archive = WeaponArchive.Resolve();
            if (archive.Entries.Count == 0)
            {
                _weaponsText.text = "아직 기록이 없다 — 첫 출격에서 만들어진다.";
                return;
            }

            // 열람 시 1회 정렬 — 원본은 건드리지 않는다.
            var sorted = new List<WeaponArchive.Entry>(archive.Entries);
            sorted.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));

            _sb.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                WeaponArchive.Entry e = sorted[i];
                _sb.Append(ResolveName(e.WeaponId).PadRight(12));
                _sb.Append($"  총 데미지 {e.TotalDamage:N0}   처치 {e.Kills:N0}   ");
                _sb.AppendLine($"발사 {e.Shots:N0}   최고 한 방 {e.BestSingleHit:F0}   사용 {e.RunsUsed}판");
            }

            _weaponsText.text = _sb.ToString();
        }

        private string ResolveName(string weaponId)
        {
            if (_catalog != null)
            {
                for (int i = 0; i < _catalog.Weapons.Length; i++)
                {
                    WeaponData data = _catalog.Weapons[i];
                    if (data != null && data.Id == weaponId)
                    {
                        return data.DisplayName;
                    }
                }
            }

            return weaponId;
        }
    }
}
