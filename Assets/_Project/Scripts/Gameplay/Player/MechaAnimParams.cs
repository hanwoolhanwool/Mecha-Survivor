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
