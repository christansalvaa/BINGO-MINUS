using System;
using System.Collections.Generic;

namespace BINGO_MINUS;

public class Deck
{
    private List<Card> cards = new List<Card>();
    private Random rng = new Random();

    public Deck() => ResetAndShuffle();

    public void ResetAndShuffle()
    {
        cards.Clear();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                cards.Add(new Card(suit, rank));
            }
        }

        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (cards[k], cards[n]) = (cards[n], cards[k]);
        }
    }

    public Card DrawCard()
    {
        if (cards.Count == 0) ResetAndShuffle();
        Card drawn = cards[0];
        cards.RemoveAt(0);
        return drawn;
    }
}