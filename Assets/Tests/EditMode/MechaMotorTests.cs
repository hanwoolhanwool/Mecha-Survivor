using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 이동 순수 로직 검증 — "입력이 곧 속도" 원칙(관성 0)과 고도 상한 클램프.
    /// </summary>
    public sealed class MechaMotorTests
    {
        private const float HorizontalSpeed = 14f;
        private const float AscendSpeed = 9f;
        private const float DescendSpeed = 12f;

        [Test]
        public void ComputeVelocity_FullInput_ReachesTopSpeedImmediately()
        {
            Vector3 v = MechaMotor.ComputeVelocity(
                new Vector2(0f, 1f), 0f, 0f, HorizontalSpeed, AscendSpeed, DescendSpeed);

            Assert.AreEqual(HorizontalSpeed, v.z, 1e-4f, "전진 입력 즉시 최고속이어야 한다 (가속 램프 금지).");
            Assert.AreEqual(0f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.y, 1e-4f);
        }

        [Test]
        public void ComputeVelocity_ZeroInput_StopsImmediately()
        {
            Vector3 v = MechaMotor.ComputeVelocity(
                Vector2.zero, 0f, 37f, HorizontalSpeed, AscendSpeed, DescendSpeed);

            Assert.AreEqual(0f, v.magnitude, 1e-4f, "입력을 떼면 즉시 정지해야 한다 (관성 금지).");
        }

        [Test]
        public void ComputeVelocity_IsCameraYawRelative()
        {
            // 카메라가 90도(동쪽)를 볼 때 전진(W)은 +X로 나가야 한다.
            Vector3 v = MechaMotor.ComputeVelocity(
                new Vector2(0f, 1f), 0f, 90f, HorizontalSpeed, AscendSpeed, DescendSpeed);

            Assert.AreEqual(HorizontalSpeed, v.x, 1e-3f);
            Assert.AreEqual(0f, v.z, 1e-3f);
        }

        [Test]
        public void ComputeVelocity_DiagonalInput_ClampedToTopSpeed()
        {
            Vector3 v = MechaMotor.ComputeVelocity(
                new Vector2(1f, 1f), 0f, 0f, HorizontalSpeed, AscendSpeed, DescendSpeed);

            Assert.AreEqual(HorizontalSpeed, v.magnitude, 1e-3f, "대각 이동이 더 빨라선 안 된다.");
        }

        [Test]
        public void ComputeVelocity_AscendAndDescend_UseSeparateSpeeds()
        {
            Vector3 up = MechaMotor.ComputeVelocity(
                Vector2.zero, 1f, 0f, HorizontalSpeed, AscendSpeed, DescendSpeed);
            Vector3 down = MechaMotor.ComputeVelocity(
                Vector2.zero, -1f, 0f, HorizontalSpeed, AscendSpeed, DescendSpeed);

            Assert.AreEqual(AscendSpeed, up.y, 1e-4f);
            Assert.AreEqual(-DescendSpeed, down.y, 1e-4f);
        }

        [Test]
        public void ClampAscent_AtCeiling_StopsUpwardMotion()
        {
            float v = MechaMotor.ClampAscent(currentY: 60f, verticalVelocity: 9f, deltaTime: 0.02f, ceiling: 60f);

            Assert.AreEqual(0f, v, 1e-4f, "천장에서는 더 오를 수 없어야 한다.");
        }

        [Test]
        public void ClampAscent_NearCeiling_ReducesToExactHeadroom()
        {
            // 남은 여유 0.1, 원하는 이동 0.9 → 이번 프레임에 정확히 0.1만 오르는 속도로 감쇠.
            float dt = 0.1f;
            float v = MechaMotor.ClampAscent(currentY: 59.9f, verticalVelocity: 9f, deltaTime: dt, ceiling: 60f);

            Assert.AreEqual(0.1f, v * dt, 1e-4f);
        }

        [Test]
        public void ClampAscent_BelowCeiling_Unchanged()
        {
            float v = MechaMotor.ClampAscent(currentY: 10f, verticalVelocity: 9f, deltaTime: 0.02f, ceiling: 60f);

            Assert.AreEqual(9f, v, 1e-4f);
        }

        [Test]
        public void ClampAscent_Descending_PassesThrough()
        {
            float v = MechaMotor.ClampAscent(currentY: 70f, verticalVelocity: -12f, deltaTime: 0.02f, ceiling: 60f);

            Assert.AreEqual(-12f, v, 1e-4f, "하강은 천장과 무관하게 통과해야 한다.");
        }
    }
}
