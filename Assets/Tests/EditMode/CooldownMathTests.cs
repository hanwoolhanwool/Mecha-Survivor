using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>
    /// 나눗셈 쿨감 모델 검증 (GDD 3.2). 이 계산식이 게임의 생사를 가른다:
    /// 쿨다운은 0에 수렴하되 절대 도달하지 않아야 로테이션이 붕괴하지 않는다.
    /// </summary>
    public sealed class CooldownMathTests
    {
        // GDD 3.2 표 그대로 검증 (기본 6초).
        [TestCase(0f, 6f)]      // 쿨감 0% → 6.00초
        [TestCase(1f, 3f)]      // +100% → 3.00초 (발사 ×2)
        [TestCase(2f, 2f)]      // +200% → 2.00초 (발사 ×3)
        [TestCase(4f, 1.2f)]    // +400% → 1.20초 (발사 ×5)
        public void Effective_MatchesGddTable(float reduction, float expected)
        {
            Assert.AreEqual(expected, CooldownMath.Effective(6f, reduction), 1e-4f);
        }

        [Test]
        public void Effective_NeverReachesZero()
        {
            // 쿨감을 아무리 쌓아도 0이 되면 안 된다 (무한 난사 = 로테이션 붕괴).
            float cd = CooldownMath.Effective(6f, 10000f);

            Assert.Greater(cd, 0f, "쿨다운은 0에 수렴하되 절대 도달하지 않아야 한다.");
        }

        [Test]
        public void Effective_GroundedMultiplier_SpeedsUpRotation()
        {
            // 착지 중 ×1.5 — 지상 = 화력 회전, 공중 = 안전 (GDD 2.2).
            Assert.AreEqual(4f, CooldownMath.Effective(6f, 0f, 1.5f), 1e-4f);
            Assert.AreEqual(3f, CooldownMath.Effective(6f, 0f, 2f), 1e-4f);
        }

        [Test]
        public void Effective_StacksReductionAndGroundedBonus()
        {
            // 6 / (1+1) / 1.5 = 2 — 적용 순서 (GDD 3.2).
            Assert.AreEqual(2f, CooldownMath.Effective(6f, 1f, 1.5f), 1e-4f);
        }

        [Test]
        public void Effective_NegativeReduction_ClampedToZero()
        {
            Assert.AreEqual(6f, CooldownMath.Effective(6f, -0.5f), 1e-4f,
                "음수 쿨감이 쿨다운을 늘려선 안 된다.");
        }

        [Test]
        public void Effective_MultiplierBelowOne_ClampedToOne()
        {
            Assert.AreEqual(6f, CooldownMath.Effective(6f, 0f, 0.5f), 1e-4f,
                "지상 배율이 1 미만으로 쿨다운을 늘려선 안 된다.");
        }
    }
}
