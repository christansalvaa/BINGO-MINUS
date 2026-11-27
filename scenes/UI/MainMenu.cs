using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal readonly record struct MainMenuResult(IMinigame? Game = null, bool OpenPaywall = false);

internal sealed record GameRegistryItem(
    string AssetPath,
    Func<CurrencyManager, IMinigame> CreateGame
);

internal sealed class MainMenuScene
{
    private const float HeaderHeight = 70f;
    private const float FooterHeight = 28f;
    private const float GridStartY = 265f;
    private const int Columns = 4;
    private const float CardWidth = 270f;
    private const float CardHeight = 325f;
    private const float GapX = 26f;
    private const float GapY = 24f;

    private readonly CurrencyManager _currency;

    private Texture2D _logo;
    private Texture2D _banner;
    private Texture2D _btnDeposit;
    private readonly Dictionary<string, Texture2D> _cardTextures = new(StringComparer.OrdinalIgnoreCase);

    private float _scrollY;
    private float _targetScrollY;

    // Game Registry: add a new minigame in a single line here.
    private static readonly List<GameRegistryItem> Games =
    [
        new("scenes/UI/Assets/card_crash.png",      c => new CrashMinigame(c)),
        new("scenes/UI/Assets/card_slots.png",      c => new SlotsMinigame(c)),
        new("scenes/UI/Assets/card_blackjack.png",  c => new Blackjack(c)),
        new("scenes/UI/Assets/card_crossyroad.png", c => new Crossyroad(c)),
        new("scenes/UI/Assets/card_mines.png",      c => new MinesMinigame(c)),
        new("scenes/UI/Assets/card_roulette.png",   c => new RouletteMinigame(c)),
        new("scenes/UI/Assets/card_colorgame.png",  c => new ColorGameMinigame(c)),
        new("scenes/UI/Assets/card_plinko.png",     c => new PlinkoMinigame(c)),
        new("scenes/UI/Assets/card_baccarat.png",   c => new BaccaratMinigame(c)),
    ];

    public MainMenuScene(CurrencyManager currency)
    {
        _currency = currency;
        LoadAssets();
    }

    private void LoadAssets()
    {
        _logo = UiImageButton.LoadTexture("scenes/UI/Assets/logo.png");
        _banner = UiImageButton.LoadTexture("scenes/UI/Assets/banner.png");
        _btnDeposit = UiImageButton.LoadTexture("scenes/UI/Assets/btn_deposit.png");

        foreach (var game in Games)
        {
            if (!_cardTextures.ContainsKey(game.AssetPath))
            {
                _cardTextures[game.AssetPath] = UiImageButton.LoadTexture(game.AssetPath);
            }
        }
    }

    public MainMenuResult Update()
    {
        float wheel = Raylib.GetMouseWheelMove();
        if (Math.Abs(wheel) > 0.01f)
        {
            _targetScrollY += wheel * 60f;
        }

        int rows = (Games.Count + Columns - 1) / Columns;
        float contentBottom = GridStartY + rows * CardHeight + Math.Max(0, rows - 1) * GapY + 20f;
        float maxScroll = Math.Max(0f, contentBottom - (GameCanvas.Height - FooterHeight));
        _targetScrollY = Math.Clamp(_targetScrollY, -maxScroll, 0f);
        _scrollY += (_targetScrollY - _scrollY) * 0.2f;

        Rectangle depositBounds = new(1080, 14, 170, 44);
        if (UiImageButton.WasClicked(depositBounds))
        {
            return new MainMenuResult(OpenPaywall: true);
        }

        Vector2 mouse = GameCanvas.GetMousePosition();
        if (mouse.Y >= HeaderHeight && mouse.Y <= GameCanvas.Height - FooterHeight)
        {
            for (int i = 0; i < Games.Count; i++)
            {
                Rectangle cardRect = GetCardRect(i);
                if (cardRect.Y + cardRect.Height >= HeaderHeight && cardRect.Y <= GameCanvas.Height - FooterHeight)
                {
                    if (UiImageButton.WasClicked(cardRect))
                    {
                        return new MainMenuResult(Game: Games[i].CreateGame(_currency));
                    }
                }
            }
        }

        return default;
    }

    public void Draw()
    {
        Raylib.ClearBackground(new Color(14, 19, 34, 255));

        for (int x = 0; x < GameCanvas.Width; x += 60)
        {
            Raylib.DrawLine(x, (int)HeaderHeight, x, GameCanvas.Height, new Color(255, 255, 255, 4));
        }

        // Scrollable Hero Banner
        Rectangle bannerRect = new(30, 80 + _scrollY, 1220, 165);
        if (bannerRect.Y + bannerRect.Height > HeaderHeight)
        {
            UiImageButton.Draw(_banner, bannerRect, enabled: false);
        }

        // Scrollable Game Grid
        for (int i = 0; i < Games.Count; i++)
        {
            Rectangle cardRect = GetCardRect(i);
            _cardTextures.TryGetValue(Games[i].AssetPath, out var tex);
            UiImageButton.DrawCard(tex, cardRect);
        }

        // Top Header Bar (always on top)
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, (int)HeaderHeight, new Color(16, 21, 38, 255));
        Raylib.DrawLine(0, (int)HeaderHeight - 1, GameCanvas.Width, (int)HeaderHeight - 1, new Color(255, 180, 0, 120));

        if (_logo.Id != 0)
        {
            Rectangle logoSrc = new(0, 0, _logo.Width, _logo.Height);
            Rectangle logoDest = new(25, 8, 250, 54);
            Raylib.DrawTexturePro(_logo, logoSrc, logoDest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            UiFont.DrawText("BINGO MINUS", 30, 20, 26, Color.Gold, isBold: true);
        }

        Rectangle balanceRect = new(840, 14, 220, 44);
        Raylib.DrawRectangleRec(balanceRect, new Color(24, 32, 54, 255));
        Raylib.DrawRectangleLinesEx(balanceRect, 1, new Color(255, 215, 0, 180));

        Raylib.DrawCircle(865, 36, 13, Color.Gold);
        Raylib.DrawCircleLines(865, 36, 13, Color.White);
        UiFont.DrawText("$", 860, 28, 16, Color.Black, isBold: true);

        UiFont.DrawText("COINS", 888, 17, 11, Color.LightGray, isBold: true);
        UiFont.DrawText($"{_currency.Balance:N2}", 888, 30, 19, Color.Gold, isBold: true);

        UiImageButton.Draw(_btnDeposit, new Rectangle(1080, 14, 170, 44));

        // Bottom Footer Bar
        int footerY = GameCanvas.Height - (int)FooterHeight;
        Raylib.DrawRectangle(0, footerY, GameCanvas.Width, (int)FooterHeight, new Color(10, 13, 22, 255));
        Raylib.DrawLine(0, footerY, GameCanvas.Width, footerY, new Color(255, 255, 255, 15));

        UiFont.DrawText("DEMO CURRENCY ONLY -- NO REAL-MONEY PURCHASES", 25, GameCanvas.Height - 20, 12, new Color(150, 160, 180, 255), isBold: false);

        const string footerRight = "BINGO MINUS CASINO LOBBY";
        int frWidth = (int)UiFont.MeasureText(footerRight, 12, isBold: true).X;
        UiFont.DrawText(footerRight, GameCanvas.Width - frWidth - 25, GameCanvas.Height - 20, 12, new Color(180, 190, 210, 255), isBold: true);
    }

    private Rectangle GetCardRect(int index)
    {
        float startX = (GameCanvas.Width - (Columns * CardWidth + (Columns - 1) * GapX)) / 2f;
        float startY = GridStartY + _scrollY;
        int col = index % Columns;
        int row = index / Columns;
        return new Rectangle(startX + col * (CardWidth + GapX), startY + row * (CardHeight + GapY), CardWidth, CardHeight);
    }
}
