using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 실험실 (RigLab 씬 전용, Docs/06). 카탈로그의 리그 프로필을 RigBuilder로 재구성해
    /// 마운트·총구·애니메이션을 눈으로 확인하고, 조정 결과를 프로필(SO)에 저장한다 —
    /// 랩에서 보는 것 = 본편에서 나오는 것.
    ///
    /// 조작: Tab 캐릭터↔적 탭 · ←/→ 대상 순환 · W 장착 무기 순환(조정 항목도 그 무기 것만) ·
    /// [ ] 마운트/총구 선택 ·
    /// (선택 중) 화살표+PgUp/Dn 이동, Shift=회전, Ctrl=미세, Alt+PgUp/Dn=스케일 ·
    /// R 리셋 · F 시험 발사 · S 저장 · 1~5 이동 상태(8방) · 6/7/8 사격 토글 ·
    /// 9/0 피격 · P 포즈 클립 순환 · =/- 재생 속도 · 우클릭 드래그/휠 카메라.
    /// </summary>
    public sealed class RigLabController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RigBuilder _builder;
        [SerializeField] private RigLabOrbitCamera _orbitCamera;
        [SerializeField] private RigLabGizmo _gizmo;

        [Header("대상 카탈로그 (Docs/06 §4.1)")]
        [SerializeField] private RigProfileData[] _characterProfiles;
        [SerializeField] private RigProfileData[] _enemyProfiles;

        [Header("포즈 클립 (Docs/07 — 전시용, AC에 없는 클립)")]
        [Tooltip("P 키로 순환 재생. AnimatorController를 거치지 않고 PlayableGraph로 직접 물린다 — "
            + "게임용 AC_Mecha는 건드리지 않는다")]
        [SerializeField] private AnimationClip[] _poseClips;

        [Header("배치")]
        [Tooltip("비행 상태에서 바닥 위로 띄우는 높이. 지상 상태는 최저점이 바닥에 닿게 자동 보정")]
        [SerializeField] private float _flyHoverHeight = 1.2f;

        [Header("넛지 스텝")]
        [SerializeField] private float _moveStep = 0.05f;
        [SerializeField] private float _moveStepFine = 0.005f;
        [SerializeField] private float _rotateStep = 5f;
        [SerializeField] private float _rotateStepFine = 0.5f;
        [SerializeField] private float _scaleStep = 0.05f;
        [SerializeField] private float _scaleStepFine = 0.005f;

        [Tooltip("HUD 갱신 주기(초) — 매 프레임 문자열 할당을 피한다")]
        [SerializeField] private float _hudRefreshInterval = 0.25f;

        [Header("UI 패널 (H 토글)")]
        [SerializeField] private float _panelWidth = 280f;

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

        private int _poseIndex = -1;            // -1 = 포즈 재생 안 함
        private PlayableGraph _poseGraph;
        private AnimationClipPlayable _posePlayable;

        private int _adjustIndex = -1;          // -1=조정 안 함, 0..M-1=마운트, M..=총구

        // 장착 무기로 걸러낸 조정 항목의 전역 인덱스. 목록·[ ] 순환이 이걸 따르고,
        // 저장·dirty 검사는 여전히 프로필 전체를 훑는다 (안 보이는 항목의 값도 지켜야 한다).
        private readonly List<int> _visibleAdjust = new();

        private bool _dirty;
        private bool _grounded;                 // 현재 이동 상태 (루트 높이 결정)
        private float _groundOffset;            // 모델 최저점→바닥 보정 (대상 선택 시 자동 측정)

        private Weapon _testWeapon;             // 시험 발사용 실제 무기 인스턴스
        private string _testWeaponId;

        // ── 장착 무기 (본편 WeaponMountVisuals를 랩에서 수동 재현) ──
        private static readonly WeaponData[] NoWeapons = new WeaponData[0];
        private readonly List<WeaponData> _equipBuffer = new();
        private WeaponData[] _equippables = NoWeapons;
        private int _equipIndex = -1;           // -1 = 전체 표시(랩 기본), 0.. = 그 무기만
        private MountHand _equipHand = MountHand.Any;   // 장착 손 (Any = 좌우 다 표시)

        private static readonly string[] HandNames = { "양손 전부", "오른손", "왼손" };

#if UNITY_EDITOR
        private PartUpgradeData[] _weaponParts; // WeaponData.Id → 무기 프리팹 룩업
        private EnemyData[] _enemyDatas;        // RigProfile → 적 투사체 룩업
#endif

        private float _nextHudRefresh;
        private string _hudText = string.Empty;
        private GUIStyle _hudStyle;

        // ── UI 패널 상태 ──
        private bool _panelVisible = true;
        private bool _fineStep;                 // 버튼 넛지 미세 모드 (핫키 Ctrl과 동일 스텝)
        private Vector2 _panelScroll;
        private readonly string[] _fields = new string[7];  // pos xyz / rot xyz / scale
        private int _bufferedAdjustIndex = -2;  // 필드 버퍼가 가리키는 선택 (-2 = 강제 갱신)
        private bool _textFieldFocused;         // 필드 타이핑 중엔 핫키 정지

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

        private int MountCount
        {
            get
            {
                RigProfileData p = CurrentProfile;
                return p != null && p.Mounts != null ? p.Mounts.Length : 0;
            }
        }

        private int MuzzleCount
        {
            get
            {
                RigProfileData p = CurrentProfile;
                return p != null && p.Muzzles != null ? p.Muzzles.Length : 0;
            }
        }

        private WeaponData EquippedWeapon =>
            _equipIndex >= 0 && _equipIndex < _equippables.Length ? _equippables[_equipIndex] : null;

        /// <summary>장착 무기의 Id — 전체 표시 모드면 null (표시 규칙이 "전부 켬"으로 바뀐다).</summary>
        private string EquippedWeaponId
        {
            get
            {
                WeaponData equipped = EquippedWeapon;
                return equipped != null ? equipped.Id : null;
            }
        }

        private void Awake()
        {
            // 비포커스 Play 루프 정지 함정 (Docs/06 §7) — 랩은 백그라운드에서도 돌게 한다.
            Application.runInBackground = true;

            for (int i = 0; i < _fields.Length; i++)
            {
                _fields[i] = string.Empty;
            }

#if UNITY_EDITOR
            // 시험 발사 룩업 — 배선 없이 항상 최신 에셋을 쓴다 (랩은 에디터 전용 씬).
            _weaponParts = LoadAll<PartUpgradeData>("t:PartUpgradeData");
            _enemyDatas = LoadAll<EnemyData>("t:EnemyData");
#endif
        }

#if UNITY_EDITOR
        private static T[] LoadAll<T>(string filter) where T : ScriptableObject
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(filter);
            var result = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                result[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return result;
        }
#endif

        private void Start() => SelectCurrent();

        private void OnDisable()
        {
            // 그래프를 안 지우면 Play 종료 시 누수 경고가 뜬다.
            StopPoseGraph();
            Application.runInBackground = false;
        }

        private void Update()
        {
            ReadHotkeys();

            if (Time.unscaledTime >= _nextHudRefresh)
            {
                _nextHudRefresh = Time.unscaledTime + _hudRefreshInterval;
                _dirty = ComputeDirty();
                RefreshHud();
                // 핫키·씬 뷰 핸들 조정도 주기적으로 필드에 반영 (타이핑 중엔 OnGUI 쪽이 보호).
                _bufferedAdjustIndex = -2;
            }
        }

        // ── 대상 선택 (Docs/06 §4.2-①) ──────────────────────────

        private void SelectCurrent()
        {
            // 대상이 바뀌면 이전 Animator가 파괴된다 — 물려 있던 포즈 그래프를 먼저 끊는다.
            StopPoseGraph();
            _poseIndex = -1;

            ReleaseTestWeapon();
            _adjustIndex = -1;
            SyncGizmo();

            _builder.SetProfile(CurrentProfile);
            _builder.Build();

            _animator = _builder.ModelRoot != null
                ? _builder.ModelRoot.GetComponentInChildren<Animator>()
                : null;

            if (_animator != null)
            {
                // 비포커스/오프스크린에서도 본 포즈가 갱신되게 (기본 컬링은 미렌더 시 포즈 정지).
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            // 대상이 바뀌면 무기 목록도 바뀐다 — 전체 표시(랩 기본)로 되돌린 뒤 다시 수집한다.
            _equipIndex = -1;
            CollectEquippables();
            ApplyEquipVisibility();
            RebuildVisibleAdjust();

            if (_orbitCamera != null)
            {
                _orbitCamera.SetTarget(_builder.ModelRoot != null
                    ? _builder.ModelRoot : _builder.transform);
            }

            _fireGroup = -1;
            _stateLabel = "FlyIdle";
            _dirty = false;
            _grounded = _animator == null;   // AC 없는 적은 바닥에 세워 둔다
            MeasureGroundOffset();
            ApplyPlaybackSpeed();
            ApplyLocomotion(grounded: false, move: Vector2.zero);
            ApplyRootHeight();               // ApplyLocomotion이 스킵되는 적도 높이는 적용
        }

        /// <summary>
        /// 모델 최저점(렌더러 바운즈)이 바닥(y=0)에 닿는 루트 높이를 잰다.
        /// 클립 루트가 골반 기준이라 모델이 루트 아래로 뻗는다 — y=0 스폰이면 다리가 파묻힌다.
        /// </summary>
        private void MeasureGroundOffset()
        {
            _groundOffset = 0f;
            if (_builder.ModelRoot == null)
            {
                return;
            }

            // 스폰 직후엔 본 포즈가 아직 안 써졌다 — 강제 갱신 (바인드 포즈 오측 방지).
            if (_animator != null)
            {
                _animator.Update(0f);
                _animator.Update(0f);
            }

            float minY = float.MaxValue;
            Renderer[] renderers = _builder.ModelRoot.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
            }

            if (minY < float.MaxValue)
            {
                _groundOffset = _builder.transform.position.y - minY;
            }
        }

        /// <summary>지상 = 최저점이 바닥, 비행 = 바닥에서 호버 높이만큼 위.</summary>
        private void ApplyRootHeight()
        {
            Vector3 position = _builder.transform.position;
            position.y = _groundOffset + (_grounded ? 0f : _flyHoverHeight);
            _builder.transform.position = position;
        }

        private void CycleTarget(int delta)
        {
            RigProfileData[] list = CurrentList;
            int count = list != null ? list.Length : 0;
            _selection[_tab] = RigLabMath.CycleIndex(_selection[_tab], delta, count);
            SelectCurrent();
        }

        // ── 장착 무기 (Docs/06 §4.2-①) ──────────────────────────
        //
        // 본편에선 WeaponMountVisuals가 보유 무기에 맞춰 장착 모델을 켜고 끈다. 랩엔 그게 없어
        // 종전에는 전 모델을 한꺼번에 켰고, 같은 손을 쓰는 마운트끼리 겹쳐 보였다. 여기서 무기를
        // 골라 본편과 같은 규칙으로 하나만 남긴다 — 랩에서 보는 것 = 본편에서 나오는 것.

        /// <summary>장착 후보 = 마운트가 ShowForWeapon으로 지목한 무기들 (등장 순서, 중복 제거).</summary>
        private void CollectEquippables()
        {
            _equipBuffer.Clear();
            RigProfileData profile = CurrentProfile;
            if (profile != null && profile.Mounts != null)
            {
                for (int i = 0; i < profile.Mounts.Length; i++)
                {
                    WeaponData weapon = profile.Mounts[i].ShowForWeapon;
                    if (weapon != null && !_equipBuffer.Contains(weapon))
                    {
                        _equipBuffer.Add(weapon);
                    }
                }
            }

            _equippables = _equipBuffer.Count > 0 ? _equipBuffer.ToArray() : NoWeapons;
        }

        /// <summary>
        /// 마운트 자식 중 장착 모델만 켜고 끈다. 총구 앵커도 마운트의 자식이라 접두사로
        /// 걸러내야 한다 — 같이 끄면 시험 발사·기즈모가 가리킬 지점이 사라진다.
        /// </summary>
        private void ApplyEquipVisibility()
        {
            RigProfileData profile = CurrentProfile;
            if (profile == null || profile.Mounts == null)
            {
                return;
            }

            string equippedId = EquippedWeaponId;
            for (int i = 0; i < profile.Mounts.Length; i++)
            {
                RigProfileData.MountDef def = profile.Mounts[i];
                if (!_builder.TryGetMount(def.Id, out Transform mount))
                {
                    continue;
                }

                bool show = RigLabMath.ShouldShowMount(MountWeaponId(def), equippedId)
                    && RigProfileMath.MatchesHand(def.Hand, _equipHand);

                for (int c = 0; c < mount.childCount; c++)
                {
                    Transform child = mount.GetChild(c);
                    if (child.name.StartsWith(
                        RigProfileMath.MuzzleNamePrefix, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    child.gameObject.SetActive(show);
                }
            }
        }

        /// <summary>W 순환: 전체 표시(-1) → 무기들 → 전체 표시.</summary>
        private void CycleEquip(int delta)
        {
            _equipIndex = RigLabMath.CycleWithNone(_equipIndex, delta, _equippables.Length);
            ApplyEquip();
        }

        private void SetEquip(int index)
        {
            _equipIndex = index;
            ApplyEquip();
        }

        /// <summary>E 순환: 양손 전부 → 오른손 → 왼손 (Docs/06 §3.4 좌우 마운트 확인용).</summary>
        private void CycleEquipHand()
        {
            _equipHand = (MountHand)RigLabMath.CycleIndex((int)_equipHand, +1, 3);
            ApplyEquip();
        }

        private void SetEquipHand(MountHand hand)
        {
            _equipHand = hand;
            ApplyEquip();
        }

        private void ApplyEquip()
        {
            ApplyEquipVisibility();
            RebuildVisibleAdjust();

            // 시험 발사 무기는 Id로 캐시된다 — 장착이 바뀌면 버려서 다음 F가 새 무기를 물게 한다.
            ReleaseTestWeapon();
            SyncFireGroupToEquip();
        }

        /// <summary>
        /// 조정 항목을 장착 무기 것만 남긴다 — 무기를 고르고 나면 다른 무기의 마운트·총구는
        /// 화면에 모델도 없어서 만져도 보이지 않는다. 전체 표시 모드(-1)에서는 전부 나온다.
        /// </summary>
        private void RebuildVisibleAdjust()
        {
            _visibleAdjust.Clear();
            RigProfileData profile = CurrentProfile;
            if (profile != null)
            {
                string equippedId = EquippedWeaponId;

                for (int i = 0; i < MountCount; i++)
                {
                    RigProfileData.MountDef def = profile.Mounts[i];
                    if (RigLabMath.ShouldShowMount(MountWeaponId(def), equippedId)
                        && RigProfileMath.MatchesHand(def.Hand, _equipHand))
                    {
                        _visibleAdjust.Add(i);
                    }
                }

                for (int i = 0; i < MuzzleCount; i++)
                {
                    RigProfileData.MuzzleDef def = profile.Muzzles[i];
                    if (RigLabMath.ShouldShowMuzzle(def.Id, OwnerMountWeaponId(profile, def.MountId),
                            equippedId)
                        && RigProfileMath.MatchesHand(
                            RigProfileMath.ResolveMuzzleHand(profile, def.MountId), _equipHand))
                    {
                        _visibleAdjust.Add(MountCount + i);
                    }
                }
            }

            // 걸러져 사라진 항목을 계속 조정하고 있으면 기즈모만 남고 목록엔 없다 — 선택을 푼다.
            if (_adjustIndex >= 0 && !_visibleAdjust.Contains(_adjustIndex))
            {
                _adjustIndex = -1;
                SyncGizmo();
            }
        }

        private static string MountWeaponId(RigProfileData.MountDef def) =>
            def != null && def.ShowForWeapon != null ? def.ShowForWeapon.Id : null;

        /// <summary>총구가 달린 마운트의 표시 조건 무기 Id (마운트가 없으면 null).</summary>
        private static string OwnerMountWeaponId(RigProfileData profile, string mountId)
        {
            if (string.IsNullOrEmpty(mountId) || profile.Mounts == null)
            {
                return null;
            }

            for (int i = 0; i < profile.Mounts.Length; i++)
            {
                if (profile.Mounts[i].Id == mountId)
                {
                    return MountWeaponId(profile.Mounts[i]);
                }
            }

            return null;
        }

        /// <summary>사격을 켜 둔 채 무기를 바꾸면 반동 그룹도 그 무기 것으로 따라간다.</summary>
        private void SyncFireGroupToEquip()
        {
            WeaponData equipped = EquippedWeapon;
            if (_animator == null || _fireGroup < 0 || equipped == null)
            {
                return;
            }

            _fireGroup = MechaAnimParams.GetFireGroup(equipped.Id);
            _animator.SetInteger(FireTypeHash, _fireGroup);
        }

        // ── 핫키 ────────────────────────────────────────────────

        private void ReadHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            // 수치 입력 필드 타이핑 중엔 핫키가 오발동하지 않게 전부 정지.
            if (_textFieldFocused)
            {
                return;
            }

            if (kb.hKey.wasPressedThisFrame)
            {
                _panelVisible = !_panelVisible;
            }

            if (kb.tabKey.wasPressedThisFrame)
            {
                _tab = 1 - _tab;
                SelectCurrent();
            }

            if (kb.wKey.wasPressedThisFrame)
            {
                CycleEquip(+1);
            }

            if (kb.eKey.wasPressedThisFrame)
            {
                CycleEquipHand();
            }

            if (kb.leftBracketKey.wasPressedThisFrame)
            {
                CycleAdjust(-1);
            }

            if (kb.rightBracketKey.wasPressedThisFrame)
            {
                CycleAdjust(+1);
            }

            if (_adjustIndex >= 0)
            {
                ReadAdjustKeys(kb);
            }
            else
            {
                if (kb.leftArrowKey.wasPressedThisFrame)
                {
                    CycleTarget(-1);
                }

                if (kb.rightArrowKey.wasPressedThisFrame)
                {
                    CycleTarget(+1);
                }
            }

            if (kb.fKey.wasPressedThisFrame)
            {
                TestFire();
            }

            if (kb.sKey.wasPressedThisFrame)
            {
                SaveProfile();
            }

            ReadAnimationKeys(kb);
        }

        // ── 마운트·총구 조정 (Docs/06 §4.2-②③) ──────────────────

        /// <summary>
        /// [ ] 순환: 없음(-1) → 마운트들 → 총구들 → 없음. 없음일 때 ←/→는 대상 순환.
        /// 순환 대상은 장착 무기로 걸러낸 목록 — 패널에 안 보이는 항목이 선택되면 안 된다.
        /// </summary>
        private void CycleAdjust(int delta)
        {
            int slot = _visibleAdjust.IndexOf(_adjustIndex);   // 미선택(-1)이면 그대로 -1
            slot = RigLabMath.CycleWithNone(slot, delta, _visibleAdjust.Count);
            _adjustIndex = slot >= 0 ? _visibleAdjust[slot] : -1;
            SyncGizmo();
        }

        private bool TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out string id)
        {
            anchor = null;
            isMuzzle = false;
            id = null;
            RigProfileData profile = CurrentProfile;
            if (profile == null || _adjustIndex < 0)
            {
                return false;
            }

            if (_adjustIndex < MountCount)
            {
                id = profile.Mounts[_adjustIndex].Id;
                return _builder.TryGetMount(id, out anchor);
            }

            int muzzleIndex = _adjustIndex - MountCount;
            if (muzzleIndex < MuzzleCount)
            {
                isMuzzle = true;
                RigProfileData.MuzzleDef def = profile.Muzzles[muzzleIndex];
                id = def.Id;
                // ★ 손을 넘겨야 한다 — 좌우 마운트가 같은 무기 Id의 총구를 하나씩 이고 있어서
                //   Id만으로 찾으면 둘 다 오른손 앵커를 가리킨다 (조정도 저장도 엉뚱해진다).
                return _builder.TryGetMuzzle(
                    id, RigProfileMath.ResolveMuzzleHand(profile, def.MountId), out anchor);
            }

            return false;
        }

        private void SyncGizmo()
        {
            if (_gizmo == null)
            {
                return;
            }

            if (TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out _))
            {
                _gizmo.Target = anchor;
                _gizmo.IsMuzzle = isMuzzle;
#if UNITY_EDITOR
                // 씬 뷰 핸들 병용 (Docs/06 §4.2-②) — 선택하면 씬 뷰에서 바로 끌 수 있게.
                UnityEditor.Selection.activeGameObject = anchor.gameObject;
#endif
            }
            else
            {
                _gizmo.Target = null;
            }
        }

        private void ReadAdjustKeys(Keyboard kb)
        {
            if (!TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out _))
            {
                return;
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                ResetSelected(anchor, isMuzzle);
                return;
            }

            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;

            int x = (kb.rightArrowKey.wasPressedThisFrame ? 1 : 0)
                - (kb.leftArrowKey.wasPressedThisFrame ? 1 : 0);
            int z = (kb.upArrowKey.wasPressedThisFrame ? 1 : 0)
                - (kb.downArrowKey.wasPressedThisFrame ? 1 : 0);
            int y = (kb.pageUpKey.wasPressedThisFrame ? 1 : 0)
                - (kb.pageDownKey.wasPressedThisFrame ? 1 : 0);

            if (x == 0 && y == 0 && z == 0)
            {
                return;
            }

            if (alt && !isMuzzle)
            {
                // Alt+PgUp/Dn: 균일 스케일 (마운트 전용 — 총구는 위치·방향만 의미 있다).
                float step = ctrl ? _scaleStepFine : _scaleStep;
                anchor.localScale += Vector3.one * (y * step);
            }
            else if (shift)
            {
                // Shift: 회전 — ←/→ 요, ↑/↓ 피치, PgUp/Dn 롤.
                float step = ctrl ? _rotateStepFine : _rotateStep;
                anchor.localRotation *= Quaternion.Euler(z * step, x * step, y * step);
            }
            else
            {
                // 이동 — 본 로컬 기준 (기즈모 축과 일치).
                float step = ctrl ? _moveStepFine : _moveStep;
                anchor.localPosition += new Vector3(x * step, y * step, z * step);
            }
        }

        private void ResetSelected(Transform anchor, bool isMuzzle)
        {
            RigProfileData profile = CurrentProfile;
            if (isMuzzle)
            {
                RigProfileData.MuzzleDef def = profile.Muzzles[_adjustIndex - MountCount];
                RigProfileMath.ApplyLocal(anchor, def.LocalPosition, def.LocalEulerAngles, Vector3.one);
            }
            else
            {
                RigProfileData.MountDef def = profile.Mounts[_adjustIndex];
                RigProfileMath.ApplyLocal(anchor, def.LocalPosition, def.LocalEulerAngles, def.LocalScale);
            }
        }

        // ── 시험 발사 (Docs/06 §4.2-③) ──────────────────────────

        private void TestFire()
        {
#if UNITY_EDITOR
            RigProfileData profile = CurrentProfile;
            if (profile == null || profile.Muzzles == null || profile.Muzzles.Length == 0)
            {
                return;
            }

            // 총구 우선순위: 조정 중인 총구 > 장착 무기의 총구 > 첫 총구.
            // ★ 앵커는 (Id, 손)으로 찾는다 — 좌우 마운트가 같은 무기 Id를 공유한다 (Docs/06 §3.4).
            string muzzleId = profile.Muzzles[0].Id;
            MountHand muzzleHand = RigProfileMath.ResolveMuzzleHand(profile, profile.Muzzles[0].MountId);

            string equippedId = EquippedWeaponId;
            if (equippedId != null && _builder.TryGetMuzzle(equippedId, _equipHand, out _))
            {
                muzzleId = equippedId;
                muzzleHand = _equipHand;
            }

            Transform anchor;
            if (TryGetAdjustTarget(out Transform selected, out bool isMuzzle, out string selectedId)
                && isMuzzle)
            {
                // 조정 중인 총구가 있으면 방금 맞춘 그 앵커에서 쏜다.
                muzzleId = selectedId;
                anchor = selected;
            }
            else if (!_builder.TryGetMuzzle(muzzleId, muzzleHand, out anchor))
            {
                return;
            }

            if (_tab == 0)
            {
                FirePlayerWeapon(muzzleId, anchor);
            }
            else
            {
                FireEnemyProjectile(anchor);
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>본편과 같은 무기 프리팹 발사 경로 — aimer 없음 → Muzzle.forward로 나간다.</summary>
        private void FirePlayerWeapon(string weaponId, Transform muzzle)
        {
            if (_testWeaponId != weaponId)
            {
                ReleaseTestWeapon();

                for (int i = 0; i < _weaponParts.Length; i++)
                {
                    PartUpgradeData part = _weaponParts[i];
                    if (part == null || part.Weapon == null || part.WeaponPrefab == null
                        || part.Weapon.Id != weaponId)
                    {
                        continue;
                    }

                    _testWeapon = (Weapon)PoolManager.Instance.Spawn(
                        part.WeaponPrefab, _builder.transform.position, Quaternion.identity);
                    _testWeapon.transform.SetParent(_builder.transform, worldPositionStays: false);
                    _testWeapon.SetData(part.Weapon);
                    _testWeapon.SetLevel(1);
                    _testWeaponId = weaponId;
                    break;
                }
            }

            if (_testWeapon != null)
            {
                _testWeapon.SetMuzzle(muzzle);
                _testWeapon.TryFire(null, null);
            }
        }

        private void FireEnemyProjectile(Transform muzzle)
        {
            RigProfileData profile = CurrentProfile;
            for (int i = 0; i < _enemyDatas.Length; i++)
            {
                EnemyData data = _enemyDatas[i];
                if (data == null || data.RigProfile != profile || data.ProjectilePrefab == null)
                {
                    continue;
                }

                var projectile = (Projectile)PoolManager.Instance.Spawn(
                    data.ProjectilePrefab, muzzle.position, Quaternion.LookRotation(muzzle.forward));
                projectile.Launch(new ProjectileLaunchData(
                    muzzle.forward, data.ProjectileSpeed, data.ProjectileDamage,
                    sourceId: null, range: data.AttackRange * 2f));
                return;
            }
        }
#endif

        private void ReleaseTestWeapon()
        {
            if (_testWeapon != null)
            {
                PoolManager.Instance.Despawn(_testWeapon);
                _testWeapon = null;
            }

            _testWeaponId = null;
        }

        // ── 저장 (Docs/06 §4.2-⑤) ───────────────────────────────

        /// <summary>SO는 에셋이라 Play 중 저장해도 유지된다 — 이 워크플로의 근거.</summary>
        private void SaveProfile()
        {
#if UNITY_EDITOR
            RigProfileData profile = CurrentProfile;
            if (profile == null)
            {
                return;
            }

            if (profile.Mounts != null)
            {
                for (int i = 0; i < profile.Mounts.Length; i++)
                {
                    RigProfileData.MountDef def = profile.Mounts[i];
                    if (_builder.TryGetMount(def.Id, out Transform anchor))
                    {
                        def.LocalPosition = anchor.localPosition;
                        def.LocalEulerAngles = anchor.localEulerAngles;
                        def.LocalScale = anchor.localScale;
                    }
                }
            }

            if (profile.Muzzles != null)
            {
                for (int i = 0; i < profile.Muzzles.Length; i++)
                {
                    RigProfileData.MuzzleDef def = profile.Muzzles[i];
                    if (_builder.TryGetMuzzle(def.Id,
                        RigProfileMath.ResolveMuzzleHand(profile, def.MountId), out Transform anchor))
                    {
                        def.LocalPosition = anchor.localPosition;
                        def.LocalEulerAngles = anchor.localEulerAngles;
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(profile);
            UnityEditor.AssetDatabase.SaveAssets();
            _dirty = false;
            Debug.Log($"[RigLab] 저장 완료: {profile.name}");
#endif
        }

        /// <summary>앵커 로컬 값과 프로필 값의 차이 — 미저장 변경 표시용 (HUD 주기로만 검사).</summary>
        private bool ComputeDirty()
        {
            RigProfileData profile = CurrentProfile;
            if (profile == null)
            {
                return false;
            }

            const float posEpsilon = 1e-5f;
            const float angleEpsilon = 0.01f;

            if (profile.Mounts != null)
            {
                for (int i = 0; i < profile.Mounts.Length; i++)
                {
                    RigProfileData.MountDef def = profile.Mounts[i];
                    if (!_builder.TryGetMount(def.Id, out Transform anchor))
                    {
                        continue;
                    }

                    if ((anchor.localPosition - def.LocalPosition).sqrMagnitude > posEpsilon
                        || Quaternion.Angle(anchor.localRotation,
                            Quaternion.Euler(def.LocalEulerAngles)) > angleEpsilon
                        || (anchor.localScale - def.LocalScale).sqrMagnitude > posEpsilon)
                    {
                        return true;
                    }
                }
            }

            if (profile.Muzzles != null)
            {
                for (int i = 0; i < profile.Muzzles.Length; i++)
                {
                    RigProfileData.MuzzleDef def = profile.Muzzles[i];
                    if (!_builder.TryGetMuzzle(def.Id,
                        RigProfileMath.ResolveMuzzleHand(profile, def.MountId), out Transform anchor))
                    {
                        continue;
                    }

                    if ((anchor.localPosition - def.LocalPosition).sqrMagnitude > posEpsilon
                        || Quaternion.Angle(anchor.localRotation,
                            Quaternion.Euler(def.LocalEulerAngles)) > angleEpsilon)
                    {
                        return true;
                    }
                }
            }

            return false;
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
                ActFlyIdle();
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                ActFly();
            }

            if (kb.digit3Key.wasPressedThisFrame)
            {
                ActGroundIdle();
            }

            if (kb.digit4Key.wasPressedThisFrame)
            {
                ActWalk();
            }

            if (kb.digit5Key.wasPressedThisFrame)
            {
                ActDash();
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
                ActHit(heavy: false);
            }

            if (kb.digit0Key.wasPressedThisFrame)
            {
                ActHit(heavy: true);
            }

            if (kb.pKey.wasPressedThisFrame)
            {
                ActCyclePose();
            }

            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
            {
                ActSpeed(0.1f);
            }

            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                ActSpeed(-0.1f);
            }
        }

        // ── 포즈 클립 재생 (Docs/07 §9-5) ────────────────────────
        //
        // 전시용 포즈(Mecha_Pose_HeroLunge 등)는 AC_Mecha에 상태를 만들지 않는다 —
        // 게임용 컨트롤러를 확인용 클립으로 오염시키지 않으려고, 랩에서만 PlayableGraph를
        // Animator에 직접 물려 재생한다. 그래프를 파괴하면 컨트롤러 그래프로 되돌아온다.

        private bool PoseActive => _poseGraph.IsValid();

        private int PoseCount => _poseClips != null ? _poseClips.Length : 0;

        private AnimationClip CurrentPoseClip =>
            _poseIndex >= 0 && _poseIndex < PoseCount ? _poseClips[_poseIndex] : null;

        /// <summary>P: 없음 → 포즈들 → 없음 순환.</summary>
        private void ActCyclePose()
        {
            if (_animator == null)
            {
                return;
            }

            SetPoseIndex(RigLabMath.CycleWithNone(_poseIndex, +1, PoseCount));
        }

        /// <summary>UI 버튼용 직접 지정 — 켜져 있는 항목을 다시 누르면 해제.</summary>
        private void TogglePose(int index)
        {
            SetPoseIndex(_poseIndex == index ? -1 : index);
        }

        private void SetPoseIndex(int index)
        {
            StopPoseGraph();
            _poseIndex = index;

            AnimationClip clip = CurrentPoseClip;
            if (clip == null || _animator == null)
            {
                _poseIndex = -1;
                ActFlyIdle();               // 포즈 해제 → 기본 상태로 복귀
                return;
            }

            _poseGraph = PlayableGraph.Create("RigLabPose");
            _poseGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_poseGraph, "Pose", _animator);
            _posePlayable = AnimationClipPlayable.Create(_poseGraph, clip);
            _posePlayable.SetApplyFootIK(false);
            output.SetSourcePlayable(_posePlayable);
            _poseGraph.Play();

            _stateLabel = "포즈 " + clip.name;
            _fireGroup = -1;
            ApplyPlaybackSpeed();

            // 공중 포즈는 루트 아래로 크게 뻗는다 — 바닥에 파묻히지 않게 높이를 다시 잰다.
            _grounded = false;
            MeasureGroundOffset();
            ApplyRootHeight();
        }

        private void StopPoseGraph()
        {
            if (_poseGraph.IsValid())
            {
                _poseGraph.Destroy();
            }
        }

        /// <summary>포즈 재생 중 다른 상태 키를 누르면 먼저 컨트롤러로 되돌린다.</summary>
        private void ExitPose()
        {
            if (!PoseActive)
            {
                return;
            }

            StopPoseGraph();
            _poseIndex = -1;
            ApplyPlaybackSpeed();
            MeasureGroundOffset();
            ApplyRootHeight();
        }

        // 핫키·UI 버튼 공용 동작 — 같은 코드 경로를 태운다.

        private void ActFlyIdle()
        {
            ExitPose();
            _stateLabel = "FlyIdle";
            ApplyLocomotion(grounded: false, move: Vector2.zero);
        }

        private void ActFly()
        {
            ExitPose();
            AdvanceDirectionIf("Fly");
            _stateLabel = "Fly";
            ApplyLocomotion(grounded: false, move: RigLabMath.DirectionForIndex(_directionIndex));
        }

        private void ActGroundIdle()
        {
            ExitPose();
            _stateLabel = "GroundIdle";
            ApplyLocomotion(grounded: true, move: Vector2.zero);
        }

        private void ActWalk()
        {
            ExitPose();
            AdvanceDirectionIf("Walk");
            _stateLabel = "Walk";
            ApplyLocomotion(grounded: true, move: RigLabMath.DirectionForIndex(_directionIndex));
        }

        private void ActDash()
        {
            ExitPose();
            AdvanceDirectionIf("Dash");
            Vector2 dir = RigLabMath.DirectionForIndex(_directionIndex);
            _stateLabel = "Dash";
            _animator.SetFloat(DashXHash, dir.x);
            _animator.SetFloat(DashZHash, dir.y);
            _animator.SetTrigger(DashTriggerHash);
        }

        private void ActHit(bool heavy)
        {
            ExitPose();
            _stateLabel = heavy ? "HitHeavy" : "Hit";
            _animator.SetTrigger(heavy ? HitHeavyTriggerHash : HitTriggerHash);
        }

        private void ActSpeed(float delta)
        {
            _playbackSpeed = RigLabMath.StepPlaybackSpeed(_playbackSpeed, delta);
            ApplyPlaybackSpeed();
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

            _grounded = grounded;
            ApplyRootHeight();
            _animator.SetBool(IsGroundedHash, grounded);
            _animator.SetFloat(MoveXHash, move.x);
            _animator.SetFloat(MoveZHash, move.y);
            _animator.SetFloat(SpeedHash, 1f);
            _animator.SetFloat(VerticalYHash, 0f);
        }

        private void ToggleFire(int group)
        {
            // 사격은 상체 레이어라 포즈(전신 그래프)와 같이 못 쓴다 — 포즈에서 먼저 빠져나온다.
            if (PoseActive)
            {
                ActFlyIdle();
            }

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

            // Animator.speed는 PlayableGraph 출력에 걸리지 않는다 — 플레이어블에 직접 준다.
            if (_poseGraph.IsValid())
            {
                _posePlayable.SetSpeed(_playbackSpeed);
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

            string adjust;
            if (TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out string id))
            {
                string kind = isMuzzle ? "총구" : "마운트";
                adjust = $"{kind} {id}  pos{anchor.localPosition:F3}  rot{anchor.localEulerAngles:F1}"
                    + (isMuzzle ? "" : $"  scale{anchor.localScale:F2}");
            }
            else
            {
                adjust = $"없음 ([ ] 로 선택 — 후보 {_visibleAdjust.Count}개)";
            }

            string dirtyMark = _dirty ? "  ● 미저장 변경 — S 저장!" : "";

            WeaponData equipped = EquippedWeapon;
            string equip = _equippables.Length == 0
                ? "(무기 마운트 없음)"
                : equipped != null
                    ? $"{equipped.DisplayName} ({equipped.Id})  {_equipIndex + 1}/{_equippables.Length}"
                      + $"  손 {HandNames[(int)_equipHand]}  — 조정 항목도 이 무기 것만 표시"
                    : $"전체 표시 (무기 {_equippables.Length}종)  손 {HandNames[(int)_equipHand]}";

            _hudText =
                $"[리그 실험실]  탭 {TabNames[_tab]}  대상 {target}{dirtyMark}\n" +
                $"장착  {equip}\n" +
                $"조정  {adjust}\n" +
                $"애니메이션  {anim}\n" +
                "Tab 탭   ←/→ 대상(선택 없을 때)   W 장착 무기   E 장착 손   [ ] 마운트/총구 선택   "
                + "R 리셋   F 시험 발사   S 저장\n" +
                "선택 중: 화살표 X/Z · PgUp/Dn Y · Shift=회전 · Ctrl=미세 · Alt+PgUp/Dn=스케일\n" +
                "1 FlyIdle  2 Fly(8방)  3 GroundIdle  4 Walk(8방)  5 Dash(8방)  6/7/8 사격  9/0 피격  "
                + $"P 포즈({PoseCount})  =/- 속도  H 패널  우클릭/휠 카메라";
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _hudStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
            }

            GUI.Label(new Rect(12f, 12f, 1100f, 180f), _hudText, _hudStyle);

            if (_panelVisible)
            {
                DrawPanel();
            }

            // 필드 포커스 감지는 OnGUI 안에서만 유효 — Update의 핫키 가드가 읽는다.
            _textFieldFocused = GUIUtility.keyboardControl != 0;
        }

        // ── UI 패널 (Docs/06 — 핫키와 동일 기능의 버튼) ──────────

        private void DrawPanel()
        {
            var area = new Rect(Screen.width - _panelWidth - 12f, 12f, _panelWidth, Screen.height - 24f);
            GUILayout.BeginArea(area, GUI.skin.box);
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);

            DrawTargetSection();
            GUILayout.Space(8f);
            DrawEquipSection();
            GUILayout.Space(8f);
            DrawAdjustSection();
            GUILayout.Space(8f);
            DrawActionSection();
            GUILayout.Space(8f);
            DrawAnimationSection();

            GUILayout.Space(6f);
            GUILayout.Label("H: 패널 숨김/표시  ·  필드 입력 중엔 핫키 정지");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>선택 상태 버튼 — 켜져 있으면 초록 틴트.</summary>
        private bool ToggleButton(string label, bool active, params GUILayoutOption[] options)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.5f, 1f, 0.5f) : old;
            bool clicked = GUILayout.Button(label, options);
            GUI.backgroundColor = old;
            if (clicked)
            {
                GUIUtility.keyboardControl = 0;  // 버튼 조작 시 필드 포커스 해제 → 핫키 복귀
            }

            return clicked;
        }

        private bool ActionButton(string label, params GUILayoutOption[] options)
        {
            bool clicked = GUILayout.Button(label, options);
            if (clicked)
            {
                GUIUtility.keyboardControl = 0;
            }

            return clicked;
        }

        private void DrawTargetSection()
        {
            GUILayout.Label("── 대상 ──");
            GUILayout.BeginHorizontal();
            for (int t = 0; t < 2; t++)
            {
                if (ToggleButton(TabNames[t], _tab == t) && _tab != t)
                {
                    _tab = t;
                    SelectCurrent();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ActionButton("◀", GUILayout.Width(32f)))
            {
                CycleTarget(-1);
            }

            RigProfileData profile = CurrentProfile;
            GUILayout.Label(profile != null ? profile.name : "(없음)",
                GUILayout.ExpandWidth(true));
            if (ActionButton("▶", GUILayout.Width(32f)))
            {
                CycleTarget(+1);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawEquipSection()
        {
            GUILayout.Label("── 장착 무기 (W 순환) ──");
            if (_equippables.Length == 0)
            {
                GUILayout.Label("(이 대상엔 무기 마운트가 없다)");
                return;
            }

            GUILayout.Label("장착 손 (E 순환)");
            GUILayout.BeginHorizontal();
            for (int h = 0; h < 3; h++)
            {
                var hand = (MountHand)h;
                if (ToggleButton(HandNames[h], _equipHand == hand) && _equipHand != hand)
                {
                    SetEquipHand(hand);
                }
            }

            GUILayout.EndHorizontal();

            if (ToggleButton("전체 표시 (겹쳐 보임)", _equipIndex < 0) && _equipIndex >= 0)
            {
                SetEquip(-1);
            }

            for (int i = 0; i < _equippables.Length; i++)
            {
                WeaponData weapon = _equippables[i];
                if (weapon == null)
                {
                    continue;
                }

                if (ToggleButton($"{weapon.DisplayName}  ({weapon.Id})", _equipIndex == i)
                    && _equipIndex != i)
                {
                    SetEquip(i);
                }
            }
        }

        private void DrawAdjustSection()
        {
            WeaponData equipped = EquippedWeapon;
            int total = MountCount + MuzzleCount;
            bool filtered = _visibleAdjust.Count < total;
            GUILayout.Label(filtered
                ? $"── 조정 항목 · {(equipped != null ? equipped.DisplayName : "전체")}"
                  + $" / {HandNames[(int)_equipHand]} ({_visibleAdjust.Count}/{total}) ──"
                : "── 조정 항목 (전체) ──");

            if (ToggleButton("선택 없음 (←/→ = 대상 순환)", _adjustIndex < 0))
            {
                _adjustIndex = -1;
                SyncGizmo();
            }

            RigProfileData profile = CurrentProfile;
            if (profile == null)
            {
                return;
            }

            if (_visibleAdjust.Count == 0)
            {
                GUILayout.Label(total > 0
                    ? "(이 무기·손 조합에 딸린 마운트·총구가 없다)"
                    : "(이 대상엔 마운트·총구가 없다)");
                return;
            }

            for (int i = 0; i < _visibleAdjust.Count; i++)
            {
                int index = _visibleAdjust[i];
                string label;
                if (index < MountCount)
                {
                    label = "마운트  " + profile.Mounts[index].Id;
                }
                else
                {
                    RigProfileData.MuzzleDef muzzle = profile.Muzzles[index - MountCount];
                    // 같은 Id의 좌우 총구가 나란히 뜨므로 어느 마운트 것인지 붙여 준다.
                    label = "총구  " + muzzle.Id
                        + (string.IsNullOrEmpty(muzzle.MountId) ? "" : "  @" + muzzle.MountId);
                }
                if (ToggleButton(label, _adjustIndex == index))
                {
                    _adjustIndex = index;
                    SyncGizmo();
                }
            }

            if (filtered)
            {
                GUILayout.Label("↑ 장착 무기·손에 걸리는 항목만 — 전부 보려면 '전체 표시' + '양손 전부'");
            }

            if (!TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out _))
            {
                return;
            }

            RefreshFieldsIfStale(anchor, force: false);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (ToggleButton(_fineStep ? "미세 스텝 ON" : "미세 스텝 OFF", _fineStep))
            {
                _fineStep = !_fineStep;
            }

            if (ActionButton("R 리셋"))
            {
                ResetSelected(anchor, isMuzzle);
                RefreshFieldsIfStale(anchor, force: true);
            }

            GUILayout.EndHorizontal();

            float moveStep = _fineStep ? _moveStepFine : _moveStep;
            float rotStep = _fineStep ? _rotateStepFine : _rotateStep;

            GUILayout.Label($"위치 (스텝 {moveStep:0.###})");
            DrawFieldRow(0);
            DrawNudgeRow(isMove: true, (axis, sign) =>
            {
                Vector3 delta = Vector3.zero;
                delta[axis] = sign * moveStep;
                anchor.localPosition += delta;
                RefreshFieldsIfStale(anchor, force: true);
            });

            GUILayout.Label($"회전 ° (스텝 {rotStep:0.#})");
            DrawFieldRow(3);
            DrawNudgeRow(isMove: false, (axis, sign) =>
            {
                Vector3 euler = Vector3.zero;
                euler[axis] = sign * rotStep;
                anchor.localRotation *= Quaternion.Euler(euler);
                RefreshFieldsIfStale(anchor, force: true);
            });

            if (!isMuzzle)
            {
                float scaleStep = _fineStep ? _scaleStepFine : _scaleStep;
                GUILayout.Label($"스케일 (균일, 스텝 {scaleStep:0.###})");
                GUILayout.BeginHorizontal();
                _fields[6] = GUILayout.TextField(_fields[6], GUILayout.ExpandWidth(true));
                if (ActionButton("−", GUILayout.Width(36f)))
                {
                    anchor.localScale -= Vector3.one * scaleStep;
                    RefreshFieldsIfStale(anchor, force: true);
                }

                if (ActionButton("+", GUILayout.Width(36f)))
                {
                    anchor.localScale += Vector3.one * scaleStep;
                    RefreshFieldsIfStale(anchor, force: true);
                }

                GUILayout.EndHorizontal();
            }

            if (ActionButton("입력 값 적용"))
            {
                ApplyFields(anchor, isMuzzle);
            }
        }

        /// <summary>수치 입력 3칸 (pos 또는 rot).</summary>
        private void DrawFieldRow(int offset)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                _fields[offset + i] = GUILayout.TextField(_fields[offset + i], GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
        }

        private static readonly string[] MoveNudgeLabels = { "X−", "X+", "Y−", "Y+", "Z−", "Z+" };
        private static readonly string[] RotateNudgeLabels = { "P−", "P+", "Y−", "Y+", "R−", "R+" };

        private void DrawNudgeRow(bool isMove, System.Action<int, float> nudge)
        {
            string[] labels = isMove ? MoveNudgeLabels : RotateNudgeLabels;
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                if (ActionButton(labels[i * 2], GUILayout.ExpandWidth(true)))
                {
                    nudge(i, -1f);
                }

                if (ActionButton(labels[i * 2 + 1], GUILayout.ExpandWidth(true)))
                {
                    nudge(i, +1f);
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>선택 변경·버튼 조정 후 필드 버퍼 동기화. 타이핑 중(포커스)엔 덮지 않는다.</summary>
        private void RefreshFieldsIfStale(Transform anchor, bool force)
        {
            if (!force && _bufferedAdjustIndex == _adjustIndex)
            {
                return;
            }

            if (!force && GUIUtility.keyboardControl != 0)
            {
                return;
            }

            _bufferedAdjustIndex = _adjustIndex;
            Vector3 pos = anchor.localPosition;
            Vector3 rot = anchor.localEulerAngles;
            _fields[0] = pos.x.ToString("0.####");
            _fields[1] = pos.y.ToString("0.####");
            _fields[2] = pos.z.ToString("0.####");
            _fields[3] = rot.x.ToString("0.##");
            _fields[4] = rot.y.ToString("0.##");
            _fields[5] = rot.z.ToString("0.##");
            _fields[6] = anchor.localScale.x.ToString("0.####");
        }

        private void ApplyFields(Transform anchor, bool isMuzzle)
        {
            GUIUtility.keyboardControl = 0;
            bool ok = float.TryParse(_fields[0], out float px)
                & float.TryParse(_fields[1], out float py)
                & float.TryParse(_fields[2], out float pz)
                & float.TryParse(_fields[3], out float rx)
                & float.TryParse(_fields[4], out float ry)
                & float.TryParse(_fields[5], out float rz);
            if (!ok)
            {
                Debug.LogWarning("[RigLab] 입력 값 해석 실패 — 숫자만 입력하세요.");
                return;
            }

            anchor.localPosition = new Vector3(px, py, pz);
            anchor.localRotation = Quaternion.Euler(rx, ry, rz);
            if (!isMuzzle && float.TryParse(_fields[6], out float scale))
            {
                anchor.localScale = Vector3.one * scale;
            }

            RefreshFieldsIfStale(anchor, force: true);
        }

        private void DrawActionSection()
        {
            GUILayout.Label("── 동작 ──");
            GUILayout.BeginHorizontal();
            if (ActionButton("F 시험 발사"))
            {
                TestFire();
            }

            if (ActionButton(_dirty ? "S 저장 ●" : "S 저장"))
            {
                SaveProfile();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawAnimationSection()
        {
            GUILayout.Label("── 애니메이션 ──");
            if (_animator == null)
            {
                GUILayout.Label("없음 (AC 없는 대상 — 스킵)");
                return;
            }

            GUILayout.BeginHorizontal();
            if (ToggleButton("FlyIdle", _stateLabel == "FlyIdle"))
            {
                ActFlyIdle();
            }

            if (ToggleButton("Fly ▸8방", _stateLabel == "Fly"))
            {
                ActFly();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ToggleButton("GroundIdle", _stateLabel == "GroundIdle"))
            {
                ActGroundIdle();
            }

            if (ToggleButton("Walk ▸8방", _stateLabel == "Walk"))
            {
                ActWalk();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ToggleButton("Dash ▸8방", _stateLabel == "Dash"))
            {
                ActDash();
            }

            if (ActionButton("피격"))
            {
                ActHit(heavy: false);
            }

            if (ActionButton("강피격"))
            {
                ActHit(heavy: true);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("사격 유지 토글");
            GUILayout.BeginHorizontal();
            if (ToggleButton("경화기", _fireGroup == MechaAnimParams.FireGroupLight))
            {
                ToggleFire(MechaAnimParams.FireGroupLight);
            }

            if (ToggleButton("발사기", _fireGroup == MechaAnimParams.FireGroupLauncher))
            {
                ToggleFire(MechaAnimParams.FireGroupLauncher);
            }

            if (ToggleButton("중화기", _fireGroup == MechaAnimParams.FireGroupHeavy))
            {
                ToggleFire(MechaAnimParams.FireGroupHeavy);
            }

            GUILayout.EndHorizontal();

            // 명시적 중지 — 켜진 그룹 버튼 재클릭과 같은 동작이지만 눈에 띄게 따로 둔다.
            GUI.enabled = _fireGroup >= 0;
            if (ActionButton("■ 사격 중지"))
            {
                ToggleFire(_fireGroup);
            }

            GUI.enabled = true;

            if (PoseCount > 0)
            {
                GUILayout.Label("포즈 클립 (P 순환 · AC 밖의 전시용)");
                for (int i = 0; i < PoseCount; i++)
                {
                    AnimationClip poseClip = _poseClips[i];
                    if (poseClip == null)
                    {
                        continue;
                    }

                    if (ToggleButton(poseClip.name, _poseIndex == i))
                    {
                        TogglePose(i);
                    }
                }
            }

            GUILayout.BeginHorizontal();
            if (ActionButton("속도 −", GUILayout.ExpandWidth(true)))
            {
                ActSpeed(-0.1f);
            }

            GUILayout.Label($"x{_playbackSpeed:0.0}", GUILayout.Width(44f));
            if (ActionButton("속도 +", GUILayout.ExpandWidth(true)))
            {
                ActSpeed(0.1f);
            }

            GUILayout.EndHorizontal();
        }
    }
}
