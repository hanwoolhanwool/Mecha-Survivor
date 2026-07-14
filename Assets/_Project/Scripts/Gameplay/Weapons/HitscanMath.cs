using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>히트스캔 무기(레이저·레일건) 순수 계산 — EditMode 테스트 대상.</summary>
    public static class HitscanMath
    {
        /// <summary>
        /// 부채꼴 확산 방향 (레이저 캐논 강화 — 라인 2→4갈래, GDD 3.4).
        /// count개 라인을 totalAngle에 균등 배치. index는 0..count-1, axis 기준 회전.
        /// count가 1이면 forward 그대로.
        /// </summary>
        public static Vector3 FanDirection(Vector3 forward, Vector3 axis, int index, int count,
            float totalAngleDegrees)
        {
            forward = forward.normalized;
            if (count <= 1)
            {
                return forward;
            }

            float t = Mathf.Clamp01(index / (float)(count - 1));
            float angle = (t - 0.5f) * totalAngleDegrees;
            return Quaternion.AngleAxis(angle, axis) * forward;
        }

        /// <summary>
        /// RaycastNonAlloc 결과(순서 보장 없음)를 거리 오름차순 인덱스로 정렬한다.
        /// 관통 무기는 가까운 대상부터 뚫어야 "몇 번째에서 멈췄는가"가 성립한다.
        /// 버퍼 재사용 전제 — 할당 없음 (삽입 정렬, count는 수십 이하).
        /// </summary>
        public static void SortIndicesByDistance(float[] distances, int count, int[] indices)
        {
            count = Mathf.Min(count, Mathf.Min(distances.Length, indices.Length));

            for (int i = 0; i < count; i++)
            {
                indices[i] = i;
            }

            for (int i = 1; i < count; i++)
            {
                int key = indices[i];
                float keyDistance = distances[key];
                int j = i - 1;

                while (j >= 0 && distances[indices[j]] > keyDistance)
                {
                    indices[j + 1] = indices[j];
                    j--;
                }

                indices[j + 1] = key;
            }
        }
    }
}
