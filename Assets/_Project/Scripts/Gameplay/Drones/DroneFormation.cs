using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>서포트 드론 대형 순수 계산 — 플레이어 등 뒤 호(arc)에 균등 배치.</summary>
    public static class DroneFormation
    {
        /// <summary>
        /// index번째 드론의 로컬 오프셋 (요 회전 적용 전).
        /// 호는 뒤쪽(-Z, 180도)을 중심으로 arcDegrees만큼 펼쳐진다.
        /// </summary>
        public static Vector3 LocalOffset(int index, int count, float radius, float height, float arcDegrees)
        {
            float t = count <= 1 ? 0.5f : (float)index / (count - 1);
            float angle = 180f + Mathf.Lerp(-arcDegrees * 0.5f, arcDegrees * 0.5f, t);
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
        }
    }
}
