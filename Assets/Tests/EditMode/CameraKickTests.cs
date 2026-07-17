using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 레일건 카메라 킥 검증 — 킥은 위로 튀었다가(음의 피치) 반드시 0으로 복귀해야 한다.
    /// 복귀하지 않으면 조준선이 영구히 틀어진다 (GDD 2.4: 조준을 방해하면 실패).
    /// </summary>
    public sealed class CameraKickTests
    {
        private GameObject _go;
        private CameraDynamics _dynamics;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CameraDynamicsTest");
            _dynamics = _go.AddComponent<CameraDynamics>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void AddKick_PushesPitchUpward()
        {
            _dynamics.AddKick(2.5f);

            Assert.Less(_dynamics.CurrentPitchOffset, 0f,
                "킥은 음의 피치 오프셋(올려다봄)이어야 한다 — TargetPitchOffset과 같은 부호 규약");
        }

        [Test]
        public void Kick_DecaysBackToZero()
        {
            _dynamics.AddKick(2.5f);
            float initial = _dynamics.CurrentPitchOffset;

            // 1초 분량 감쇠 시뮬레이션 (60fps 가정).
            for (int i = 0; i < 60; i++)
            {
                _dynamics.Tick(0f, 0f, 0f, 1f / 60f);
            }

            Assert.Less(Mathf.Abs(_dynamics.CurrentPitchOffset), Mathf.Abs(initial) * 0.01f,
                "킥은 1초 안에 사실상 0으로 복귀해야 한다 — 남으면 조준이 영구히 틀어진다");
        }

        [Test]
        public void RepeatedKicks_Accumulate()
        {
            _dynamics.AddKick(1f);
            float single = _dynamics.CurrentPitchOffset;
            _dynamics.AddKick(1f);

            Assert.Less(_dynamics.CurrentPitchOffset, single,
                "연속 킥은 누적돼야 한다 (더 큰 음의 값)");
        }
    }
}
