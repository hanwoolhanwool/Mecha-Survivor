using UnityEngine;
using UnityEngine.InputSystem;
using MechaSurvivor.Systems;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 리그 실험실 (RigLab 씬 전용, Docs/06). 카탈로그의 리그 프로필을 RigBuilder로 재구성해
    /// 마운트·총구·애니메이션을 눈으로 확인하고, 조정 결과를 프로필(SO)에 저장한다 —
    /// 랩에서 보는 것 = 본편에서 나오는 것.
    ///
    /// 조작: Tab 캐릭터↔적 탭 · ←/→ 대상 순환 · [ ] 마운트/총구 선택 ·
    /// (선택 중) 화살표+PgUp/Dn 이동, Shift=회전, Ctrl=미세, Alt+PgUp/Dn=스케일 ·
    /// R 리셋 · F 시험 발사 · S 저장 · 1~5 이동 상태(8방) · 6/7/8 사격 토글 ·
    /// 9/0 피격 · =/- 재생 속도 · 우클릭 드래그/휠 카메라.
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

        [Header("넛지 스텝")]
        [SerializeField] private float _moveStep = 0.05f;
        [SerializeField] private float _moveStepFine = 0.005f;
        [SerializeField] private float _rotateStep = 5f;
        [SerializeField] private float _rotateStepFine = 0.5f;
        [SerializeField] private float _scaleStep = 0.05f;
        [SerializeField] private float _scaleStepFine = 0.005f;

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

        private int _adjustIndex = -1;          // -1=조정 안 함, 0..M-1=마운트, M..=총구
        private bool _dirty;

        private Weapon _testWeapon;             // 시험 발사용 실제 무기 인스턴스
        private string _testWeaponId;

#if UNITY_EDITOR
        private PartUpgradeData[] _weaponParts; // WeaponData.Id → 무기 프리팹 룩업
        private EnemyData[] _enemyDatas;        // RigProfile → 적 투사체 룩업
#endif

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

        private void Awake()
        {
            // 비포커스 Play 루프 정지 함정 (Docs/06 §7) — 랩은 백그라운드에서도 돌게 한다.
            Application.runInBackground = true;

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
            }
        }

        // ── 대상 선택 (Docs/06 §4.2-①) ──────────────────────────

        private void SelectCurrent()
        {
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
            _dirty = false;
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

        /// <summary>[ ] 순환: 없음(-1) → 마운트들 → 총구들 → 없음. 없음일 때 ←/→는 대상 순환.</summary>
        private void CycleAdjust(int delta)
        {
            int count = MountCount + MuzzleCount;
            if (count == 0)
            {
                _adjustIndex = -1;
                SyncGizmo();
                return;
            }

            // -1 포함 순환 (count+1 칸을 돌리고 -1 오프셋).
            _adjustIndex = RigLabMath.CycleIndex(_adjustIndex + 1, delta, count + 1) - 1;
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
                id = profile.Muzzles[muzzleIndex].Id;
                return _builder.TryGetMuzzle(id, out anchor);
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

            // 선택 항목이 총구면 그 총구, 아니면 첫 총구.
            string muzzleId = profile.Muzzles[0].Id;
            if (TryGetAdjustTarget(out _, out bool isMuzzle, out string selectedId) && isMuzzle)
            {
                muzzleId = selectedId;
            }

            if (!_builder.TryGetMuzzle(muzzleId, out Transform anchor))
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
                    if (_builder.TryGetMuzzle(def.Id, out Transform anchor))
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
                    if (!_builder.TryGetMuzzle(def.Id, out Transform anchor))
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

            string adjust;
            if (TryGetAdjustTarget(out Transform anchor, out bool isMuzzle, out string id))
            {
                string kind = isMuzzle ? "총구" : "마운트";
                adjust = $"{kind} {id}  pos{anchor.localPosition:F3}  rot{anchor.localEulerAngles:F1}"
                    + (isMuzzle ? "" : $"  scale{anchor.localScale:F2}");
            }
            else
            {
                adjust = "없음 ([ ] 로 선택)";
            }

            string dirtyMark = _dirty ? "  ● 미저장 변경 — S 저장!" : "";

            _hudText =
                $"[리그 실험실]  탭 {TabNames[_tab]}  대상 {target}{dirtyMark}\n" +
                $"조정  {adjust}\n" +
                $"애니메이션  {anim}\n" +
                "Tab 탭   ←/→ 대상(선택 없을 때)   [ ] 마운트/총구 선택   R 리셋   F 시험 발사   S 저장\n" +
                "선택 중: 화살표 X/Z · PgUp/Dn Y · Shift=회전 · Ctrl=미세 · Alt+PgUp/Dn=스케일\n" +
                "1 FlyIdle  2 Fly(8방)  3 GroundIdle  4 Walk(8방)  5 Dash(8방)  6/7/8 사격  9/0 피격  =/- 속도  우클릭/휠 카메라";
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _hudStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
            }

            GUI.Label(new Rect(12f, 12f, 1100f, 160f), _hudText, _hudStyle);
        }
    }
}
