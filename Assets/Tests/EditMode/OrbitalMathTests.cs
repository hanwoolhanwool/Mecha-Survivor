using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>궤도 폭격 융단 마커 배치 검증.</summary>
    public sealed class OrbitalMathTests
    {
        [Test]
        public void MarkerPosition_FirstMarker_IsAtCenter()
        {
            Vector3 center = new(10f, 0f, -5f);
            Vector3 pos = OrbitalMath.MarkerPosition(center, Vector3.forward, 0, 7f);
            Assert.AreEqual(center, pos, "첫 마커는 조준점에 찍혀야 한다.");
        }

        [Test]
        public void MarkerPosition_ConsecutiveMarkers_KeepSpacing()
        {
            Vector3 a = OrbitalMath.MarkerPosition(Vector3.zero, Vector3.forward, 1, 7f);
            Vector3 b = OrbitalMath.MarkerPosition(Vector3.zero, Vector3.forward, 2, 7f);
            Assert.AreEqual(7f, Vector3.Distance(a, b), 1e-4f,
                "융단 마커는 일정 간격이어야 한다.");
        }

        [Test]
        public void MarkerPosition_FlattensForwardDirection()
        {
            Vector3 pos = OrbitalMath.MarkerPosition(
                Vector3.zero, new Vector3(0f, -0.9f, 0.1f), 3, 5f);
            Assert.AreEqual(0f, pos.y, 1e-4f,
                "내려다보며 쏴도 마커 줄은 수평으로 이어져야 한다.");
            Assert.AreEqual(15f, pos.z, 1e-3f);
        }

        [Test]
        public void MarkerPosition_VerticalAim_FallsBackToForward()
        {
            Vector3 pos = OrbitalMath.MarkerPosition(Vector3.zero, Vector3.down, 2, 4f);
            Assert.AreEqual(new Vector3(0f, 0f, 8f), pos,
                "수평 성분이 없는 조준이면 기본 전방으로 이어야 한다 (0 벡터 금지).");
        }
    }
}
