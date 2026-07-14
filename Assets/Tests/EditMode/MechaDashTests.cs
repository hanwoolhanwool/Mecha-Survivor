using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>대시 방향(MechaMotor.DashDirection)·측면 오프셋(CameraDynamics) 검증.</summary>
    public sealed class MechaDashTests
    {
        [Test]
        public void DashDirection_UsesMoveInput_CameraRelative()
        {
            // 카메라 요 90도(동쪽 바라봄) + 전진 입력 → 동쪽(+x)으로 대시.
            Vector3 dir = MechaMotor.DashDirection(new Vector2(0f, 1f), 90f, Vector3.forward);
            Assert.Less(Vector3.Angle(Vector3.right, dir), 1e-3f,
                "대시는 카메라 기준 입력 방향이어야 한다.");
        }

        [Test]
        public void DashDirection_NoInput_FallsBackToFacing()
        {
            Vector3 facing = new(0f, 0.5f, -1f);
            Vector3 dir = MechaMotor.DashDirection(Vector2.zero, 0f, facing);
            Assert.Less(Vector3.Angle(Vector3.back, dir), 1e-3f,
                "입력이 없으면 기체가 향한 방향(수평)으로 대시한다.");
        }

        [Test]
        public void DashDirection_AlwaysHorizontalUnit()
        {
            Vector3 dir = MechaMotor.DashDirection(new Vector2(1f, 1f), 37f, Vector3.forward);
            Assert.AreEqual(0f, dir.y, 1e-4f, "대시는 수평 기동이다.");
            Assert.AreEqual(1f, dir.magnitude, 1e-4f);
        }

        [Test]
        public void DashDirection_DegenerateFacing_StillValid()
        {
            Vector3 dir = MechaMotor.DashDirection(Vector2.zero, 0f, Vector3.up);
            Assert.AreEqual(1f, dir.magnitude, 1e-4f,
                "수직만 바라봐도 0 벡터 대시가 나오면 안 된다.");
        }

        [Test]
        public void LateralOffset_MovingRight_ShiftsCameraRight()
        {
            float offset = CameraDynamics.TargetLateralOffset(1f, 1.8f, 1f);
            Assert.AreEqual(1.8f, offset, 1e-4f,
                "우측 최고 속도면 카메라가 우측 최대로 밀려 기체가 화면 좌측에 온다.");
        }

        [Test]
        public void LateralOffset_DashAllowsOvershoot_ButClamped()
        {
            float offset = CameraDynamics.TargetLateralOffset(3f, 1.8f, 1f);
            Assert.AreEqual(1.8f * 1.5f, offset, 1e-4f,
                "대시 속도는 1.5배까지만 반영한다 — 무한정 밀리면 조준을 방해한다.");
        }

        [Test]
        public void LateralOffset_ZeroIntensity_Disables()
        {
            Assert.AreEqual(0f, CameraDynamics.TargetLateralOffset(1f, 1.8f, 0f), 1e-4f,
                "강도 0이면 연출이 완전히 꺼져야 한다 (GDD 3.6-6).");
        }
    }
}
