namespace MinesCasino
{
    /// <summary>Play-money balance. No real money anywhere.</summary>
    public static class Wallet
    {
        public const double StartingBalance = 1000.00;
        public static double Balance { get; private set; } = StartingBalance;

        public static bool TryDeduct(double amount)
        {
            if (amount <= 0 || amount > Balance + 0.0001) return false;
            Balance = Math.Round(Balance - amount, 2);
            return true;
        }

        public static void Add(double amount)
        {
            Balance = Math.Round(Balance + amount, 2);
        }

        public static void Reset()
        {
            Balance = StartingBalance;
        }
    }
}
