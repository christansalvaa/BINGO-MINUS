// Roulette rules + bet bookkeeping (no drawing here, so it is easy to test).
// European single-zero wheel: 37 pockets (0-36). Every bet pays 36 / (numbers covered) to 1 in total,
// which gives the classic table: straight 35:1, split 17:1, street/trio 11:1, corner 8:1,
// six line 5:1, dozen/column 2:1, even-money bets 1:1.

using System.Numerics;

namespace BINGO_MINUS;

internal enum RouletteColor
{
    Green,
    Red,
    Black
}

internal static class RouletteRules
{
    public const int PocketCount = 37;

    // Physical pocket order on a European wheel, clockwise starting at zero.
    public static readonly int[] WheelOrder =
    [
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14,
        31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
    ];

    private static readonly HashSet<int> RedNumbers = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36];

    public static RouletteColor ColorOf(int number)
    {
        if (number == 0) return RouletteColor.Green;
        return RedNumbers.Contains(number) ? RouletteColor.Red : RouletteColor.Black;
    }

    public static int PocketIndexOf(int number) => Array.IndexOf(WheelOrder, number);

    public static int Spin(Random rng) => rng.Next(PocketCount);

    public static int[] Range(int from, int to)
    {
        var result = new int[to - from + 1];
        for (int i = 0; i < result.Length; i++) result[i] = from + i;
        return result;
    }

    // Profit multiplier (the "35" in 35:1).
    public static int PayoutRatio(int coveredCount) => 36 / coveredCount - 1;

    // Total coins handed back for a winning bet (stake included).
    public static decimal GrossReturn(decimal stake, int coveredCount) => stake * 36m / coveredCount;
}

// A place on the table where chips can go (straight, split, street, corner, dozen, ...).
internal sealed record RouletteBetSpot(string Label, int[] Numbers, Vector2 Anchor)
{
    // Same covered numbers = same spot, so chips on it stack.
    public string Key { get; } = string.Join(',', Numbers.OrderBy(n => n));

    public int PayoutRatio => RouletteRules.PayoutRatio(Numbers.Length);

    public bool Covers(int number) => Array.IndexOf(Numbers, number) >= 0;
}

internal sealed class RouletteBet
{
    public RouletteBetSpot Spot { get; }
    public decimal Amount { get; set; }

    public RouletteBet(RouletteBetSpot spot, decimal amount)
    {
        Spot = spot;
        Amount = amount;
    }
}

// Everything the player has staked this round, with undo support.
internal sealed class RouletteBetBook
{
    private readonly Dictionary<string, RouletteBet> _bets = new();
    private readonly List<(string Key, decimal Amount)> _placements = [];

    public IEnumerable<RouletteBet> Bets => _bets.Values;

    public bool HasBets => _bets.Count > 0;

    public decimal TotalStake => _bets.Values.Sum(b => b.Amount);

    public decimal AmountOn(RouletteBetSpot spot) => _bets.TryGetValue(spot.Key, out var bet) ? bet.Amount : 0m;

    public void Place(RouletteBetSpot spot, decimal amount)
    {
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));

        if (_bets.TryGetValue(spot.Key, out var existing))
        {
            existing.Amount += amount;
        }
        else
        {
            _bets[spot.Key] = new RouletteBet(spot, amount);
        }

        _placements.Add((spot.Key, amount));
    }

    // Removes the most recent chip placement and tells the caller how much to refund.
    public bool TryUndo(out decimal refunded)
    {
        refunded = 0m;
        if (_placements.Count == 0) return false;

        var (key, amount) = _placements[^1];
        _placements.RemoveAt(_placements.Count - 1);

        var bet = _bets[key];
        bet.Amount -= amount;
        if (bet.Amount <= 0m) _bets.Remove(key);

        refunded = amount;
        return true;
    }

    // Removes every chip on one spot (right click) and returns the refund.
    public decimal RemoveSpot(RouletteBetSpot spot)
    {
        if (!_bets.Remove(spot.Key, out var bet)) return 0m;

        _placements.RemoveAll(p => p.Key == spot.Key);
        return bet.Amount;
    }

    public decimal Clear()
    {
        decimal total = TotalStake;
        _bets.Clear();
        _placements.Clear();
        return total;
    }

    public List<(RouletteBetSpot Spot, decimal Amount)> Snapshot() =>
        _bets.Values.Select(b => (b.Spot, b.Amount)).ToList();

    // Total coins returned to the player for this winning number (stake of winning bets included).
    public decimal Settle(int winningNumber)
    {
        decimal total = 0m;
        foreach (var bet in _bets.Values)
        {
            if (bet.Spot.Covers(winningNumber))
            {
                total += RouletteRules.GrossReturn(bet.Amount, bet.Spot.Numbers.Length);
            }
        }

        return total;
    }
}
