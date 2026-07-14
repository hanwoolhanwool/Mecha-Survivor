using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>EMP Lv.5 체인 감전 경로 순수 계산 — EditMode 테스트 대상.</summary>
    public static class EmpChainMath
    {
        /// <summary>
        /// 감전 체인 구성 (GDD 3.4-10 Lv.5 — 감전된 적끼리 전기가 연쇄).
        /// startIndex에서 시작해 아직 안 거친 가장 가까운 이웃으로 이어간다.
        /// maxLinkDistance보다 멀면 체인이 끊긴다. chain에는 방문 순서의 인덱스가 담긴다.
        /// 반환: 체인 길이 (시작점 포함).
        /// </summary>
        public static int BuildChain(IReadOnlyList<Vector3> points, int startIndex,
            float maxLinkDistance, int maxLinks, List<int> chain)
        {
            chain.Clear();
            if (points == null || points.Count == 0 ||
                startIndex < 0 || startIndex >= points.Count || maxLinks <= 0)
            {
                return 0;
            }

            chain.Add(startIndex);
            float maxSqr = maxLinkDistance * maxLinkDistance;
            int current = startIndex;

            while (chain.Count < maxLinks)
            {
                int nearest = -1;
                float nearestSqr = maxSqr;
                Vector3 from = points[current];

                for (int i = 0; i < points.Count; i++)
                {
                    if (chain.Contains(i)) // 체인은 maxLinks(수 개) 이하 — 선형 탐색으로 충분.
                    {
                        continue;
                    }

                    float sqr = (points[i] - from).sqrMagnitude;
                    if (sqr <= nearestSqr)
                    {
                        nearestSqr = sqr;
                        nearest = i;
                    }
                }

                if (nearest < 0)
                {
                    break; // 사거리 내 다음 고리 없음 — 체인 종료.
                }

                chain.Add(nearest);
                current = nearest;
            }

            return chain.Count;
        }
    }
}
