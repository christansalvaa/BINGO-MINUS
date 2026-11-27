using System;
using System.Collections.Generic;

namespace BINGO_MINUS;

public enum BaccaratBetSpot
{
    Player,
    Banker,
    Tie,
    PlayerPair,
    BankerPair
}

public enum BaccaratOutcome
{
    PlayerWin,
    BankerWin,
    Tie
}

public sealed record BaccaratRoundRecord(
    BaccaratOutcome Outcome,
    int PlayerScore,
    int BankerScore,
    bool PlayerPair,
    bool BankerPair,
    bool IsNatural);

public sealed class BaccaratShoe
{
    public const int DeckCount = 8;
    public const int TotalCards = DeckCount * 52;
    public const int ReshuffleThreshold = 24;

    private readonly List<Card> _cards = new(TotalCards);
    private readonly Random _rng = new();

    public int Remaining => _cards.Count;
    public bool NeedsReshuffle => _cards.Count < ReshuffleThreshold;

    public BaccaratShoe()
    {
        ResetAndShuffle();
    }

    public void ResetAndShuffle()
    {
        _cards.Clear();
        for (int d = 0; d < DeckCount; d++)
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card(suit, rank));
                }
            }
        }

        // Fisher-Yates shuffle
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            ResetAndShuffle();
        }
        int last = _cards.Count - 1;
        Card card = _cards[last];
        _cards.RemoveAt(last);
        return card;
    }
}

public static class BaccaratRules
{
    public static int GetCardValue(Card card) => card.Rank switch
    {
        Rank.Ace => 1,
        Rank.Ten or Rank.Jack or Rank.Queen or Rank.King => 0,
        _ => (int)card.Rank
    };

    public static int GetHandScore(IReadOnlyList<Card> cards)
    {
        int sum = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            sum += GetCardValue(cards[i]);
        }
        return sum % 10;
    }

    public static bool IsNatural(IReadOnlyList<Card> playerTwoCards, IReadOnlyList<Card> bankerTwoCards)
    {
        int p = GetHandScore(playerTwoCards);
        int b = GetHandScore(bankerTwoCards);
        return p >= 8 || b >= 8;
    }

    public static bool ShouldPlayerDraw(int playerTwoCardScore) => playerTwoCardScore <= 5;

    // Standard casino Punto Banco Banker third-card tableau
    public static bool ShouldBankerDraw(int bankerTwoCardScore, int? playerThirdCardValue)
    {
        if (playerThirdCardValue is null)
        {
            // Player stood on 6 or 7
            return bankerTwoCardScore <= 5;
        }

        int p3 = playerThirdCardValue.Value;
        return bankerTwoCardScore switch
        {
            0 or 1 or 2 => true,
            3 => p3 != 8,
            4 => p3 is >= 2 and <= 7,
            5 => p3 is >= 4 and <= 7,
            6 => p3 is 6 or 7,
            _ => false
        };
    }

    public static BaccaratOutcome DetermineOutcome(int playerScore, int bankerScore)
    {
        if (playerScore > bankerScore) return BaccaratOutcome.PlayerWin;
        if (bankerScore > playerScore) return BaccaratOutcome.BankerWin;
        return BaccaratOutcome.Tie;
    }

    public static decimal CalculateSpotReturn(
        BaccaratBetSpot spot,
        decimal wager,
        BaccaratOutcome outcome,
        bool isPlayerPair,
        bool isBankerPair)
    {
        if (wager <= 0m) return 0m;

        return spot switch
        {
            BaccaratBetSpot.Player => outcome switch
            {
                BaccaratOutcome.PlayerWin => wager * 2.00m,
                BaccaratOutcome.Tie => wager, // Push on tie
                _ => 0m
            },
            BaccaratBetSpot.Banker => outcome switch
            {
                BaccaratOutcome.BankerWin => decimal.Round(wager * 1.95m, 2), // 5% commission
                BaccaratOutcome.Tie => wager, // Push on tie
                _ => 0m
            },
            BaccaratBetSpot.Tie => outcome == BaccaratOutcome.Tie ? wager * 9.00m : 0m,
            BaccaratBetSpot.PlayerPair => isPlayerPair ? wager * 12.00m : 0m,
            BaccaratBetSpot.BankerPair => isBankerPair ? wager * 12.00m : 0m,
            _ => 0m
        };
    }
}
