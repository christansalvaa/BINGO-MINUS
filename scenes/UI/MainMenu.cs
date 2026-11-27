using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal readonly record struct MainMenuResult(IMinigame? Game = null, bool OpenPaywall = false);

internal sealed record GameRegistryItem(
    string CardAssetPath,
    Func<CurrencyManager, IMinigame> CreateGame
);

internal sealed class MainMenuScene
{
    private enum SidebarTab
    {
        Games,
        Funds,
        About
    }

    // Fixed Left Sidebar (1920x1080)
    private const float SidebarWidth = 300f;
    private static readonly Rectangle LogoBoxRect = new(24, 30, 252, 130);
    private static readonly Rectangle BalanceRect = new(24, 180, 252, 74);
    private static readonly Rectangle NavGamesRect = new(36, 286, 228, 68);
    private static readonly Rectangle NavFundsRect = new(36, 372, 228, 68);
    private static readonly Rectangle NavAboutRect = new(36, 458, 228, 68);

    // Right Content Area (X = 300..1920, Width = 1620)
    private const float ContentX = 360f;
    private const float ContentWidth = 1500f;
    private const float BannerTopY = 54f;
    private const float BannerHeight = 360f;
    private const float GridStartY = 460f;
    private const int Columns = 4;
    private const float CardWidth = 345f;
    private const float CardHeight = 415f;
    private const float GapX = 40f;
    private const float GapY = 36f;
    private const float BottomPadding = 48f;

    private readonly CurrencyManager _currency;

    private Texture2D _logo;
    private Texture2D _heroBanner;
    private Texture2D _sidebarBg;
    private Texture2D _rightBg;
    private readonly Dictionary<string, Texture2D> _cardTextures = new(StringComparer.OrdinalIgnoreCase);

    private SidebarTab _activeTab = SidebarTab.Games;
    private string _fundsMessage = "Select a coin pack to instantly refill your demo balance.";
    private Color _fundsMessageColor = Color.LightGray;
    private float _scrollY;
    private float _targetScrollY;

    // Game Registry: add a new minigame in a single line here.
    private static readonly List<GameRegistryItem> Games =
    [
        new("scenes/UI/assets/card_crash.png",      c => new CrashMinigame(c)),
        new("scenes/UI/assets/card_slots.png",      c => new SlotsMinigame(c)),
        new("scenes/UI/assets/card_blackjack.png",  c => new Blackjack(c)),
        new("scenes/UI/assets/card_crossyroad.png", c => new Crossyroad(c)),
        new("scenes/UI/assets/card_mines.png",      c => new MinesMinigame(c)),
        new("scenes/UI/assets/card_roulette.png",   c => new RouletteMinigame(c)),
        new("scenes/UI/assets/card_colorgame.png",  c => new ColorGameMinigame(c)),
        new("scenes/UI/assets/card_baccarat.png",   c => new BaccaratMinigame(c)),
        new("scenes/UI/assets/card_plinko.png",     c => new PlinkoMinigame(c)),
    ];

    public MainMenuScene(CurrencyManager currency)
    {
        _currency = currency;
        LoadAssets();
    }

    private void LoadAssets()
    {
        _logo = UiImageButton.LoadTexture("scenes/UI/assets/logo.png");
        _heroBanner = UiImageButton.LoadTexture("scenes/UI/assets/banner.png");
        _sidebarBg = UiImageButton.LoadTexture("scenes/UI/assets/sidebar.png");
        _rightBg = UiImageButton.LoadTexture("scenes/UI/assets/bg_resizable.png");

        foreach (var game in Games)
        {
            if (!_cardTextures.ContainsKey(game.CardAssetPath))
            {
                _cardTextures[game.CardAssetPath] = UiImageButton.LoadTexture(game.CardAssetPath);
            }
        }
    }

    public void OpenFunds()
    {
        _activeTab = SidebarTab.Funds;
        _fundsMessage = "Select a coin pack to instantly refill your demo balance.";
        _fundsMessageColor = Color.LightGray;
    }

    public MainMenuResult Update()
    {
        // Sidebar Navigation
        if (UiImageButton.WasClicked(NavGamesRect))
        {
            _activeTab = SidebarTab.Games;
            return default;
        }

        if (UiImageButton.WasClicked(NavFundsRect))
        {
            OpenFunds();
            return default;
        }

        if (UiImageButton.WasClicked(NavAboutRect))
        {
            _activeTab = SidebarTab.About;
            return default;
        }

        if (_activeTab == SidebarTab.Funds)
        {
            UpdateFundsContent();
            return default;
        }

        if (_activeTab != SidebarTab.Games)
        {
            return default;
        }

        // Scroll right content area
        float wheel = Raylib.GetMouseWheelMove();
        if (Math.Abs(wheel) > 0.01f)
        {
            _targetScrollY += wheel * 90f;
        }

        int rows = (Games.Count + Columns - 1) / Columns;
        float contentBottom = GridStartY + rows * CardHeight + Math.Max(0, rows - 1) * GapY + BottomPadding;
        float maxScroll = Math.Max(0f, contentBottom - GameCanvas.Height);
        _targetScrollY = Math.Clamp(_targetScrollY, -maxScroll, 0f);
        _scrollY += (_targetScrollY - _scrollY) * 0.2f;

        Vector2 mouse = GameCanvas.GetMousePosition();
        if (mouse.X >= SidebarWidth)
        {
            for (int i = 0; i < Games.Count; i++)
            {
                Rectangle cardRect = GetCardRect(i);
                if (UiImageButton.WasClicked(cardRect))
                {
                    return new MainMenuResult(Game: Games[i].CreateGame(_currency));
                }
            }
        }

        return default;
    }

    public void Draw()
    {
        Raylib.ClearBackground(new Color(14, 19, 34, 255));

        // Right-side resizable background
        Rectangle rightArea = new(SidebarWidth, 0, GameCanvas.Width - SidebarWidth, GameCanvas.Height);
        if (_rightBg.Id != 0)
        {
            var patch = new NPatchInfo
            {
                Source = new Rectangle(0, 0, _rightBg.Width, _rightBg.Height),
                Left = 12,
                Top = 12,
                Right = 12,
                Bottom = 12,
                Layout = NPatchLayout.NinePatch
            };
            Raylib.DrawTextureNPatch(_rightBg, patch, rightArea, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            for (int x = (int)SidebarWidth; x < GameCanvas.Width; x += 90)
            {
                Raylib.DrawLine(x, 0, x, GameCanvas.Height, new Color(255, 255, 255, 4));
            }
        }

        if (_activeTab == SidebarTab.Games)
        {
            DrawGamesContent();
        }
        else if (_activeTab == SidebarTab.Funds)
        {
            DrawFundsContent();
        }
        else
        {
            DrawAboutContent();
        }

        DrawSidebar();
    }

    private void UpdateFundsContent()
    {
        const float packW = 315f;
        const float packH = 355f;
        const float gap = 40f;
        float startX = ContentX + (ContentWidth - (4 * packW + 3 * gap)) / 2f;
        float packY = 390f;

        if (UiImageButton.WasClicked(new Rectangle(startX, packY, packW, packH)))
        {
            _currency.Add(100m);
            _fundsMessage = "Success! +100 Demo Coins credited to your wallet!";
            _fundsMessageColor = Color.Lime;
        }
        else if (UiImageButton.WasClicked(new Rectangle(startX + packW + gap, packY, packW, packH)))
        {
            _currency.Add(500m);
            _fundsMessage = "Success! +500 Demo Coins credited to your wallet!";
            _fundsMessageColor = Color.Lime;
        }
        else if (UiImageButton.WasClicked(new Rectangle(startX + 2 * (packW + gap), packY, packW, packH)))
        {
            _currency.Add(1000m);
            _fundsMessage = "Success! +1,000 Demo Coins credited to your wallet!";
            _fundsMessageColor = Color.Lime;
        }
        else if (UiImageButton.WasClicked(new Rectangle(startX + 3 * (packW + gap), packY, packW, packH)))
        {
            _currency.Add(5000m);
            _fundsMessage = "VIP Pack! +5,000 Demo Coins credited to your wallet!";
            _fundsMessageColor = Color.Gold;
        }
    }

    private void DrawFundsContent()
    {
        Rectangle panel = new(ContentX, 54, ContentWidth, GameCanvas.Height - 108);
        Raylib.DrawRectangleRec(panel, Color.White);
        Raylib.DrawRectangleLinesEx(panel, 2, new Color(210, 214, 220, 255));

        UiFont.DrawCenteredInRect("CASHIER & COIN SHOP", new Rectangle(panel.X, panel.Y + 42, panel.Width, 48), 40, new Color(30, 34, 42, 255), isBold: true);
        UiFont.DrawCenteredInRect("FREE DEMO REFILLS", new Rectangle(panel.X, panel.Y + 104, panel.Width, 38), 32, new Color(220, 102, 38, 255), isBold: true);
        UiFont.DrawCenteredInRect("Tap any package to instantly deposit demo credits into your account.", new Rectangle(panel.X, panel.Y + 154, panel.Width, 30), 22, new Color(90, 95, 105, 255));
        Color msgColor = _fundsMessageColor.R == Color.Lime.R && _fundsMessageColor.G == Color.Lime.G
            ? new Color(22, 163, 74, 255)
            : _fundsMessageColor.R == Color.Gold.R && _fundsMessageColor.G == Color.Gold.G
                ? new Color(220, 102, 38, 255)
                : new Color(90, 95, 105, 255);
        UiFont.DrawCenteredInRect(_fundsMessage, new Rectangle(panel.X, panel.Y + 216, panel.Width, 36), 26, msgColor, isBold: true);

        const float packW = 315f;
        const float packH = 355f;
        const float gap = 40f;
        float startX = ContentX + (ContentWidth - (4 * packW + 3 * gap)) / 2f;
        float packY = 390f;

        DrawPackCard(new Rectangle(startX, packY, packW, packH), "+100", "STARTER PACK");
        DrawPackCard(new Rectangle(startX + packW + gap, packY, packW, packH), "+500", "POPULAR PACK");
        DrawPackCard(new Rectangle(startX + 2 * (packW + gap), packY, packW, packH), "+1,000", "HIGH ROLLER");
        DrawPackCard(new Rectangle(startX + 3 * (packW + gap), packY, packW, packH), "+5,000", "VIP PACK");

        UiFont.DrawCenteredInRect("DEMO CURRENCY ONLY -- NO REAL MONEY INVOLVED", new Rectangle(panel.X, panel.Y + panel.Height - 64, panel.Width, 28), 18, new Color(110, 115, 125, 255));
    }

    private static void DrawPackCard(Rectangle rect, string amount, string subtitle)
    {
        bool hovered = UiImageButton.IsHovered(rect);
        Rectangle drawRect = hovered
            ? new Rectangle(rect.X - 4.5f, rect.Y - 6f, rect.Width + 9f, rect.Height + 12f)
            : rect;
        UiButton.DrawNinePatch(
            drawRect,
            hovered ? new Color(210, 210, 210, 255) : Color.White);

        UiFont.DrawCenteredInRect(amount, new Rectangle(drawRect.X, drawRect.Y + 90, drawRect.Width, 60), 48, Color.White, isBold: true);
        UiFont.DrawCenteredInRect("COINS", new Rectangle(drawRect.X, drawRect.Y + 158, drawRect.Width, 36), 24, new Color(235, 235, 235, 255), isBold: true);
        UiFont.DrawCenteredInRect(subtitle, new Rectangle(drawRect.X, drawRect.Y + 230, drawRect.Width, 32), 20, Color.White, isBold: true);
    }

    private void DrawGamesContent()
    {
        // Single Hero Banner at full brightness
        Rectangle bannerRect = new(ContentX, BannerTopY + _scrollY, ContentWidth, BannerHeight);
        if (bannerRect.Y + bannerRect.Height > 0 && bannerRect.Y < GameCanvas.Height)
        {
            if (_heroBanner.Id != 0)
            {
                Raylib.DrawTexturePro(_heroBanner, new Rectangle(0, 0, _heroBanner.Width, _heroBanner.Height), bannerRect, Vector2.Zero, 0f, Color.White);
            }
            else
            {
                Raylib.DrawRectangleRec(bannerRect, new Color(40, 50, 70, 255));
                Raylib.DrawRectangleLinesEx(bannerRect, 2, Color.Gold);
            }
        }

        // 4-Column Game Grid
        for (int i = 0; i < Games.Count; i++)
        {
            Rectangle cardRect = GetCardRect(i);
            if (cardRect.Y + cardRect.Height > 0 && cardRect.Y < GameCanvas.Height)
            {
                _cardTextures.TryGetValue(Games[i].CardAssetPath, out var tex);
                UiImageButton.DrawCard(tex, cardRect);
            }
        }
    }

    private void DrawAboutContent()
    {
        Rectangle panel = new(ContentX, 54, ContentWidth, GameCanvas.Height - 108);
        Raylib.DrawRectangleRec(panel, Color.White);
        Raylib.DrawRectangleLinesEx(panel, 2, new Color(210, 214, 220, 255));

        UiFont.DrawText("ABOUT BINGO MINUS", panel.X + 60, panel.Y + 54, 42, new Color(220, 102, 38, 255), isBold: true);
        UiFont.DrawText("A satirical casino lobby built with C#, .NET, and Raylib-cs.", panel.X + 60, panel.Y + 114, 24, new Color(90, 95, 105, 255));

        Raylib.DrawLine((int)panel.X + 60, (int)panel.Y + 168, (int)(panel.X + panel.Width - 60), (int)panel.Y + 168, new Color(220, 224, 230, 255));

        UiFont.DrawText("CRASHOUT STUDIOS", panel.X + 60, panel.Y + 196, 28, new Color(30, 34, 42, 255), isBold: true);
        string[] credits =
        [
            "christansalvaa  --  Project Manager",
            "Xclipsey        --  Project Developer",
            "Kennsama        --  Project Developer",
            "bruce           --  Project Developer",
            "rhy             --  Project Developer",
            "TheBiggestPig   --  Project Artist",
            "Proksy24        --  Project Artist / SFX Artist"
        ];

        float y = panel.Y + 250;
        foreach (string line in credits)
        {
            UiFont.DrawText(line, panel.X + 72, y, 22, new Color(55, 60, 70, 255));
            y += 42;
        }

        Rectangle notice = new(panel.X + 60, panel.Y + panel.Height - 120, panel.Width - 120, 64);
        Raylib.DrawRectangleRec(notice, new Color(255, 245, 238, 255));
        Raylib.DrawRectangleLinesEx(notice, 1.5f, new Color(220, 102, 38, 180));
        UiFont.DrawCenteredInRect("DEMO CURRENCY ONLY -- PLAY MONEY ONLY, NO REAL-MONEY GAMBLING.", notice, 20, new Color(200, 82, 24, 255), isBold: true);
    }

    private void DrawSidebar()
    {
        // Sidebar panel background
        if (_sidebarBg.Id != 0)
        {
            Raylib.DrawTexturePro(_sidebarBg, new Rectangle(0, 0, _sidebarBg.Width, _sidebarBg.Height), new Rectangle(0, 0, SidebarWidth, GameCanvas.Height), Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Raylib.DrawRectangle(0, 0, (int)SidebarWidth, GameCanvas.Height, Color.White);
        }
        Raylib.DrawLine((int)SidebarWidth - 1, 0, (int)SidebarWidth - 1, GameCanvas.Height, new Color(210, 214, 220, 255));

        // Logo box
        if (_logo.Id != 0)
        {
            float scale = Math.Min(LogoBoxRect.Width / _logo.Width, LogoBoxRect.Height / _logo.Height);
            float w = _logo.Width * scale;
            float h = _logo.Height * scale;
            Rectangle logoDest = new(
                LogoBoxRect.X + (LogoBoxRect.Width - w) / 2f,
                LogoBoxRect.Y + (LogoBoxRect.Height - h) / 2f,
                w,
                h);
            Raylib.DrawTexturePro(_logo, new Rectangle(0, 0, _logo.Width, _logo.Height), logoDest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            UiFont.DrawCenteredInRect("BINGO MINUS", LogoBoxRect, 30, new Color(220, 102, 38, 255), isBold: true);
        }

        // Coin Balance Pill
        Raylib.DrawRectangleRec(BalanceRect, new Color(248, 248, 250, 255));
        Raylib.DrawRectangleLinesEx(BalanceRect, 1.5f, new Color(220, 102, 38, 200));

        Raylib.DrawCircle((int)BalanceRect.X + 34, (int)(BalanceRect.Y + BalanceRect.Height / 2f), 19f, new Color(220, 102, 38, 255));
        UiFont.DrawText("$", (int)BalanceRect.X + 27, (int)(BalanceRect.Y + BalanceRect.Height / 2f) - 12, 24, Color.White, isBold: true);

        UiFont.DrawText("COINS", BalanceRect.X + 66, BalanceRect.Y + 12, 14, new Color(100, 105, 115, 255), isBold: true);
        UiFont.DrawText($"{_currency.Balance:N2}", BalanceRect.X + 66, BalanceRect.Y + 32, 26, new Color(200, 82, 24, 255), isBold: true);

        // Navigation Buttons
        DrawSidebarButton(NavGamesRect, "Games", _activeTab == SidebarTab.Games);
        DrawSidebarButton(NavFundsRect, "Funds", _activeTab == SidebarTab.Funds);
        DrawSidebarButton(NavAboutRect, "About", _activeTab == SidebarTab.About);

        // Sidebar Footer
        UiFont.DrawCenteredInRect("DEMO CURRENCY ONLY", new Rectangle(16, GameCanvas.Height - 54, SidebarWidth - 32, 24), 14, new Color(70, 80, 100, 255));
        UiFont.DrawCenteredInRect("BINGO MINUS LOBBY", new Rectangle(16, GameCanvas.Height - 32, SidebarWidth - 32, 22), 13, new Color(40, 50, 70, 255), isBold: true);
    }

    private static void DrawSidebarButton(Rectangle rect, string label, bool active)
    {
        bool hovered = UiImageButton.IsHovered(rect);
        Rectangle drawRect = hovered
            ? new Rectangle(rect.X - 3f, rect.Y - 4f, rect.Width + 6f, rect.Height + 8f)
            : rect;
        Color tint = hovered ? new Color(210, 210, 210, 255) : Color.White;

        UiButton.DrawNinePatch(drawRect, tint);

        if (active)
        {
            Raylib.DrawRectangle((int)drawRect.X, (int)drawRect.Y, 6, (int)drawRect.Height, new Color(160, 62, 18, 255));
        }

        UiFont.DrawCenteredInRect(label, drawRect, 24, Color.White, isBold: active);
    }

    private Rectangle GetCardRect(int index)
    {
        float startY = GridStartY + _scrollY;
        int col = index % Columns;
        int row = index / Columns;
        return new Rectangle(ContentX + col * (CardWidth + GapX), startY + row * (CardHeight + GapY), CardWidth, CardHeight);
    }
}
