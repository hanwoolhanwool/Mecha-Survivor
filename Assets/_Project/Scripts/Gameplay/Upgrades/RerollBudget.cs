namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 3택 리롤 예산 (GDD §9-3 결정: 레벨업당 1회).
    /// 순수 로직 — 3택이 열릴 때마다 Reset, 리롤 버튼이 TryConsume.
    /// 남발을 막으면서 최악의 3택은 회피할 수 있게 한다.
    /// </summary>
    public sealed class RerollBudget
    {
        public int PerPick { get; }
        public int Remaining { get; private set; }

        public RerollBudget(int perPick)
        {
            PerPick = perPick < 0 ? 0 : perPick;
            Remaining = PerPick;
        }

        /// <summary>새 3택이 열릴 때 호출 — 예산 회복.</summary>
        public void Reset() => Remaining = PerPick;

        /// <summary>리롤 1회 소비. 예산이 없으면 false.</summary>
        public bool TryConsume()
        {
            if (Remaining <= 0)
            {
                return false;
            }

            Remaining--;
            return true;
        }
    }
}
