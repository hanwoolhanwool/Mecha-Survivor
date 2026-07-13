using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>포탑형 적 예측 사격(요격 해) 검증.</summary>
    public sealed class BallisticsTests
    {
        [Test]
        public void StationaryTarget_AimsDirectly()
        {
            bool solved = Ballistics.TryPredictInterceptDirection(
                Vector3.zero, new Vector3(0f, 0f, 10f), Vector3.zero, 20f, out Vector3 dir);

            Assert.IsTrue(solved);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, dir), 1e-2f);
        }

        [Test]
        public void MovingTarget_LeadsAhead()
        {
            // 타깃이 +X로 이동 → 조준은 현재 위치보다 +X쪽을 향해야 한다.
            bool solved = Ballistics.TryPredictInterceptDirection(
                Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(5f, 0f, 0f), 20f,
                out Vector3 dir);

            Assert.IsTrue(solved);
            Assert.Greater(dir.x, 0f, "이동 방향으로 리드해야 한다.");
        }

        [Test]
        public void PredictedShot_ActuallyIntercepts()
        {
            // 해가 맞는지 시뮬레이션으로 교차 검증: 예측 방향으로 쏜 탄이 타깃과 만나야 한다.
            Vector3 targetPos = new(0f, 0f, 30f);
            Vector3 targetVel = new(8f, 0f, 0f);
            const float projSpeed = 40f;

            bool solved = Ballistics.TryPredictInterceptDirection(
                Vector3.zero, targetPos, targetVel, projSpeed, out Vector3 dir);
            Assert.IsTrue(solved);

            // t 시점의 탄·타깃 거리 최솟값 탐색.
            float minDistance = float.MaxValue;
            for (float t = 0f; t < 3f; t += 0.001f)
            {
                float d = Vector3.Distance(dir * projSpeed * t, targetPos + targetVel * t);
                minDistance = Mathf.Min(minDistance, d);
            }

            Assert.Less(minDistance, 0.1f, "예측 방향의 탄이 타깃을 실제로 요격해야 한다.");
        }

        [Test]
        public void TargetFasterThanProjectile_FallsBackToDirectAim()
        {
            // 도망가는 더 빠른 타깃 — 해가 없어도 방향은 항상 반환돼야 한다.
            bool solved = Ballistics.TryPredictInterceptDirection(
                Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 50f), 20f,
                out Vector3 dir);

            Assert.IsFalse(solved);
            Assert.AreEqual(1f, dir.magnitude, 1e-3f, "폴백 방향도 정규화돼야 한다.");
        }
    }
}
