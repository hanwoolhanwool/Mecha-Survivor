using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 개틀링 배럴 스핀 속도 스텝 검증 — 가속·감속·클램프 (GatlingSpinVisuals.StepSpeed).
    /// </summary>
    public sealed class GatlingSpinVisualsTests
    {
        private const float MaxSpeed = 900f;
        private const float SpinUp = 0.25f;
        private const float SpinDown = 1.5f;

        [Test]
        public void StepSpeed_Firing_AcceleratesLinearly()
        {
            // 가속률 = 900/0.25 = 3600 도/초² → 0.1s 후 360.
            float speed = GatlingSpinVisuals.StepSpeed(0f, true, MaxSpeed, SpinUp, SpinDown, 0.1f);
            Assert.AreEqual(360f, speed, 1e-3f);
        }

        [Test]
        public void StepSpeed_Firing_ClampsAtMaxSpeed()
        {
            float speed = GatlingSpinVisuals.StepSpeed(890f, true, MaxSpeed, SpinUp, SpinDown, 1f);
            Assert.AreEqual(MaxSpeed, speed, 1e-3f, "최대 속도를 넘으면 안 된다.");
        }

        [Test]
        public void StepSpeed_NotFiring_DeceleratesLinearly()
        {
            // 감속률 = 900/1.5 = 600 도/초² → 0.1s 후 900-60 = 840.
            float speed = GatlingSpinVisuals.StepSpeed(MaxSpeed, false, MaxSpeed, SpinUp, SpinDown, 0.1f);
            Assert.AreEqual(840f, speed, 1e-3f);
        }

        [Test]
        public void StepSpeed_NotFiring_ClampsAtZero()
        {
            float speed = GatlingSpinVisuals.StepSpeed(10f, false, MaxSpeed, SpinUp, SpinDown, 1f);
            Assert.AreEqual(0f, speed, 1e-3f, "정지 아래로 내려가면 안 된다.");
        }

        [Test]
        public void StepSpeed_ZeroRampSeconds_JumpsToTarget()
        {
            Assert.AreEqual(MaxSpeed,
                GatlingSpinVisuals.StepSpeed(0f, true, MaxSpeed, 0f, 0f, 0.016f), 1e-3f,
                "도달 시간 0이면 즉시 최대 속도.");
            Assert.AreEqual(0f,
                GatlingSpinVisuals.StepSpeed(MaxSpeed, false, MaxSpeed, 0f, 0f, 0.016f), 1e-3f,
                "도달 시간 0이면 즉시 정지.");
        }

        [Test]
        public void StepSpeed_FullBurstThenRest_ReturnsToZero()
        {
            // 0.5s 사격 → 최대 도달, 이후 2s 휴지 → 완전 정지 (60fps 시뮬레이션).
            const float dt = 1f / 60f;
            float speed = 0f;
            for (int i = 0; i < 30; i++)
            {
                speed = GatlingSpinVisuals.StepSpeed(speed, true, MaxSpeed, SpinUp, SpinDown, dt);
            }

            Assert.AreEqual(MaxSpeed, speed, 1e-2f, "0.5s 사격이면 최대 속도에 도달해야 한다.");

            for (int i = 0; i < 120; i++)
            {
                speed = GatlingSpinVisuals.StepSpeed(speed, false, MaxSpeed, SpinUp, SpinDown, dt);
            }

            Assert.AreEqual(0f, speed, 1e-2f, "2s 휴지면 완전히 멈춰야 한다.");
        }
    }
}
