using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>개틀링 예열(스핀업) 계산 검증.</summary>
    public sealed class GatlingMathTests
    {
        [Test]
        public void AddHeat_AccumulatesAndClamps()
        {
            float heat = 0f;
            for (int i = 0; i < 100; i++)
            {
                heat = GatlingMath.AddHeat(heat, 0.06f);
            }

            Assert.AreEqual(1f, heat, 1e-4f, "열은 1을 넘지 않아야 한다.");
        }

        [Test]
        public void DecayHeat_CoolsDownToZero()
        {
            float heat = 1f;
            for (int i = 0; i < 200; i++)
            {
                heat = GatlingMath.DecayHeat(heat, 0.8f, 1f / 60f);
            }

            Assert.AreEqual(0f, heat, 1e-4f, "발사를 멈추면 완전히 식어야 한다.");
        }

        [Test]
        public void CooldownScale_ColdGatling_IsNeutral()
        {
            Assert.AreEqual(1f, GatlingMath.CooldownScale(0f, 0.4f), 1e-4f);
        }

        [Test]
        public void CooldownScale_FullHeat_ReachesMinScale()
        {
            Assert.AreEqual(0.4f, GatlingMath.CooldownScale(1f, 0.4f), 1e-4f,
                "최대 예열 시 최대 연사 가속이어야 한다.");
        }

        [Test]
        public void CooldownScale_NeverZero()
        {
            Assert.Greater(GatlingMath.CooldownScale(1f, 0f), 0f,
                "쿨다운 배율이 0이 되면 무한 난사 — 절대 금지 (GDD 3.2).");
        }
    }
}
