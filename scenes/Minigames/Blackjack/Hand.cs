using System.Collections.Generic;

namespace BINGO_MINUS;

public class Hand
{
    public List<Card> Cards { get; private set; } = new List<Card>();

    public void AddCard(Card card) => Cards.Add(card);

    public void Clear() => Cards.Clear();

    public int GetScore()
    {
        int score = 0;
        int aceCount = 0;

        foreach (var card in Cards)
        {
            score += card.GetValue();
            if (card.Rank == Rank.Ace) aceCount++;
        }

        while (score > 21 && aceCount > 0)
        {
            score -= 10;
            aceCount--;
        }

        return score;
    }
}