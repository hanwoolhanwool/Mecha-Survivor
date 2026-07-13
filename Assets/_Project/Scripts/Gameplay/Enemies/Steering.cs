using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 적끼리 겹침 방지용 분리 스티어링 순수 계산.
    /// 물리 충돌(Enemy↔Enemy)은 꺼져 있으므로 겹침은 전적으로 이 코드가 처리한다 (GDD 8.4).
    /// </summary>
    public static class Steering
    {
        /// <summary>
        /// 이웃 하나가 주는 분리 기여. 가까울수록 강하게 밀어낸다 (radius에서 0, 밀착 시 1).
        /// 반경 밖이면 zero.
        /// </summary>
        public static Vector3 PairSeparation(Vector3 self, Vector3 other, float radius)
        {
            Vector3 away = self - other;
            float sqrDistance = away.sqrMagnitude;

            if (sqrDistance >= radius * radius)
            {
                return Vector3.zero;
            }

            // 완전히 겹친 경우 방향이 없으므로 결정적 임의 방향으로 밀어낸다.
            if (sqrDistance < 1e-6f)
            {
                return new Vector3(1f, 0f, 0f);
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float strength = 1f - distance / radius;
            return away / distance * strength;
        }

        /// <summary>추적 방향과 분리 벡터를 합성해 최종 이동 방향을 만든다.</summary>
        public static Vector3 CombineChaseAndSeparation(
            Vector3 chaseDirection, Vector3 separation, float separationWeight)
        {
            Vector3 combined = chaseDirection + separation * separationWeight;
            return combined.sqrMagnitude > 1e-6f ? combined.normalized : chaseDirection;
        }
    }
}
