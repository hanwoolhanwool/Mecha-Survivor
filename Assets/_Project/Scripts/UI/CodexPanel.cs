using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Gameplay;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 도감 모달 — 적(처치 시 발견)과 무기(사용 시 발견). 미발견은 "???".
    /// 전집은 MetaCatalog, 발견 여부는 EnemyCodex/WeaponArchive.
    /// </summary>
    public sealed class CodexPanel : MonoBehaviour
    {
        [SerializeField] private MetaCatalog _catalog;

        private GameObject _panelRoot;
        private Text _progressText;
        private Text _enemiesText;
        private Text _weaponsText;
        private readonly StringBuilder _sb = new(512);

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            Transform box = UiFactory.CreateModal(gameObject, sortingOrder: 40,
                "도감", new Vector2(860f, 640f), out _panelRoot);

            _progressText = UiFactory.CreateText("Progress", box,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                new Vector2(760f, 36f), string.Empty, 22, TextAnchor.MiddleCenter,
                new Color(0.65f, 0.7f, 0.78f));

            UiFactory.CreateText("EnemiesHeader", box,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -165f),
                new Vector2(340f, 36f), "── 적 기체 ──", 22, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.6f, 0.55f));

            // 오프셋은 pivot(중앙) 기준 — 본문(높이 330)의 상단이 헤더 밑에 오도록 중심을 -365에 둔다.
            _enemiesText = UiFactory.CreateText("Enemies", box,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -365f),
                new Vector2(340f, 330f), string.Empty, 20, TextAnchor.UpperLeft, Color.white);

            UiFactory.CreateText("WeaponsHeader", box,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -165f),
                new Vector2(340f, 36f), "── 아군 무장 ──", 22, TextAnchor.MiddleCenter,
                new Color(0.55f, 0.7f, 0.9f));

            _weaponsText = UiFactory.CreateText("Weapons", box,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -365f),
                new Vector2(340f, 330f), string.Empty, 20, TextAnchor.UpperLeft, Color.white);

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
            if (_catalog == null)
            {
                _progressText.text = "카탈로그 없음";
                return;
            }

            EnemyCodex codex = EnemyCodex.Resolve();
            WeaponArchive archive = WeaponArchive.Resolve();

            int enemyFound = 0;
            _sb.Clear();
            for (int i = 0; i < _catalog.Enemies.Length; i++)
            {
                EnemyData enemy = _catalog.Enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                if (codex.IsDiscovered(enemy.Id))
                {
                    enemyFound++;
                    _sb.AppendLine($"{enemy.DisplayName.PadRight(10)}  처치 {codex.GetKills(enemy.Id):N0}");
                }
                else
                {
                    _sb.AppendLine("???");
                }
            }

            _enemiesText.text = _sb.ToString();

            int weaponFound = 0;
            _sb.Clear();
            for (int i = 0; i < _catalog.Weapons.Length; i++)
            {
                WeaponData weapon = _catalog.Weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                if (archive.HasUsed(weapon.Id))
                {
                    weaponFound++;
                    _sb.AppendLine(weapon.DisplayName);
                }
                else
                {
                    _sb.AppendLine("???");
                }
            }

            _weaponsText.text = _sb.ToString();

            _progressText.text =
                $"적 기체 {enemyFound}/{_catalog.Enemies.Length}    아군 무장 {weaponFound}/{_catalog.Weapons.Length}";
        }
    }
}
