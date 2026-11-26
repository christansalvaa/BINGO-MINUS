using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal enum MainMenuAction
{
    None,
    PlayCrash,
    PlaySlots,
    OpenPaywall,
    PlayBlackjack,
    PlayCrossroads,
    PlayPlinko,
    PlayBaccarat
}

internal sealed record GameRegistryItem(
    MainMenuAction Action,
    string Title,
    string Category,
    string AssetPath,
    string Badge,
    string Subtitle
);

internal sealed class MainMenuScene
{
    private readonly CurrencyManager _currency;

    // Assets
    private Texture2D _logo;
    private Texture2D _banner;
    private Texture2D _btnDeposit;
    private readonly Dictionary<string, Texture2D> _cardTextures = new(StringComparer.OrdinalIgnoreCase);

    // Categories
    private static readonly string[] Categories = ["ALL GAMES", "HOT", "SLOTS", "TABLE", "ARCADE"];
    private int _selectedCategoryIndex = 0;

    // Scrolling
    private float _scrollY = 0f;
    private float _targetScrollY = 0f;

    // Game Registry: easy to add new minigames by adding a single line here!
    private static readonly List<GameRegistryItem> Games =
    [
        new(
            MainMenuAction.PlayCrash,
            "AVIATOR CRASH",
            "ARCADE",
            "scenes/UI/Assets/card_crash.png",
            "HOT",
            "Aviator Multiplier"
        ),
        new(
            MainMenuAction.PlaySlots,
            "MEGA SLOTS",
            "SLOTS",
            "scenes/UI/Assets/card_slots.png",
            "JACKPOT",
            "Spin 777 Reels"
        ),
        new(
            MainMenuAction.PlayBlackjack,
            "BLACKJACK 21",
            "TABLE",
            "scenes/UI/Assets/card_blackjack.png",
            "TOP",
            "Beat the Dealer"
        ),
        new(
            MainMenuAction.PlayCrossroads,
            "CROSSROADS",
            "ARCADE",
            "scenes/UI/Assets/card_crossyroad.png",
            "NEW",
            "Multiplier Highway"
        ),
        new(
            MainMenuAction.PlayPlinko,
            "PLINKO",
            "ARCADE",
            "scenes/UI/Assets/card_plinko.png",
            "HOT",
            "Drop & Win 1000x"
        ),
        new(
            MainMenuAction.PlayBaccarat,
            "BACCARAT",
            "TABLE",
            "scenes/UI/Assets/card_baccarat.png",
            "VIP",
            "Player vs Banker"
        ),
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

    public MainMenuAction Update()
    {
        // 1. Mouse wheel & keyboard scrolling
        float wheel = Raylib.GetMouseWheelMove();
        if (Math.Abs(wheel) > 0.01f)
        {
            _targetScrollY += wheel * 75f;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _targetScrollY -= 180f;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _targetScrollY += 180f;
        }

        // Clamp scroll to exact content height
        var visibleGamesForScroll = GetFilteredGames();
        int totalRows = (visibleGamesForScroll.Count + 3) / 4;
        float contentHeight = totalRows * 325f + Math.Max(0, totalRows - 1) * 24f;
        float visibleHeight = GameCanvas.Height - 28f - 310f - 16f;
        float maxScroll = Math.Max(0f, contentHeight - visibleHeight);
        _targetScrollY = Math.Clamp(_targetScrollY, -maxScroll, 0f);
        _scrollY += (_targetScrollY - _scrollY) * 0.2f;

        // 2. Deposit / Get Coins button
        Rectangle depositBounds = new(1080, 14, 170, 44);
        if (UiImageButton.WasClicked(depositBounds))
        {
            return MainMenuAction.OpenPaywall;
        }

        // 3. Category tabs
        float tabX = 40;
        float tabY = 262;
        for (int i = 0; i < Categories.Length; i++)
        {
            int textWidth = (int)UiFont.MeasureText(Categories[i], 15, isBold: true).X;
            Rectangle tabRect = new(tabX, tabY, textWidth + 24, 32);
            if (UiImageButton.WasClicked(tabRect))
            {
                _selectedCategoryIndex = i;
                _targetScrollY = 0f;
                _scrollY = 0f;
            }
            tabX += textWidth + 34;
        }

        // 4. Clickable Game Cards
        var visibleGames = GetFilteredGames();
        const int cols = 4;
        const float cardW = 270;
        const float cardH = 325;
        const float gapX = 26;
        const float gapY = 24;
        float startX = (GameCanvas.Width - (cols * cardW + (cols - 1) * gapX)) / 2f;
        float startY = 310 + _scrollY;

        Vector2 mousePos = GameCanvas.GetMousePosition();
        bool mouseInCardViewport = mousePos.Y >= 300f && mousePos.Y <= GameCanvas.Height - 28f;

        for (int i = 0; i < visibleGames.Count; i++)
        {
            var game = visibleGames[i];
            int col = i % cols;
            int row = i / cols;

            float x = startX + col * (cardW + gapX);
            float y = startY + row * (cardH + gapY);
            Rectangle cardRect = new(x, y, cardW, cardH);

            // Check if card is visible within the scrollable grid viewport
            if (mouseInCardViewport && y + cardH >= 300f && y <= GameCanvas.Height - 28f)
            {
                if (UiImageButton.WasClicked(cardRect))
                {
                    return game.Action;
                }
            }
        }

        return MainMenuAction.None;
    }

    private List<GameRegistryItem> GetFilteredGames()
    {
        string selectedCat = Categories[_selectedCategoryIndex];
        if (selectedCat == "ALL GAMES") return Games;
        if (selectedCat == "HOT") return Games.FindAll(g => g.Badge == "HOT" || g.Badge == "TOP" || g.Badge == "VIP");
        return Games.FindAll(g => g.Category.Equals(selectedCat, StringComparison.OrdinalIgnoreCase));
    }

    public void Draw()
    {
        // Background
        Raylib.ClearBackground(new Color(14, 19, 34, 255));

        // Background subtle grid/mesh pattern
        for (int x = 0; x < GameCanvas.Width; x += 60)
        {
            Raylib.DrawLine(x, 70, x, GameCanvas.Height, new Color(255, 255, 255, 4));
        }

        // --- SCROLLABLE CONTENT (Game Grid) ---
        var visibleGames = GetFilteredGames();
        const int cols = 4;
        const float cardW = 270;
        const float cardH = 325;
        const float gapX = 26;
        const float gapY = 24;
        float startX = (GameCanvas.Width - (cols * cardW + (cols - 1) * gapX)) / 2f;
        float startY = 310 + _scrollY;

        for (int i = 0; i < visibleGames.Count; i++)
        {
            var game = visibleGames[i];
            int col = i % cols;
            int row = i / cols;

            float x = startX + col * (cardW + gapX);
            float y = startY + row * (cardH + gapY);
            Rectangle cardRect = new(x, y, cardW, cardH);

            if (_cardTextures.TryGetValue(game.AssetPath, out var tex))
            {
                UiImageButton.DrawCard(tex, cardRect);
            }
            else
            {
                UiImageButton.DrawCard(default, cardRect);
            }
        }

        // Subtle scrollbar indicator when lobby has multiple rows
        int totalRowsDraw = (visibleGames.Count + cols - 1) / cols;
        float contentHeightDraw = totalRowsDraw * cardH + Math.Max(0, totalRowsDraw - 1) * gapY;
        float viewportHeightDraw = GameCanvas.Height - 28f - 310f - 16f;
        float maxScrollDraw = Math.Max(0f, contentHeightDraw - viewportHeightDraw);
        if (maxScrollDraw > 1f)
        {
            float trackY = 308f;
            float trackH = GameCanvas.Height - 28f - trackY - 10f;
            Raylib.DrawRectangleRounded(new Rectangle(GameCanvas.Width - 14, trackY, 6, trackH), 0.5f, 4, new Color(255, 255, 255, 20));
            float thumbH = Math.Max(42f, trackH * (viewportHeightDraw / contentHeightDraw));
            float scrollRatio = Math.Clamp(-_scrollY / maxScrollDraw, 0f, 1f);
            float thumbY = trackY + scrollRatio * (trackH - thumbH);
            Raylib.DrawRectangleRounded(new Rectangle(GameCanvas.Width - 14, thumbY, 6, thumbH), 0.5f, 4, new Color(255, 195, 0, 180));
        }

        // --- HERO BANNER & CATEGORY BAR (Pinned under Header) ---
        // Solid backdrop behind hero banner so scrolled cards tuck cleanly beneath it
        Raylib.DrawRectangle(0, 70, GameCanvas.Width, 185, new Color(14, 19, 34, 255));
        // Top Hero Banner
        Rectangle bannerRect = new(30, 80, 1220, 165);
        UiImageButton.Draw(_banner, bannerRect, enabled: false);

        // Category Bar Background
        Raylib.DrawRectangle(0, 255, GameCanvas.Width, 44, new Color(18, 24, 40, 240));
        Raylib.DrawLine(0, 299, GameCanvas.Width, 299, new Color(255, 255, 255, 18));

        // Category Tabs
        float tabX = 40;
        float tabY = 262;
        for (int i = 0; i < Categories.Length; i++)
        {
            bool isSelected = i == _selectedCategoryIndex;
            string text = Categories[i];
            int textWidth = (int)UiFont.MeasureText(text, 15, isBold: isSelected).X;
            Rectangle tabRect = new(tabX, tabY, textWidth + 24, 30);

            bool hovered = UiImageButton.IsHovered(tabRect);
            if (isSelected)
            {
                Raylib.DrawRectangleRec(tabRect, new Color(255, 140, 0, 220));
                Raylib.DrawRectangleLinesEx(tabRect, 1, Color.Gold);
                UiFont.DrawCenteredInRect(text, tabRect, 15, Color.White, isBold: true);
            }
            else
            {
                if (hovered)
                {
                    Raylib.DrawRectangleRec(tabRect, new Color(255, 255, 255, 20));
                }
                UiFont.DrawCenteredInRect(text, tabRect, 15, hovered ? Color.White : Color.LightGray, isBold: false);
            }

            tabX += textWidth + 34;
        }

        // --- TOP HEADER BAR (Always on top) ---
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, 70, new Color(16, 21, 38, 255));
        Raylib.DrawLine(0, 69, GameCanvas.Width, 69, new Color(255, 180, 0, 120));

        // Logo
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

        // Live Coin Balance Container Pill
        Rectangle balanceRect = new(840, 14, 220, 44);
        Raylib.DrawRectangleRec(balanceRect, new Color(24, 32, 54, 255));
        Raylib.DrawRectangleLinesEx(balanceRect, 1, new Color(255, 215, 0, 180));

        // Gold coin symbol + text
        Raylib.DrawCircle(865, 36, 13, Color.Gold);
        Raylib.DrawCircleLines(865, 36, 13, Color.White);
        UiFont.DrawText("$", 860, 28, 16, Color.Black, isBold: true);

        string balanceStr = $"{_currency.Balance:N2}";
        UiFont.DrawText("COINS", 888, 17, 11, Color.LightGray, isBold: true);
        UiFont.DrawText(balanceStr, 888, 30, 19, Color.Gold, isBold: true);

        // "GET COINS" Button (PNG)
        Rectangle depRect = new(1080, 14, 170, 44);
        UiImageButton.Draw(_btnDeposit, depRect);

        // --- BOTTOM FOOTER BAR ---
        Raylib.DrawRectangle(0, GameCanvas.Height - 28, GameCanvas.Width, 28, new Color(10, 13, 22, 255));
        Raylib.DrawLine(0, GameCanvas.Height - 28, GameCanvas.Width, GameCanvas.Height - 28, new Color(255, 255, 255, 15));

        string footerLeft = "DEMO CURRENCY ONLY -- NO REAL-MONEY PURCHASES";
        UiFont.DrawText(footerLeft, 25, GameCanvas.Height - 20, 12, new Color(150, 160, 180, 255), isBold: false);

        string footerRight = "BINGO MINUS CASINO LOBBY";
        int frWidth = (int)UiFont.MeasureText(footerRight, 12, isBold: true).X;
        UiFont.DrawText(footerRight, GameCanvas.Width - frWidth - 25, GameCanvas.Height - 20, 12, new Color(180, 190, 210, 255), isBold: true);
    }
}
