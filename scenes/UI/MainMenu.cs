using Raylib_cs;

namespace BINGO_MINUS;

internal enum MainMenuAction
{
    None,
    PlayCrash,
    PlaySlots,
    OpenPaywall,
    PlayBlackjack,
    PlayCrossroads
}
internal sealed class MainMenuScene
{
    private readonly CurrencyManager _currency;

    public MainMenuScene(CurrencyManager currency)
    {
        _currency = currency;
    }

    public MainMenuAction Update()
    {
        if (UiButton.WasClicked(new Rectangle(730, 28, 190, 52)))
        {
            return MainMenuAction.OpenPaywall;
        }

        if (UiButton.WasClicked(new Rectangle(100, 260, 350, 235)))
        {
            return MainMenuAction.PlayCrash;
        }

        if (UiButton.WasClicked(new Rectangle(510, 260, 350, 235)))
        {
            return MainMenuAction.PlaySlots;
        }

        if (UiButton.WasClicked(new Rectangle(100, 510, 350, 235)))
        {
            return MainMenuAction.PlayBlackjack;
        }

        if (UiButton.WasClicked(new Rectangle(510, 510, 350, 235)))
        {
            return MainMenuAction.PlayCrossroads;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One)) return MainMenuAction.PlayCrash;
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) return MainMenuAction.PlaySlots;
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) return MainMenuAction.PlayBlackjack;
        if (Raylib.IsKeyPressed(KeyboardKey.Four)) return MainMenuAction.PlayCrossroads;
        if (Raylib.IsKeyPressed(KeyboardKey.P)) return MainMenuAction.OpenPaywall;

        return MainMenuAction.None;
    }

    public void Draw()
    {
        DrawCenteredText("BINGO MINUS", 70, 48, Color.RayWhite);
        DrawCenteredText("YOUR LUCKY LOBBY", 132, 18, Color.LightGray);

        Raylib.DrawText($"COINS  {_currency.Balance:0.00}", 40, 42, 22, Color.Gold);
        UiButton.Draw(new Rectangle(730, 28, 190, 52), "GET COINS", new Color(150, 94, 35, 255));

        DrawCenteredText("Choose a game", 205, 24, Color.RayWhite);

        UiButton.Draw(new Rectangle(100, 260, 350, 235), "PLAY CRASH", new Color(35, 103, 83, 255));
        Raylib.DrawText("Cash out before the multiplier crashes.", 123, 346, 18, Color.LightGray);
        Raylib.DrawText("1", 123, 286, 16, Color.Gold);

        UiButton.Draw(new Rectangle(510, 260, 350, 235), "PLAY SLOTS", new Color(113, 62, 119, 255));
        Raylib.DrawText("Spin the reels and match symbols.", 533, 346, 18, Color.LightGray);
        Raylib.DrawText("2", 533, 286, 16, Color.Gold);

        UiButton.Draw(new Rectangle(100, 510, 350, 235), "PLAY BLACKJACK", new Color(139, 0, 0, 255)); 
        Raylib.DrawText("Beat the dealer to 21.", 123, 596, 18, Color.LightGray);
        Raylib.DrawText("3", 123, 536, 16, Color.Gold);

        UiButton.Draw(new Rectangle(510, 510, 350, 235), "PLAY CROSSROADS", new Color(25, 25, 112, 255)); 
        Raylib.DrawText("Cross the road for big multipliers.", 533, 596, 18, Color.LightGray);
        Raylib.DrawText("4", 533, 536, 16, Color.Gold);

        DrawCenteredText("DEMO CURRENCY ONLY - NO REAL-MONEY PURCHASES", 800, 16, Color.Gray);
        DrawCenteredText("P: coin shop   ESC: close window", 830, 16, Color.Gray);
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color)
    {
        int x = (GameCanvas.Width - Raylib.MeasureText(text, fontSize)) / 2;
        Raylib.DrawText(text, x, y, fontSize, color);
    }
}
