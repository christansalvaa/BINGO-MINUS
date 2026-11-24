namespace BINGO_MINUS;

// Optional contract for minigames that can send the player to the deposit screen
// (e.g. from an "out of coins" popup).
// Program.cs checks this after a minigame finishes, so a new minigame only needs to
// implement this interface instead of adding another type check to Program.cs.
public interface IDepositRequester
{
    bool WantsDeposit { get; }
}
