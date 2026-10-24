namespace BINGO_MINUS;

// Stores and updates the shared player balance; new minigames do not need to edit this.
public sealed class CurrencyManager
{
    public decimal Balance { get; private set; }

    public CurrencyManager(decimal startingBalance)
    {
        if (startingBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startingBalance),
                "The starting balance cannot be negative.");
        }

        Balance = startingBalance;
    }

    public bool TrySpend(decimal amount)
    {
        ValidatePositiveAmount(amount);

        if (amount > Balance)
        {
            return false;
        }

        Balance -= amount;
        return true;
    }

    public void Add(decimal amount)
    {
        ValidatePositiveAmount(amount);
        Balance += amount;
    }

    private static void ValidatePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "The amount must be greater than zero.");
        }
    }
}