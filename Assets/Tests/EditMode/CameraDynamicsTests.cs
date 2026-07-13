using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 카메라 역동 연출의 순수 계산 검증.
    /// 핵심 계약: 강도 0이면 모든 효과가 완전히 꺼져 정적인 카메라가 된다 (GDD 2.4).
    /// </summary>
    public sealed class CameraDynamicsTests
    {
        [Test]
        public void TargetFov_AtZeroSpeed_ReturnsBase()
        {
            Assert.AreEqual(60f, CameraDynamics.TargetFov(60f, 75f, 0f, 1f), 1e-4f);
        }

        [Test]
        public void TargetFov_AtTopSpeed_ReturnsMax()
        {
            Assert.AreEqual(75f, CameraDynamics.TargetFov(60f, 75f, 1f, 1f), 1e-4f);
        }

        [Test]
        public void TargetFov_SpeedOverOne_ClampedToMax()
        {
            Assert.AreEqual(75f, CameraDynamics.TargetFov(60f, 75f, 3f, 1f), 1e-4f);
        }

        [Test]
        public void TargetFov_ZeroIntensity_AlwaysBase()
        {
            Assert.AreEqual(60f, CameraDynamics.TargetFov(60f, 75f, 1f, 0f), 1e-4f,
                "강도 0이면 속도와 무관하게 기본 FOV여야 한다.");
        }

        [Test]
        public void TargetBank_RightInput_RollsRight()
        {
            // 우측 이동(+x) → 음의 롤(우측 기울임).
            Assert.Less(CameraDynamics.TargetBank(1f, 10f, 1f), 0f);
            Assert.AreEqual(-10f, CameraDynamics.TargetBank(1f, 10f, 1f), 1e-4f);
        }

        [Test]
        public void TargetBank_InputClamped()
        {
            Assert.AreEqual(-10f, CameraDynamics.TargetBank(5f, 10f, 1f), 1e-4f,
                "입력이 1을 넘어도 최대 각도를 넘지 않아야 한다.");
        }

        [Test]
        public void TargetBank_ZeroIntensity_NoRoll()
        {
            Assert.AreEqual(0f, CameraDynamics.TargetBank(1f, 10f, 0f), 1e-4f);
        }

        [Test]
        public void TargetPitchOffset_Ascending_LooksUp()
        {
            // 상승 시 카메라가 아래에서 올려다봄 → 피치 감소(음수).
            Assert.AreEqual(-6f, CameraDynamics.TargetPitchOffset(1f, 6f, 1f), 1e-4f);
        }

        [Test]
        public void TargetPitchOffset_Descending_LooksDown()
        {
            Assert.AreEqual(6f, CameraDynamics.TargetPitchOffset(-1f, 6f, 1f), 1e-4f);
        }

        [Test]
        public void Smooth_ConvergesToTarget()
        {
            float value = 0f;
            for (int i = 0; i < 300; i++)
            {
                value = CameraDynamics.Smooth(value, 10f, 8f, 1f / 60f);
            }

            Assert.AreEqual(10f, value, 0.01f);
        }

        [Test]
        public void Smooth_IsFramerateStable()
        {
            // 같은 총 시간이면 프레임 수가 달라도 결과가 비슷해야 한다 (지수 보간).
            float at60 = 0f;
            for (int i = 0; i < 60; i++) at60 = CameraDynamics.Smooth(at60, 10f, 8f, 1f / 60f);

            float at30 = 0f;
            for (int i = 0; i < 30; i++) at30 = CameraDynamics.Smooth(at30, 10f, 8f, 1f / 30f);

            Assert.AreEqual(at60, at30, 0.05f);
        }
    }
}
