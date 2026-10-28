namespace BINGO_MINUS;

public enum Suit { Hearts, Diamonds, Clubs, Spades }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public int GetValue()
    {
        if (Rank >= Rank.Ten && Rank <= Rank.King) return 10;
        if (Rank == Rank.Ace) return 11;
        return (int)Rank;
    }

    public override string ToString() => $"{Rank} of {Suit}";
}