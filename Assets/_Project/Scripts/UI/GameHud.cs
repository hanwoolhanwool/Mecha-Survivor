using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Core;
using MechaSurvivor.Gameplay;
using MechaSurvivor.Systems;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 인게임 HUD (GDD 8.1). 가장 중요한 것은 무기별 쿨다운 게이지 —
    /// "지금 어느 무기가 준비됐는가"가 한눈에 보이지 않으면 로테이션 게임이 성립하지 않는다.
    /// 크로스헤어 주변(화면 중앙)은 비워둔다 (GDD 3.6 규칙 1).
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private WeaponSlots _weaponSlots;
        [SerializeField] private PlayerExperience _experience;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private MechaAimer _aimer;

        private static readonly string[] SlotKeyLabels = { "LMB", "RMB", "Q", "E" };

        private const int EnemyLayer = 9;
        private static readonly Color CrosshairNeutral = new(1f, 1f, 1f, 0.9f);
        private static readonly Color CrosshairOnEnemy = new(1f, 0.25f, 0.25f, 1f);

        private Image _healthFill;
        private Text _healthText;
        private Image[] _cooldownFills;
        private Text[] _cooldownLabels;
        private GameObject[] _slotRoots;
        private Text _timerText;
        private Text _levelText;
        private Image _xpFill;
        private Image[] _crosshairParts;
        private GameObject _hitMarker;
        private float _hitMarkerUntil;

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<DamageDealtEvent>.Subscribe(OnDamageDealt);
        }

        private void OnDisable()
        {
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<DamageDealtEvent>.Unsubscribe(OnDamageDealt);
        }

        private void BuildUi()
        {
            UiFactory.CreateCanvas(gameObject, sortingOrder: 0);

            // ── 크로스헤어: 중앙 점 + 4방향 틱 (간격을 둬 중앙 시야는 비운다 — GDD 3.6 규칙 1).
            //    실제 조준 레이 = 카메라 정면 = 화면 정중앙이므로 이 표시가 곧 정확한 탄착점이다.
            BuildCrosshair();

            // ── 체력바: 좌하단.
            Image healthBg = UiFactory.CreatePanel("HealthBg", transform,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 60f),
                new Vector2(300f, 26f), new Color(0f, 0f, 0f, 0.55f));
            _healthFill = UiFactory.CreateBarFill("HealthFill", healthBg.rectTransform,
                new Color(0.9f, 0.25f, 0.25f, 0.95f));
            _healthText = UiFactory.CreateText("HealthText", healthBg.rectTransform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "100 / 100", 16, TextAnchor.MiddleCenter, Color.white);

            // ── 무기 쿨다운 게이지 4개: 하단 중앙.
            _cooldownFills = new Image[WeaponSlots.MaxSlots];
            _cooldownLabels = new Text[WeaponSlots.MaxSlots];
            _slotRoots = new GameObject[WeaponSlots.MaxSlots];

            const float slotWidth = 120f;
            const float slotGap = 14f;
            float totalWidth = WeaponSlots.MaxSlots * slotWidth + (WeaponSlots.MaxSlots - 1) * slotGap;

            for (int i = 0; i < WeaponSlots.MaxSlots; i++)
            {
                float x = -totalWidth * 0.5f + slotWidth * 0.5f + i * (slotWidth + slotGap);

                Image slotBg = UiFactory.CreatePanel($"WeaponSlot{i}", transform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 70f),
                    new Vector2(slotWidth, 44f), new Color(0f, 0f, 0f, 0.55f));
                _slotRoots[i] = slotBg.gameObject;

                _cooldownFills[i] = UiFactory.CreateBarFill($"CooldownFill{i}",
                    slotBg.rectTransform, new Color(0.3f, 0.75f, 1f, 0.9f));

                _cooldownLabels[i] = UiFactory.CreateText($"SlotLabel{i}", slotBg.rectTransform,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    SlotKeyLabels[i], 15, TextAnchor.MiddleCenter, Color.white);
            }

            // ── 타이머: 상단 중앙.
            _timerText = UiFactory.CreateText("Timer", transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                new Vector2(220f, 44f), "00:00", 34, TextAnchor.MiddleCenter, Color.white);

            // ── 레벨 + 경험치 바: 화면 최하단 전폭.
            Image xpBg = UiFactory.CreatePanel("XpBg", transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f),
                new Vector2(-40f, 12f), new Color(0f, 0f, 0f, 0.55f));
            _xpFill = UiFactory.CreateBarFill("XpFill", xpBg.rectTransform,
                new Color(0.4f, 0.9f, 0.4f, 0.95f), padding: 1.5f);

            _levelText = UiFactory.CreateText("Level", transform,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 30f),
                new Vector2(100f, 30f), "Lv.1", 22, TextAnchor.MiddleLeft, Color.white);
        }

        /// <summary>크로스헤어 구축: 외곽선(검정) 위에 흰 틱 — 어떤 배경에서도 보인다.</summary>
        private void BuildCrosshair()
        {
            var root = UiFactory.CreateRect("Crosshair", transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));

            _crosshairParts = new Image[5];
            Color outline = new(0f, 0f, 0f, 0.6f);

            // 중앙 점 (외곽선 → 본체)
            UiFactory.CreatePanel("DotOutline", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(8f, 8f), outline);
            _crosshairParts[0] = UiFactory.CreatePanel("Dot", root, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f), CrosshairNeutral);

            // 4방향 틱 (상하좌우, 중앙에서 8px 띄움)
            Vector2[] offsets = { new(0f, 14f), new(0f, -14f), new(-14f, 0f), new(14f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                bool verticalTick = i < 2;
                Vector2 size = verticalTick ? new Vector2(3f, 10f) : new Vector2(10f, 3f);
                UiFactory.CreatePanel($"TickOutline{i}", root, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), offsets[i], size + new Vector2(3f, 3f), outline);
                _crosshairParts[i + 1] = UiFactory.CreatePanel($"Tick{i}", root,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offsets[i], size, CrosshairNeutral);
            }

            // 히트마커: 45도 기울인 X자 — 명중 순간에만 표시.
            var marker = UiFactory.CreateRect("HitMarker", root,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Color markerColor = new(1f, 0.85f, 0.3f, 0.95f);
            Vector2[] markerOffsets = { new(0f, 10f), new(0f, -10f), new(-10f, 0f), new(10f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                bool verticalTick = i < 2;
                Vector2 size = verticalTick ? new Vector2(3f, 8f) : new Vector2(8f, 3f);
                UiFactory.CreatePanel($"MarkTick{i}", marker, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), markerOffsets[i], size, markerColor);
            }

            _hitMarker = marker.gameObject;
            _hitMarker.SetActive(false);
        }

        private void Update()
        {
            UpdateCooldowns();
            UpdateTimer();
            UpdateExperience();
            UpdateCrosshair();
        }

        /// <summary>적 조준 시 적색 전환 + 히트마커 수명 관리.</summary>
        private void UpdateCrosshair()
        {
            if (_crosshairParts != null)
            {
                bool onEnemy = _aimer != null && _aimer.HasHit && _aimer.HitCollider != null &&
                               _aimer.HitCollider.gameObject.layer == EnemyLayer;
                Color color = onEnemy ? CrosshairOnEnemy : CrosshairNeutral;
                for (int i = 0; i < _crosshairParts.Length; i++)
                {
                    _crosshairParts[i].color = color;
                }
            }

            if (_hitMarker != null && _hitMarker.activeSelf && Time.unscaledTime >= _hitMarkerUntil)
            {
                _hitMarker.SetActive(false);
            }
        }

        /// <summary>플레이어 무기가 데미지를 성립시킨 순간 — 히트마커 점멸.</summary>
        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (string.IsNullOrEmpty(evt.SourceId) || _hitMarker == null)
            {
                return;
            }

            _hitMarker.SetActive(true);
            _hitMarkerUntil = Time.unscaledTime + 0.1f;
        }

        private void UpdateCooldowns()
        {
            if (_weaponSlots == null)
            {
                return;
            }

            for (int i = 0; i < WeaponSlots.MaxSlots; i++)
            {
                Weapon weapon = _weaponSlots.GetWeapon(i);
                bool slotVisible = i < _weaponSlots.UnlockedSlots;
                _slotRoots[i].SetActive(slotVisible);

                if (!slotVisible)
                {
                    continue;
                }

                if (weapon == null || weapon.Data == null)
                {
                    UiFactory.SetBarFill(_cooldownFills[i], 0f);
                    _cooldownLabels[i].text = $"{SlotKeyLabels[i]}  -";
                    continue;
                }

                // 게이지 = 준비도 (가득 = 발사 가능).
                float readiness = 1f - weapon.Cooldown01;
                UiFactory.SetBarFill(_cooldownFills[i], readiness);
                _cooldownFills[i].color = weapon.IsReady
                    ? new Color(0.35f, 0.85f, 1f, 0.95f)
                    : new Color(0.25f, 0.45f, 0.6f, 0.9f);
                _cooldownLabels[i].text =
                    $"{SlotKeyLabels[i]}  {weapon.Data.DisplayName} Lv.{weapon.Level}";
            }
        }

        private void UpdateTimer()
        {
            if (!ServiceLocator.TryGet(out RunTimer timer))
            {
                return;
            }

            int seconds = Mathf.FloorToInt(timer.Elapsed);
            _timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private void UpdateExperience()
        {
            if (_experience == null)
            {
                return;
            }

            _levelText.text = $"Lv.{_experience.Level}";
            UiFactory.SetBarFill(_xpFill,
                _experience.RequiredXp > 0 ? (float)_experience.CurrentXp / _experience.RequiredXp : 0f);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            UiFactory.SetBarFill(_healthFill,
                evt.MaxHealth > 0f ? evt.RemainingHealth / evt.MaxHealth : 0f);
            _healthText.text = $"{Mathf.CeilToInt(evt.RemainingHealth)} / {Mathf.CeilToInt(evt.MaxHealth)}";
        }

        private void Start()
        {
            if (_playerHealth != null)
            {
                UiFactory.SetBarFill(_healthFill, _playerHealth.Current / _playerHealth.MaxHealth);
                _healthText.text =
                    $"{Mathf.CeilToInt(_playerHealth.Current)} / {Mathf.CeilToInt(_playerHealth.MaxHealth)}";
            }
        }
    }
}
