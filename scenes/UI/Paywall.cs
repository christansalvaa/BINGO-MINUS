using Raylib_cs;

namespace BINGO_MINUS;

// Provides the demo coin shop; new minigames do not need to edit this file.
internal sealed class PaywallScene
{
    private readonly CurrencyManager _currency;
    private string _message = "Demo shop: coin packs are free and use no payment service.";

    public PaywallScene(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        _message = "Demo shop: coin packs are free and use no payment service.";
    }

    public bool Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            return true;
        }

        if (UiButton.WasClicked(new Rectangle(150, 255, 190, 150)))
        {
            _currency.Add(100m);
            _message = "100 demo coins added to your balance.";
        }
        else if (UiButton.WasClicked(new Rectangle(385, 255, 190, 150)))
        {
            _currency.Add(500m);
            _message = "500 demo coins added to your balance.";
        }
        else if (UiButton.WasClicked(new Rectangle(620, 255, 190, 150)))
        {
            _currency.Add(1000m);
            _message = "1000 demo coins added to your balance.";
        }

        return UiButton.WasClicked(new Rectangle(365, 480, 230, 55));
    }

    public void Draw()
    {
        DrawCenteredText("COIN SHOP", 70, 42, Color.RayWhite);
        DrawCenteredText("DEMO ONLY - NO REAL-MONEY PURCHASES", 130, 18, Color.Gold);
        DrawCenteredText($"Your balance: {_currency.Balance:0.00} coins", 185, 23, Color.RayWhite);

        UiButton.Draw(new Rectangle(150, 255, 190, 150), "ADD 100 COINS", new Color(54, 100, 76, 255));
        UiButton.Draw(new Rectangle(385, 255, 190, 150), "ADD 500 COINS", new Color(54, 100, 76, 255));
        UiButton.Draw(new Rectangle(620, 255, 190, 150), "ADD 1000 COINS", new Color(54, 100, 76, 255));

        DrawCenteredText(_message, 435, 17, Color.LightGray);
        UiButton.Draw(new Rectangle(365, 480, 230, 55), "BACK TO LOBBY", new Color(72, 80, 99, 255));
        DrawCenteredText("ESC: return to lobby", 565, 16, Color.Gray);
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color)
    {
        int x = (GameCanvas.Width - Raylib.MeasureText(text, fontSize)) / 2;
        Raylib.DrawText(text, x, y, fontSize, color);
    }
}
