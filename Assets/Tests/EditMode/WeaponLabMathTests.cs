using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>무기 실험실 표적 링 배치·인덱스 순환 검증.</summary>
    public sealed class WeaponLabMathTests
    {
        [Test]
        public void RingPosition_KeepsRadius()
        {
            Vector3 center = new(5f, 0f, -3f);
            for (int i = 0; i < 8; i++)
            {
                Vector3 pos = WeaponLabMath.RingPosition(center, 20f, i, 8);
                Assert.AreEqual(20f, Vector3.Distance(center, pos), 1e-3f,
                    "모든 표적은 링 반경 위에 있어야 한다.");
                Assert.AreEqual(center.y, pos.y, 1e-4f, "링은 수평면이어야 한다.");
            }
        }

        [Test]
        public void RingPosition_DistributesEvenly()
        {
            Vector3 a = WeaponLabMath.RingPosition(Vector3.zero, 10f, 0, 4);
            Vector3 b = WeaponLabMath.RingPosition(Vector3.zero, 10f, 1, 4);
            Vector3 c = WeaponLabMath.RingPosition(Vector3.zero, 10f, 2, 4);

            Assert.AreEqual(90f, Vector3.Angle(a, b), 1e-2f, "4개면 90도 간격이어야 한다.");
            Assert.AreEqual(180f, Vector3.Angle(a, c), 1e-2f);
        }

        [Test]
        public void WrapIndex_WrapsForward()
        {
            Assert.AreEqual(0, WeaponLabMath.WrapIndex(11, 11), "끝 다음은 처음으로 돌아온다.");
            Assert.AreEqual(1, WeaponLabMath.WrapIndex(12, 11));
        }

        [Test]
        public void WrapIndex_WrapsBackward()
        {
            Assert.AreEqual(10, WeaponLabMath.WrapIndex(-1, 11), "처음 이전은 끝으로 돌아온다.");
        }

        [Test]
        public void WrapIndex_EmptyList_ReturnsZero()
        {
            Assert.AreEqual(0, WeaponLabMath.WrapIndex(5, 0));
        }
    }
}
