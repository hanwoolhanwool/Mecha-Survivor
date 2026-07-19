using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 애니메이터 Speed 파라미터 정규화 검증. 이 값이 블렌드 트리(0=idle, 1=run)의
    /// 입력이므로, 범위 이탈이나 0 나누기가 생기면 애니메이션이 통째로 깨진다.
    /// </summary>
    public sealed class MechaAnimationDriverTests
    {
        [Test]
        public void NormalizedSpeed_Stationary_IsZero()
        {
            Assert.AreEqual(0f, MechaAnimationDriver.NormalizedSpeed(Vector3.zero, 14f), 1e-5f);
        }

        [Test]
        public void NormalizedSpeed_AtReferenceSpeed_IsOne()
        {
            Assert.AreEqual(1f, MechaAnimationDriver.NormalizedSpeed(new Vector3(14f, 0f, 0f), 14f), 1e-5f);
        }

        [Test]
        public void NormalizedSpeed_HalfSpeedDiagonal_UsesPlanarMagnitude()
        {
            // 수평 (3,4) → 크기 5 = 기준 10의 절반. 수직 성분은 무시돼야 한다.
            Assert.AreEqual(0.5f, MechaAnimationDriver.NormalizedSpeed(new Vector3(3f, 99f, 4f), 10f), 1e-5f);
        }

        [Test]
        public void NormalizedSpeed_DashOverReference_ClampsToOne()
        {
            // 대시(42) > 보행 기준(14) — 블렌드 입력은 1로 클램프.
            Assert.AreEqual(1f, MechaAnimationDriver.NormalizedSpeed(new Vector3(42f, 0f, 0f), 14f), 1e-5f);
        }

        [Test]
        public void NormalizedSpeed_ZeroReference_ReturnsZeroInsteadOfNaN()
        {
            Assert.AreEqual(0f, MechaAnimationDriver.NormalizedSpeed(new Vector3(5f, 0f, 0f), 0f), 1e-5f);
        }
    }
}
