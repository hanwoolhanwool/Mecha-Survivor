using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 실험실 (RigLab 씬 전용, Docs/06). 카탈로그의 리그 프로필을 RigBuilder로 재구성해
    /// 마운트·총구·애니메이션을 눈으로 확인한다 — 랩에서 보는 것 = 본편에서 나오는 것.
    ///
    /// 조작: Tab 캐릭터↔적 탭 · ←/→ 대상 순환 · 1~5 이동 상태(재누름 8방 순환) ·
    /// 6/7/8 사격 그룹 토글 · 9/0 피격/강피격 · =/- 재생 속도 · 우클릭 드래그/휠 카메라.
    /// </summary>
    public sealed class RigLabController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RigBuilder _builder;
        [SerializeField] private RigLabOrbitCamera _orbitCamera;

        [Header("대상 카탈로그 (Docs/06 §4.1)")]
        [SerializeField] private RigProfileData[] _characterProfiles;
        [SerializeField] private RigProfileData[] _enemyProfiles;

        [Tooltip("HUD 갱신 주기(초) — 매 프레임 문자열 할당을 피한다")]
        [SerializeField] private float _hudRefreshInterval = 0.25f;

        // 애니메이션 파라미터 (MechaAnimationDriver와 동일 규격 — Docs/05 §6)
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int FireTypeHash = Animator.StringToHash("FireType");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int HitHeavyTriggerHash = Animator.StringToHash("HitHeavyTrigger");
        private static readonly int DashTriggerHash = Animator.StringToHash("DashTrigger");
        private static readonly int DashXHash = Animator.StringToHash("DashX");
        private static readonly int DashZHash = Animator.StringToHash("DashZ");
        private static readonly int VerticalYHash = Animator.StringToHash("VerticalY");

        private static readonly string[] TabNames = { "캐릭터", "적" };

        private int _tab;                       // 0=캐릭터, 1=적
        private readonly int[] _selection = new int[2];
        private Animator _animator;
        private float _playbackSpeed = 1f;
        private int _directionIndex;            // 8방 순환 (이동/대쉬 공용)
        private int _fireGroup = -1;            // -1 = 사격 꺼짐
        private string _stateLabel = "FlyIdle";

        private float _nextHudRefresh;
        private string _hudText = string.Empty;
        private GUIStyle _hudStyle;

        private RigProfileData[] CurrentList => _tab == 0 ? _characterProfiles : _enemyProfiles;

        private RigProfileData CurrentProfile
        {
            get
            {
                RigProfileData[] list = CurrentList;
                int index = _selection[_tab];
                return list != null && index >= 0 && index < list.Length ? list[index] : null;
            }
        }

        private void Awake()
        {
            // 비포커스 Play 루프 정지 함정 (Docs/06 §7) — 랩은 백그라운드에서도 돌게 한다.
            Application.runInBackground = true;
        }

        private void Start() => SelectCurrent();

        private void OnDisable()
        {
            Application.runInBackground = false;
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

        // ── 대상 선택 (Docs/06 §4.2-①) ──────────────────────────

        private void SelectCurrent()
        {
            _builder.SetProfile(CurrentProfile);
            _builder.Build();

            _animator = _builder.ModelRoot != null
                ? _builder.ModelRoot.GetComponentInChildren<Animator>()
                : null;

            // WeaponMountVisuals가 없는 랩에서는 장착 모델을 전부 켜서 눈으로 확인한다.
            RigProfileData profile = CurrentProfile;
            if (profile != null && profile.Mounts != null)
            {
                for (int i = 0; i < profile.Mounts.Length; i++)
                {
                    if (_builder.TryGetMount(profile.Mounts[i].Id, out Transform mount))
                    {
                        for (int c = 0; c < mount.childCount; c++)
                        {
                            mount.GetChild(c).gameObject.SetActive(true);
                        }
                    }
                }
            }

            if (_orbitCamera != null)
            {
                _orbitCamera.SetTarget(_builder.ModelRoot != null
                    ? _builder.ModelRoot : _builder.transform);
            }

            _fireGroup = -1;
            _stateLabel = "FlyIdle";
            ApplyPlaybackSpeed();
            ApplyLocomotion(grounded: false, move: Vector2.zero);
        }

        private void CycleTarget(int delta)
        {
            RigProfileData[] list = CurrentList;
            int count = list != null ? list.Length : 0;
            _selection[_tab] = RigLabMath.CycleIndex(_selection[_tab], delta, count);
            SelectCurrent();
        }

        // ── 핫키 ────────────────────────────────────────────────

        private void ReadHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.tabKey.wasPressedThisFrame)
            {
                _tab = 1 - _tab;
                SelectCurrent();
            }

            if (kb.leftArrowKey.wasPressedThisFrame)
            {
                CycleTarget(-1);
            }

            if (kb.rightArrowKey.wasPressedThisFrame)
            {
                CycleTarget(+1);
            }

            ReadAnimationKeys(kb);
        }

        // ── 애니메이션 확인 (Docs/06 §4.2-④) ─────────────────────

        private void ReadAnimationKeys(Keyboard kb)
        {
            if (_animator == null)
            {
                return;
            }

            if (kb.digit1Key.wasPressedThisFrame)
            {
                _stateLabel = "FlyIdle";
                ApplyLocomotion(grounded: false, move: Vector2.zero);
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                AdvanceDirectionIf("Fly");
                Vector2 dir = RigLabMath.DirectionForIndex(_directionIndex);
                _stateLabel = "Fly";
                ApplyLocomotion(grounded: false, move: dir);
            }

            if (kb.digit3Key.wasPressedThisFrame)
            {
                _stateLabel = "GroundIdle";
                ApplyLocomotion(grounded: true, move: Vector2.zero);
            }

            if (kb.digit4Key.wasPressedThisFrame)
            {
                AdvanceDirectionIf("Walk");
                Vector2 dir = RigLabMath.DirectionForIndex(_directionIndex);
                _stateLabel = "Walk";
                ApplyLocomotion(grounded: true, move: dir);
            }

            if (kb.digit5Key.wasPressedThisFrame)
            {
                AdvanceDirectionIf("Dash");
                Vector2 dir = RigLabMath.DirectionForIndex(_directionIndex);
                _stateLabel = "Dash";
                _animator.SetFloat(DashXHash, dir.x);
                _animator.SetFloat(DashZHash, dir.y);
                _animator.SetTrigger(DashTriggerHash);
            }

            if (kb.digit6Key.wasPressedThisFrame)
            {
                ToggleFire(MechaAnimParams.FireGroupLight);
            }

            if (kb.digit7Key.wasPressedThisFrame)
            {
                ToggleFire(MechaAnimParams.FireGroupLauncher);
            }

            if (kb.digit8Key.wasPressedThisFrame)
            {
                ToggleFire(MechaAnimParams.FireGroupHeavy);
            }

            if (kb.digit9Key.wasPressedThisFrame)
            {
                _stateLabel = "Hit";
                _animator.SetTrigger(HitTriggerHash);
            }

            if (kb.digit0Key.wasPressedThisFrame)
            {
                _stateLabel = "HitHeavy";
                _animator.SetTrigger(HitHeavyTriggerHash);
            }

            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
            {
                _playbackSpeed = RigLabMath.StepPlaybackSpeed(_playbackSpeed, 0.1f);
                ApplyPlaybackSpeed();
            }

            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                _playbackSpeed = RigLabMath.StepPlaybackSpeed(_playbackSpeed, -0.1f);
                ApplyPlaybackSpeed();
            }
        }

        /// <summary>같은 이동 상태를 다시 누르면 8방을 한 칸 돌린다.</summary>
        private void AdvanceDirectionIf(string label)
        {
            if (_stateLabel == label)
            {
                _directionIndex = RigLabMath.CycleIndex(_directionIndex, 1, 8);
            }
        }

        private void ApplyLocomotion(bool grounded, Vector2 move)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetBool(IsGroundedHash, grounded);
            _animator.SetFloat(MoveXHash, move.x);
            _animator.SetFloat(MoveZHash, move.y);
            _animator.SetFloat(SpeedHash, 1f);
            _animator.SetFloat(VerticalYHash, 0f);
        }

        private void ToggleFire(int group)
        {
            _fireGroup = _fireGroup == group ? -1 : group;
            _animator.SetBool(FireHash, _fireGroup >= 0);
            _animator.SetInteger(FireTypeHash, Mathf.Max(_fireGroup, 0));
        }

        private void ApplyPlaybackSpeed()
        {
            if (_animator != null)
            {
                _animator.speed = _playbackSpeed;
            }
        }

        // ── HUD ─────────────────────────────────────────────────

        private void RefreshHud()
        {
            RigProfileData profile = CurrentProfile;
            RigProfileData[] list = CurrentList;
            string target = profile != null
                ? $"{profile.name} ({_selection[_tab] + 1}/{list.Length})"
                : "(카탈로그 비어 있음)";
            string anim = _animator != null
                ? $"{_stateLabel}  사격 {(_fireGroup < 0 ? "꺼짐" : _fireGroup.ToString())}  속도 x{_playbackSpeed:0.0}"
                : "없음 (AC 없는 대상 — 스킵)";

            _hudText =
                $"[리그 실험실]  탭 {TabNames[_tab]}  대상 {target}\n" +
                $"애니메이션  {anim}\n" +
                "Tab 탭 전환   ←/→ 대상   1 FlyIdle  2 Fly(8방)  3 GroundIdle  4 Walk(8방)  5 Dash(8방)\n" +
                "6/7/8 사격 토글(경/발사기/중)   9 피격  0 강피격   =/- 재생 속도   우클릭 드래그/휠 카메라";
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _hudStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
            }

            GUI.Label(new Rect(12f, 12f, 1000f, 120f), _hudText, _hudStyle);
        }
    }
}
