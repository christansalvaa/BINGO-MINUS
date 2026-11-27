// Color Game rules + bet bookkeeping (no drawing here, so it is easy to test).
// Standard Perya rules: 3 dice, each with the 6 colours (one per face).
// A bet on a colour pays 1:1 for EVERY die that lands on it:
//   0 dice -> stake lost | 1 die -> stake + 1x | 2 dice -> stake + 2x | 3 dice -> stake + 3x

namespace BINGO_MINUS;

internal enum DiceColor
{
    Yellow,
    White,
    Pink,
    Blue,
    Red,
    Green
}

internal static class ColorGameRules
{
    public const int DiceCount = 3;

    public static readonly DiceColor[] All = Enum.GetValues<DiceColor>();

    public static DiceColor[] Roll(Random rng)
    {
        var dice = new DiceColor[DiceCount];
        for (int i = 0; i < dice.Length; i++)
        {
            dice[i] = All[rng.Next(All.Length)];
        }

        return dice;
    }

    public static int Matches(DiceColor color, DiceColor[] dice) => dice.Count(d => d == color);

    // Total coins handed back for one colour bet (stake included).
    public static decimal GrossReturn(decimal stake, int matches) => matches == 0 ? 0m : stake * (1 + matches);

    public static string NameOf(DiceColor color) => color.ToString().ToUpperInvariant();
}

// Everything the player has staked this round, with undo support.
internal sealed class ColorGameBetBook
{
    private readonly decimal[] _stakes = new decimal[ColorGameRules.All.Length];
    private readonly List<(DiceColor Color, decimal Amount)> _placements = [];

    public decimal TotalStake => _stakes.Sum();

    public bool HasBets => _stakes.Any(s => s > 0m);

    public decimal StakeOn(DiceColor color) => _stakes[(int)color];

    public void Place(DiceColor color, decimal amount)
    {
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));

        _stakes[(int)color] += amount;
        _placements.Add((color, amount));
    }

    public bool TryUndo(out decimal refunded)
    {
        refunded = 0m;
        if (_placements.Count == 0) return false;

        var (color, amount) = _placements[^1];
        _placements.RemoveAt(_placements.Count - 1);
        _stakes[(int)color] -= amount;

        refunded = amount;
        return true;
    }

    // Removes every chip on one colour (right click) and returns the refund.
    public decimal RemoveColor(DiceColor color)
    {
        decimal refund = _stakes[(int)color];
        _stakes[(int)color] = 0m;
        _placements.RemoveAll(p => p.Color == color);
        return refund;
    }

    public decimal Clear()
    {
        decimal total = TotalStake;
        Array.Clear(_stakes);
        _placements.Clear();
        return total;
    }

    public decimal[] Snapshot() => (decimal[])_stakes.Clone();

    // Total coins returned to the player for this roll (stake of matching colours included).
    public decimal Settle(DiceColor[] dice)
    {
        decimal total = 0m;
        foreach (DiceColor color in ColorGameRules.All)
        {
            decimal stake = _stakes[(int)color];
            if (stake > 0m)
            {
                total += ColorGameRules.GrossReturn(stake, ColorGameRules.Matches(color, dice));
            }
        }

        return total;
    }
}
