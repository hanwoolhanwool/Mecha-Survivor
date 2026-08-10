using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// Animator 파라미터 순수 계산 — MechaAnimationDriver에서 분리해 EditMode로 검증한다.
    /// 월드 속도를 시각 전방(yaw) 기준 로컬 좌표로 변환·정규화하는 것이 핵심.
    /// </summary>
    public static class MechaAnimParams
    {
        /// <summary>
        /// 월드 속도의 수평 성분을 시각 전방 기준 로컬 (MoveX, MoveZ)로 변환한다.
        /// 기준속도로 나눠 정규화하고 단위원 안으로 클램프한다 (대시/임펄스 초과분 컷).
        /// visualForward는 시각 루트의 forward — 기울어져 있어도 수평 투영으로 요만 취한다.
        /// </summary>
        public static Vector2 ComputeMove(Vector3 worldVelocity, Vector3 visualForward, float referenceSpeed)
        {
            Vector2 forward = new(visualForward.x, visualForward.z);
            if (referenceSpeed <= 0f || forward.sqrMagnitude < 1e-6f)
            {
                return Vector2.zero;
            }

            forward.Normalize();
            Vector2 right = new(forward.y, -forward.x);
            Vector2 planar = new(worldVelocity.x, worldVelocity.z);

            Vector2 move = new(
                Vector2.Dot(planar, right) / referenceSpeed,
                Vector2.Dot(planar, forward) / referenceSpeed);
            return Vector2.ClampMagnitude(move, 1f);
        }

        /// <summary>
        /// 대쉬 순간의 월드 속도를 시각 전방 기준 로컬 단위 방향(DashX, DashZ)으로 변환한다.
        /// 방향을 알 수 없으면(수평 성분 0) 전방 (0,1) 폴백 — BT가 항상 유효 지점을 가리키게.
        /// </summary>
        public static Vector2 ComputeDashDirection(Vector3 worldVelocity, Vector3 visualForward)
        {
            Vector2 forward = new(visualForward.x, visualForward.z);
            Vector2 planar = new(worldVelocity.x, worldVelocity.z);
            if (forward.sqrMagnitude < 1e-6f || planar.sqrMagnitude < 1e-4f)
            {
                return Vector2.up;
            }

            forward.Normalize();
            Vector2 right = new(forward.y, -forward.x);
            return new Vector2(Vector2.Dot(planar, right), Vector2.Dot(planar, forward)).normalized;
        }

        // ── 사격 반동 그룹 (Docs/05 §10-B1) — 값 = 우선순위 (클수록 우선) ──
        public const int FireGroupLight = 0;
        public const int FireGroupLauncher = 1;
        public const int FireGroupHeavy = 2;

        private static readonly Dictionary<string, int> FireGroups = new()
        {
            { "gatling", FireGroupLight },
            { "beam", FireGroupLight },
            { "laser_cannon", FireGroupLight },
            { "emp_field", FireGroupLight },
            { "gravity_well", FireGroupLight },
            { "missile_pod", FireGroupLauncher },
            { "twin_rocket", FireGroupLauncher },
            { "cluster_bomb", FireGroupLauncher },
            { "orbital_strike", FireGroupLauncher },
            { "railgun", FireGroupHeavy },
            { "shotgun_cannon", FireGroupHeavy },
        };

        /// <summary>WeaponId → 반동 그룹. 미지 무기는 Light 폴백 (신무기 추가 시 안전).</summary>
        public static int GetFireGroup(string weaponId)
        {
            return weaponId != null && FireGroups.TryGetValue(weaponId, out int group)
                ? group
                : FireGroupLight;
        }

        /// <summary>
        /// 유지창 내 동시 발사 경합 규칙 — 상위 그룹만 승격, 강등은 창이 닫힌 뒤에만.
        /// (Heavy 단발 반동이 연사 무기에 곧바로 덮이는 것을 막는다.)
        /// </summary>
        public static int ResolveFireGroup(int currentGroup, int incomingGroup, bool windowActive)
        {
            return windowActive ? Mathf.Max(currentGroup, incomingGroup) : incomingGroup;
        }

        // ── 무기별 전신 사격 포즈 (Docs/05 §10-B8·B10) — 값 = 우선순위 (클수록 우선) ──
        // AC_Mecha WeaponPose 레이어의 PoseType 정수와 1:1 대응한다.
        public const int FirePoseNone = 0;
        public const int FirePoseHero = 1;              // 오른손 한손 총
        public const int FirePoseHeroLeft = 2;          // 왼손 한손 총 (오른손 세트의 Humanoid 미러 클립)
        public const int FirePoseHeroTwoHand = 3;       // 두손 총 — 오른손 그립 + 왼손 받침
        public const int FirePoseHeroTwoHandLeft = 4;   // 두손 총 — 왼손 그립 (두손 세트의 미러 클립)

        /// <summary>
        /// 파지 방식 + 장착 손 → 전신 포즈 (Docs/05 §10-B10).
        /// 무기 ID 사전을 대체한다 — 같은 무기라도 로드아웃 순서에 따라 손이 바뀌므로
        /// 포즈는 무기 고유값이 아니라 (파지 방식, 손)의 함수다.
        ///
        /// 두손 총도 손을 가린다 (2026-08-08 수정): 두 손을 다 쓰지만 **그립을 쥐는 손**은
        /// 하나뿐이고, 무기 모델은 그 손 마운트에 붙는다 (Docs/06 §3.4). 손을 무시하면
        /// 왼손 슬롯의 두손 무기가 "빈 오른손으로 쥐는 자세 + 왼손에 매달린 총"이 된다.
        /// </summary>
        public static int ResolveWeaponPose(WeaponGrip grip, WeaponHand hand)
        {
            bool left = hand == WeaponHand.Left;
            switch (grip)
            {
                case WeaponGrip.TwoHanded:
                    return left ? FirePoseHeroTwoHandLeft : FirePoseHeroTwoHand;
                case WeaponGrip.OneHanded:
                    return left ? FirePoseHeroLeft : FirePoseHero;
                default:
                    return FirePoseNone;
            }
        }

        /// <summary>유지창 내 포즈 경합 — 반동 그룹과 동일 규칙 (승격만, 강등은 창 종료 후).</summary>
        public static int ResolveFirePose(int currentPose, int incomingPose, bool windowActive)
        {
            return ResolveFireGroup(currentPose, incomingPose, windowActive);
        }

        // ── 무기별 발사 모션 (Docs/05 §10-B11) ──
        // 전신 포즈(FirePose*)는 "쏘는 동안 유지하는 자세"고, 발사 모션은 "한 발마다 재생되는
        // 단발 동작"이다. WeaponPose 레이어가 전신 Override라 UpperBody 반동을 덮어버리므로,
        // 포즈 무기의 발사 반응은 같은 레이어의 단발 상태로 넣어야 보인다.
        //
        // 포즈와 달리 손이 아니라 **무기**가 정한다 — 같은 두손 무기라도 대출력 빔은 지속 빔이라
        // 펌프 동작이 어울리지 않는다. 좌우 그립 구분은 PoseType(3/4)이 이미 하고 있어서
        // 컨트롤러가 ShotgunFire / ShotgunFireLeft 를 알아서 고른다.
        public const int FireMotionNone = 0;
        public const int FireMotionShotgunPump = 1;   // 반동 킥 + 펌프 재장전 (0.9s)

        private static readonly Dictionary<string, int> FireMotions = new()
        {
            { "shotgun_cannon", FireMotionShotgunPump },
        };

        /// <summary>WeaponId → 발사 모션. 매핑 없는 무기는 None(모션 없음) — 폴백하지 않는다.</summary>
        public static int GetFireMotion(string weaponId)
        {
            return weaponId != null && FireMotions.TryGetValue(weaponId, out int motion)
                ? motion
                : FireMotionNone;
        }

        /// <summary>
        /// 시각 요 목표 선택 — 무기 포즈 유지 중에는 조준(카메라) 방향에 고정한다.
        /// 총구·시선을 조준 방향에 두고 몸짓만 이동 방향을 말하는 것이 방향별 포즈의
        /// 성립 조건 (Docs/07 §10-1) — 요가 이동을 따라가면 F 포즈만 보인다.
        /// 평시엔 기존 정책 그대로: 이동 중 이동 방향, 정지 시 카메라 방향(없으면 현상 유지).
        /// </summary>
        public static float SelectVisualYaw(bool poseActive, bool hasCamera, bool isMoving,
            float cameraYaw, float moveYaw, float currentYaw)
        {
            if (poseActive && hasCamera)
            {
                return cameraYaw;
            }

            if (isMoving)
            {
                return moveYaw;
            }

            return hasCamera ? cameraYaw : currentYaw;
        }

        /// <summary>
        /// 상승/하강 자세 파라미터 (Docs/05 §10-B5) — 접지 중엔 0 (보행에 수직 자세 금지),
        /// 비행 중엔 수직 입력을 -1~1로 클램프해 그대로 쓴다.
        /// </summary>
        public static float ComputeVerticalLean(float verticalInput, bool isGrounded)
        {
            return isGrounded ? 0f : Mathf.Clamp(verticalInput, -1f, 1f);
        }

        /// <summary>
        /// 강피격 판정 — 대미지가 최대체력의 threshold 비율 이상이면 강피격 (Docs/05 §10-B2).
        /// Damage 0 이하(체력 갱신 알림)는 호출 전에 걸러야 한다.
        /// </summary>
        public static bool IsHeavyHit(float damage, float maxHealth, float thresholdFraction)
        {
            // 부동소수점 경계 보정 — 정확히 임계값인 대미지(예: 100의 15% = 15)도 강피격
            return maxHealth > 0f && damage >= maxHealth * thresholdFraction - 1e-3f;
        }

        /// <summary>사격 이벤트 수신 시각으로부터 유지창 종료 시각을 계산한다.</summary>
        public static float ExtendFireWindow(float now, float window)
        {
            return now + window;
        }

        /// <summary>Fire 파라미터 값 — 유지창이 아직 열려 있는가.</summary>
        public static bool IsFireActive(float now, float fireUntil)
        {
            return now < fireUntil;
        }

        /// <summary>
        /// 수평 속력 / 기준속도 — Ground 상태의 재생속도 배율(speedParameter)로 쓰인다.
        /// 하한 1: 정지 시에도 Idle이 정상 재생돼야 하므로 1 밑으로 내리지 않는다
        /// (저속 이동의 발 미끄러짐은 QA 허용 범위). 대시 중엔 1을 넘는다.
        /// </summary>
        public static float ComputeSpeed(Vector3 worldVelocity, float referenceSpeed)
        {
            if (referenceSpeed <= 0f)
            {
                return 1f;
            }

            Vector2 planar = new(worldVelocity.x, worldVelocity.z);
            return Mathf.Max(1f, planar.magnitude / referenceSpeed);
        }
    }
}
