using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>RigLab 조작의 순수 계산 — EditMode 테스트 대상 (Docs/06 §6).</summary>
    public static class RigLabMath
    {
        public const float MinPlaybackSpeed = 0.1f;
        public const float MaxPlaybackSpeed = 2f;

        /// <summary>
        /// 8방 순환 방향 (0=전방, 시계 방향). 애니메이션 8방 블렌드 확인용 (MoveX, MoveZ).
        /// </summary>
        public static Vector2 DirectionForIndex(int index)
        {
            int i = ((index % 8) + 8) % 8;
            float rad = i * Mathf.PI / 4f;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }

        /// <summary>재생 속도 가감 — 0.1×~2× 클램프 (Docs/06 §4.2-④).</summary>
        public static float StepPlaybackSpeed(float current, float delta)
        {
            return Mathf.Clamp(current + delta, MinPlaybackSpeed, MaxPlaybackSpeed);
        }

        /// <summary>목록 순환 인덱스 (음수 델타 안전).</summary>
        public static int CycleIndex(int current, int delta, int count)
        {
            return count <= 0 ? 0 : (((current + delta) % count) + count) % count;
        }

        /// <summary>
        /// "선택 없음(-1)"을 포함한 순환 — -1 → 0 → … → count-1 → -1.
        /// 마운트/총구 선택과 포즈 클립 선택이 같은 규칙을 쓴다. 목록이 비면 항상 -1.
        /// </summary>
        public static int CycleWithNone(int current, int delta, int count)
        {
            return count <= 0 ? -1 : CycleIndex(current + 1, delta, count + 1) - 1;
        }

        /// <summary>
        /// 장착 모델 표시 판정 — 본편 WeaponMountVisuals의 규칙을 랩에서 재현한다.
        /// equippedWeaponId가 비면 "전체 표시"(랩 기본 모드)라 모든 장착 모델을 켠다.
        /// mountWeaponId가 빈 마운트는 무기 조건이 없는 장식이라 언제나 켠다.
        /// </summary>
        public static bool ShouldShowMount(string mountWeaponId, string equippedWeaponId)
        {
            if (string.IsNullOrEmpty(equippedWeaponId) || string.IsNullOrEmpty(mountWeaponId))
            {
                return true;
            }

            return mountWeaponId == equippedWeaponId;
        }

        /// <summary>
        /// 총구가 장착 무기와 관련 있는가 — 조정 항목 목록을 무기별로 좁힐 때 쓴다 (Docs/06 §4.2-②).
        /// 세 갈래다: ① 전체 표시 모드면 전부 ② 이 무기 자신의 총구면 당연히 ③ 그 외에는
        /// **총구가 달린 마운트가 보이는가**를 따른다 — 한 마운트를 여러 무기가 공유하면
        /// (등 마운트의 missile_pod/twin_rocket) 모델이 보이는 동안 그 총구들은 다 만질 수 있어야 한다.
        /// ownerMountWeaponId가 비면 마운트가 없거나(모델 루트 기준 총구) 무기 조건이 없는
        /// 마운트라 항상 보인다 — 걸러내면 조정할 방법이 사라진다.
        /// </summary>
        public static bool ShouldShowMuzzle(string muzzleId, string ownerMountWeaponId,
            string equippedWeaponId)
        {
            if (string.IsNullOrEmpty(equippedWeaponId))
            {
                return true;
            }

            if (muzzleId == equippedWeaponId)
            {
                return true;
            }

            return ShouldShowMount(ownerMountWeaponId, equippedWeaponId);
        }
    }
}
