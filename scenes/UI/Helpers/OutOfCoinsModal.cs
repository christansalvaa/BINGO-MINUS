// Shared "OUT OF COINS" popup (same look as the Slots one) so new minigames don't copy the layout.
// Usage: call Update() while the popup is open and react to the result, then Draw() on top of the scene.

using Raylib_cs;

namespace BINGO_MINUS;

internal enum OutOfCoinsResult
{
    None,
    Dismissed,
    Deposit,
    Lobby
}

internal static class OutOfCoinsModal
{
    private static readonly Rectangle BoxRect = new(585, 330, 750, 420);
    private static readonly Rectangle DepositBtnRect = new(630, 638, 308, 72);
    private static readonly Rectangle LobbyBtnRect = new(983, 638, 308, 72);
    private static readonly Rectangle CloseBtnRect = new(1281, 348, 39, 39);

    // Optional artwork (all in scenes/UI/assets/casino/): modal_panel.png, modal_btn_deposit.png, modal_btn_lobby.png
    private static Texture2D _texPanel;
    private static Texture2D _texDepositBtn;
    private static Texture2D _texLobbyBtn;

    public static void LoadArt()
    {
        _texPanel = CasinoArt.Load("scenes/UI/assets/casino/modal_panel.png");
        _texDepositBtn = CasinoArt.Load("scenes/UI/assets/casino/modal_btn_deposit.png");
        _texLobbyBtn = CasinoArt.Load("scenes/UI/assets/casino/modal_btn_lobby.png");
    }

    public static OutOfCoinsResult Update()
    {
        if (UiImageButton.WasClicked(CloseBtnRect)) return OutOfCoinsResult.Dismissed;
        if (UiImageButton.WasClicked(DepositBtnRect)) return OutOfCoinsResult.Deposit;
        if (UiImageButton.WasClicked(LobbyBtnRect)) return OutOfCoinsResult.Lobby;
        return OutOfCoinsResult.None;
    }

    public static void Draw(decimal neededCoins, decimal balance, string action)
    {
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(0, 0, 0, 210));

        if (CasinoArt.Has(_texPanel))
        {
            CasinoArt.Draw(_texPanel, BoxRect);
        }
        else
        {
            Raylib.DrawRectangleRec(BoxRect, new Color(18, 24, 38, 255));
            Raylib.DrawRectangleLinesEx(BoxRect, 2, Color.Gold);
        }

        bool closeHov = UiImageButton.IsHovered(CloseBtnRect);
        Raylib.DrawRectangleRec(CloseBtnRect, closeHov ? new Color(220, 38, 38, 200) : new Color(36, 44, 62, 255));
        Raylib.DrawRectangleLinesEx(CloseBtnRect, 1.5f, closeHov ? Color.Red : Color.Gray);
        UiFont.DrawCenteredInRect("X", CloseBtnRect, 21, Color.White, isBold: true);

        Rectangle titleRect = new(BoxRect.X, BoxRect.Y + 42, BoxRect.Width, 51);
        UiFont.DrawCenteredInRect("! OUT OF COINS !", titleRect, 36, Color.Gold, isBold: true);

        Rectangle msg1 = new(BoxRect.X + 30, BoxRect.Y + 120, BoxRect.Width - 60, 42);
        UiFont.DrawCenteredInRect($"You need at least {neededCoins:N2} coins to {action}.", msg1, 27, Color.White);

        Rectangle msg2 = new(BoxRect.X + 30, BoxRect.Y + 172, BoxRect.Width - 60, 39);
        UiFont.DrawCenteredInRect($"Your current balance: {balance:N2} coins", msg2, 24, Color.LightGray);

        Rectangle hint = new(BoxRect.X + 30, BoxRect.Y + 228, BoxRect.Width - 60, 33);
        UiFont.DrawCenteredInRect("Deposit coins now or return to the main lobby.", hint, 21, new Color(160, 174, 192, 255));

        if (CasinoArt.Has(_texDepositBtn))
        {
            UiImageButton.Draw(_texDepositBtn, DepositBtnRect);
        }
        else
        {
            bool depHov = UiImageButton.IsHovered(DepositBtnRect);
            Raylib.DrawRectangleRec(DepositBtnRect, depHov ? new Color(40, 200, 100, 255) : new Color(34, 160, 80, 255));
            Raylib.DrawRectangleLinesEx(DepositBtnRect, 3, depHov ? Color.Lime : Color.Gold);
        }

        UiFont.DrawCenteredInRect("DEPOSIT MORE", DepositBtnRect, 24, Color.White, isBold: true);

        if (CasinoArt.Has(_texLobbyBtn))
        {
            UiImageButton.Draw(_texLobbyBtn, LobbyBtnRect);
        }
        else
        {
            bool lobHov = UiImageButton.IsHovered(LobbyBtnRect);
            Raylib.DrawRectangleRec(LobbyBtnRect, lobHov ? new Color(71, 85, 105, 255) : new Color(51, 65, 85, 255));
            Raylib.DrawRectangleLinesEx(LobbyBtnRect, 1.5f, lobHov ? Color.White : Color.Gray);
        }

        UiFont.DrawCenteredInRect("RETURN TO LOBBY", LobbyBtnRect, 22, Color.LightGray, isBold: true);
    }
}
