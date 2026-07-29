using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 애니메이터 파라미터 좌표 변환 검증 — 월드 속도 + 시각 yaw → MoveX/MoveZ (Docs/05 §6).
    /// </summary>
    public sealed class MechaAnimParamsTests
    {
        private const float ReferenceSpeed = 14f;

        [Test]
        public void ComputeMove_ForwardFlight_MapsToMoveZ1()
        {
            Vector2 move = MechaAnimParams.ComputeMove(
                new Vector3(0f, 0f, ReferenceSpeed), Vector3.forward, ReferenceSpeed);

            Assert.AreEqual(0f, move.x, 1e-4f);
            Assert.AreEqual(1f, move.y, 1e-4f, "시각 전방과 같은 방향 이동은 MoveZ=1이어야 한다.");
        }

        [Test]
        public void ComputeMove_VisualYaw90_TransformsToLocalSpace()
        {
            // 기체가 +X(요 90도)를 보며 +X로 이동 → 로컬 기준 전진.
            Vector2 move = MechaAnimParams.ComputeMove(
                new Vector3(ReferenceSpeed, 0f, 0f), Vector3.right, ReferenceSpeed);

            Assert.AreEqual(0f, move.x, 1e-4f);
            Assert.AreEqual(1f, move.y, 1e-4f, "요 90도 상태의 +X 이동은 로컬 전진이어야 한다.");
        }

        [Test]
        public void ComputeMove_MovingLeftOfFacing_MapsToNegativeMoveX()
        {
            // 기체가 +X를 보는데 +Z로 이동 → 기체 기준 좌측 스트레이프.
            Vector2 move = MechaAnimParams.ComputeMove(
                new Vector3(0f, 0f, ReferenceSpeed), Vector3.right, ReferenceSpeed);

            Assert.AreEqual(-1f, move.x, 1e-4f, "시각 전방의 좌측 이동은 MoveX=-1이어야 한다.");
            Assert.AreEqual(0f, move.y, 1e-4f);
        }

        [Test]
        public void ComputeMove_ZeroVelocity_ReturnsZero()
        {
            Vector2 move = MechaAnimParams.ComputeMove(Vector3.zero, Vector3.forward, ReferenceSpeed);

            Assert.AreEqual(0f, move.magnitude, 1e-5f, "정지 시 (0,0)에 수렴해야 한다.");
        }

        [Test]
        public void ComputeMove_OverSpeed_ClampedToUnitCircle()
        {
            // 대시(42m/s)나 대각 임펄스 초과분은 단위원 안으로 클램프.
            Vector2 move = MechaAnimParams.ComputeMove(
                new Vector3(ReferenceSpeed, 0f, ReferenceSpeed), Vector3.forward, ReferenceSpeed);

            Assert.AreEqual(1f, move.magnitude, 1e-4f, "정규화 결과가 단위원을 넘으면 안 된다.");
        }

        [Test]
        public void ComputeMove_TiltedForward_UsesPlanarProjection()
        {
            // 시각 루트가 앞으로 숙여져도(lean) 수평 투영 요 기준으로 계산해야 한다.
            Vector3 tiltedForward = (Vector3.forward + Vector3.down * 0.5f).normalized;
            Vector2 move = MechaAnimParams.ComputeMove(
                new Vector3(0f, 0f, ReferenceSpeed), tiltedForward, ReferenceSpeed);

            Assert.AreEqual(0f, move.x, 1e-4f);
            Assert.AreEqual(1f, move.y, 1e-4f, "기울어진 forward의 수평 성분만 사용해야 한다.");
        }

        [Test]
        public void ComputeMove_DegenerateInputs_ReturnZeroSafely()
        {
            Assert.AreEqual(Vector2.zero, MechaAnimParams.ComputeMove(
                new Vector3(1f, 0f, 1f), Vector3.up, ReferenceSpeed), "수직 forward(수평 성분 0)는 0을 반환해야 한다.");
            Assert.AreEqual(Vector2.zero, MechaAnimParams.ComputeMove(
                new Vector3(1f, 0f, 1f), Vector3.forward, 0f), "기준속도 0은 0을 반환해야 한다.");
        }

        [Test]
        public void ComputeSpeed_HorizontalOnly_IgnoresVertical()
        {
            float speed = MechaAnimParams.ComputeSpeed(
                new Vector3(0f, 9f, ReferenceSpeed), ReferenceSpeed);

            Assert.AreEqual(1f, speed, 1e-4f, "수직 속도는 Speed에 포함되면 안 된다.");
        }

        [Test]
        public void ComputeSpeed_Dash_ExceedsOne()
        {
            float speed = MechaAnimParams.ComputeSpeed(
                new Vector3(0f, 0f, 42f), ReferenceSpeed);

            Assert.AreEqual(3f, speed, 1e-4f, "대시 속도는 재생속도 보정을 위해 1을 넘겨 전달한다.");
        }

        [Test]
        public void ComputeSpeed_Stationary_ClampsToOne()
        {
            // Ground 상태의 speedParameter로 쓰이므로 정지 시에도 1 — Idle이 멈추면 안 된다.
            float speed = MechaAnimParams.ComputeSpeed(Vector3.zero, ReferenceSpeed);

            Assert.AreEqual(1f, speed, 1e-4f, "정지 시 재생속도 배율은 1이어야 한다 (Idle 정지 방지).");
        }

        [Test]
        public void ComputeDashDirection_QuadrantMapping()
        {
            // +Z를 보며 +X 대쉬 → 로컬 우측 (1,0)
            Vector2 right = MechaAnimParams.ComputeDashDirection(
                new Vector3(42f, 0f, 0f), Vector3.forward);
            Assert.AreEqual(1f, right.x, 1e-4f);
            Assert.AreEqual(0f, right.y, 1e-4f);

            // +X를 보며 +X 대쉬 → 로컬 전방 (0,1)
            Vector2 fwd = MechaAnimParams.ComputeDashDirection(
                new Vector3(42f, 0f, 0f), Vector3.right);
            Assert.AreEqual(0f, fwd.x, 1e-4f);
            Assert.AreEqual(1f, fwd.y, 1e-4f);

            // +Z를 보며 대각(-X,-Z) 대쉬 → 로컬 후좌 단위 벡터
            Vector2 diag = MechaAnimParams.ComputeDashDirection(
                new Vector3(-30f, 0f, -30f), Vector3.forward);
            Assert.AreEqual(-0.7071f, diag.x, 1e-3f);
            Assert.AreEqual(-0.7071f, diag.y, 1e-3f);
            Assert.AreEqual(1f, diag.magnitude, 1e-4f, "대쉬 방향은 단위 벡터여야 한다.");
        }

        [Test]
        public void ComputeDashDirection_NoHorizontalVelocity_FallsBackForward()
        {
            Vector2 dir = MechaAnimParams.ComputeDashDirection(
                new Vector3(0f, -9f, 0f), Vector3.forward);

            Assert.AreEqual(Vector2.up, dir, "수평 성분이 없으면 전방 폴백이어야 한다.");
        }

        [Test]
        public void GetFireGroup_MapsWeaponsToRecoilGroups()
        {
            Assert.AreEqual(MechaAnimParams.FireGroupLight, MechaAnimParams.GetFireGroup("gatling"));
            Assert.AreEqual(MechaAnimParams.FireGroupLight, MechaAnimParams.GetFireGroup("gravity_well"));
            Assert.AreEqual(MechaAnimParams.FireGroupLauncher, MechaAnimParams.GetFireGroup("missile_pod"));
            Assert.AreEqual(MechaAnimParams.FireGroupLauncher, MechaAnimParams.GetFireGroup("orbital_strike"));
            Assert.AreEqual(MechaAnimParams.FireGroupHeavy, MechaAnimParams.GetFireGroup("railgun"));
            Assert.AreEqual(MechaAnimParams.FireGroupHeavy, MechaAnimParams.GetFireGroup("shotgun_cannon"));
        }

        [Test]
        public void GetFireGroup_UnknownOrNull_FallsBackToLight()
        {
            Assert.AreEqual(MechaAnimParams.FireGroupLight, MechaAnimParams.GetFireGroup("new_weapon_9000"));
            Assert.AreEqual(MechaAnimParams.FireGroupLight, MechaAnimParams.GetFireGroup(null));
        }

        [Test]
        public void ResolveFireGroup_WithinWindow_EscalatesOnly()
        {
            // 유지창 내 상위 그룹은 승격
            Assert.AreEqual(MechaAnimParams.FireGroupHeavy, MechaAnimParams.ResolveFireGroup(
                MechaAnimParams.FireGroupLight, MechaAnimParams.FireGroupHeavy, true));
            // 유지창 내 하위 그룹은 강등되지 않는다 (연사 무기가 Heavy 반동을 덮으면 안 됨)
            Assert.AreEqual(MechaAnimParams.FireGroupHeavy, MechaAnimParams.ResolveFireGroup(
                MechaAnimParams.FireGroupHeavy, MechaAnimParams.FireGroupLight, true));
        }

        [Test]
        public void ResolveFireGroup_WindowClosed_ResetsToIncoming()
        {
            Assert.AreEqual(MechaAnimParams.FireGroupLight, MechaAnimParams.ResolveFireGroup(
                MechaAnimParams.FireGroupHeavy, MechaAnimParams.FireGroupLight, false));
        }

        [Test]
        public void FireWindow_WithinWindow_Active()
        {
            float fireUntil = MechaAnimParams.ExtendFireWindow(10f, 0.3f);

            Assert.IsTrue(MechaAnimParams.IsFireActive(10.29f, fireUntil), "이벤트 후 0.3초 내에는 Fire가 유지돼야 한다.");
        }

        [Test]
        public void FireWindow_AfterWindow_Inactive()
        {
            float fireUntil = MechaAnimParams.ExtendFireWindow(10f, 0.3f);

            Assert.IsFalse(MechaAnimParams.IsFireActive(10.31f, fireUntil), "유지창 경과 후 Fire는 꺼져야 한다.");
        }

        [Test]
        public void FireWindow_RepeatedFire_ExtendsWindow()
        {
            float fireUntil = MechaAnimParams.ExtendFireWindow(10f, 0.3f);
            fireUntil = MechaAnimParams.ExtendFireWindow(10.2f, 0.3f);

            Assert.IsTrue(MechaAnimParams.IsFireActive(10.45f, fireUntil), "연사 중 재발사는 유지창을 갱신해야 한다.");
        }

        [Test]
        public void ComputeSpeed_SlowDrift_DoesNotSlowPlayback()
        {
            float speed = MechaAnimParams.ComputeSpeed(
                new Vector3(0f, 0f, ReferenceSpeed * 0.4f), ReferenceSpeed);

            Assert.AreEqual(1f, speed, 1e-4f, "저속에서도 재생속도는 1 밑으로 내려가지 않는다.");
        }
    }
}
