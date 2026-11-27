namespace BINGO_MINUS.Mines
{
    public sealed class RoundResult
    {
        public double Bet;
        public double Payout;
        public double Multiplier;
        public double Profit => Payout - Bet;
    }

    /// <summary>Session totals + the last few rounds (shown in the right panel).</summary>
    public static class Stats
    {
        public const int MaxHistory = 10;

        public static double Profit;
        public static double Wagered;
        public static int Wins;
        public static int Losses;
        public static readonly List<RoundResult> History = new();

        public static void Record(double bet, double payout, double multiplier)
        {
            Wagered += bet;
            Profit += payout - bet;
            if (payout > bet) Wins++; else Losses++;

            History.Insert(0, new RoundResult { Bet = bet, Payout = payout, Multiplier = multiplier });
            if (History.Count > MaxHistory) History.RemoveAt(History.Count - 1);
        }
    }
}
