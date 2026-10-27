using Raylib_cs;

namespace BINGO_MINUS;

public class BettingUI
{
    private string _betInput = "10";
    private readonly CurrencyManager _currency;

    public decimal CurrentBet => decimal.TryParse(_betInput, out decimal result) ? result : 0m;
    public string ErrorMessage { get; set; } = "";

    public BettingUI(CurrencyManager currency)
    {
        _currency = currency;
    }
//keyboard
    public bool Update(int centerX, int yOffset)
    {

        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key >= 48 && key <= 57)
            {
                if (_betInput == "0") _betInput = "";
                _betInput += (char)key;
            }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _betInput.Length > 0)
        {
            _betInput = _betInput[..^1];
            if (_betInput == "") _betInput = "0";
        }

        if (UiButton.WasClicked(new Rectangle(centerX - 100, yOffset + 90, 200, 60)))
        {
            if (CurrentBet <= 0)
            {
                ErrorMessage = "BET MUST BE > 0";
                return false;
            }
            if (CurrentBet > _currency.Balance)
            {
                ErrorMessage = "NOT ENOUGH COINS";
                return false;
            }
            return true;
        }
        return false;
    }

    public void Draw(int centerX, int yOffset, string buttonText = "DEAL")
    {
        UiButton.Draw(new Rectangle(centerX - 100, yOffset, 200, 50), $"Bet: {_betInput}", new Color(50, 50, 50, 255));
        Raylib.DrawText("Type amount with keyboard", centerX - 110, yOffset + 60, 16, Color.LightGray);
        
        if (ErrorMessage != "")
        {
            int textWidth = Raylib.MeasureText(ErrorMessage, 20);
            Raylib.DrawText(ErrorMessage, centerX - (textWidth / 2), yOffset - 30, 20, Color.Red);
        }

        UiButton.Draw(new Rectangle(centerX - 100, yOffset + 90, 200, 60), buttonText, new Color(35, 103, 83, 255));
    }
}