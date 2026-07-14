using NUnit.Framework;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>3택 리롤 예산 검증 (GDD §9-3 — 레벨업당 1회).</summary>
    public sealed class RerollBudgetTests
    {
        [Test]
        public void NewBudget_StartsFull()
        {
            var budget = new RerollBudget(1);
            Assert.AreEqual(1, budget.Remaining);
        }

        [Test]
        public void TryConsume_SpendsOne()
        {
            var budget = new RerollBudget(1);
            Assert.IsTrue(budget.TryConsume());
            Assert.AreEqual(0, budget.Remaining);
        }

        [Test]
        public void TryConsume_Exhausted_Fails()
        {
            var budget = new RerollBudget(1);
            budget.TryConsume();
            Assert.IsFalse(budget.TryConsume(), "예산 소진 후 리롤은 거부돼야 한다.");
            Assert.AreEqual(0, budget.Remaining, "실패한 소비가 예산을 깎으면 안 된다.");
        }

        [Test]
        public void Reset_RestoresBudget_EachPick()
        {
            var budget = new RerollBudget(1);
            budget.TryConsume();
            budget.Reset();
            Assert.AreEqual(1, budget.Remaining, "새 3택이 열리면 리롤이 회복돼야 한다.");
        }

        [Test]
        public void NegativePerPick_ClampsToZero()
        {
            var budget = new RerollBudget(-3);
            Assert.AreEqual(0, budget.PerPick);
            Assert.IsFalse(budget.TryConsume());
        }
    }
}
