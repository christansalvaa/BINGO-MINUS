using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class BingoPlusHeader
{
    private static Texture2D _btnBack;
    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (!_loaded)
        {
            _btnBack = UiImageButton.LoadTexture("scenes/UI/Assets/btn_back.png");
            _loaded = true;
        }
    }

    public static bool Draw(string gameTitle, CurrencyManager currency)
    {
        EnsureLoaded();

        // Top bar background
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, 62, new Color(16, 21, 38, 255));
        Raylib.DrawLine(0, 61, GameCanvas.Width, 61, new Color(255, 180, 0, 100));

        // Back button
        Rectangle backRect = new(20, 11, 130, 40);
        bool backClicked = UiImageButton.WasClicked(backRect) || Raylib.IsKeyPressed(KeyboardKey.Escape);
        UiImageButton.Draw(_btnBack, backRect);

        // Game Title
        UiFont.DrawCenteredText(gameTitle, 17, 22, Color.White, isBold: true);

        // Coin Wallet Pill
        Rectangle balanceRect = new(GameCanvas.Width - 230, 11, 210, 40);
        Raylib.DrawRectangleRec(balanceRect, new Color(24, 32, 54, 255));
        Raylib.DrawRectangleLinesEx(balanceRect, 1, new Color(255, 215, 0, 180));

        Raylib.DrawCircle((int)balanceRect.X + 22, 31, 11, Color.Gold);
        Raylib.DrawCircleLines((int)balanceRect.X + 22, 31, 11, Color.White);
        UiFont.DrawText("$", (int)balanceRect.X + 18, 23, 14, Color.Black, isBold: true);

        string balStr = $"{currency.Balance:N2}";
        UiFont.DrawText("COINS", (int)balanceRect.X + 44, 15, 10, Color.LightGray, isBold: true);
        UiFont.DrawText(balStr, (int)balanceRect.X + 44, 26, 16, Color.Gold, isBold: true);

        return backClicked;
    }
}

