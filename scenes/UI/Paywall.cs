using System;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

// BingoPlus-styled Deposit / Cashier Scene
internal sealed class PaywallScene
{
    private readonly CurrencyManager _currency;
    private string _message = "Select a coin pack to instantly refill your demo balance.";
    private Color _messageColor = Color.LightGray;

    private Texture2D _btnBack;
    private Texture2D _pack100;
    private Texture2D _pack500;
    private Texture2D _pack1000;
    private Texture2D _pack5000;

    public PaywallScene(CurrencyManager currency)
    {
        _currency = currency;
        LoadAssets();
    }

    private void LoadAssets()
    {
        _btnBack = UiImageButton.LoadTexture("scenes/UI/Assets/btn_back.png");
        _pack100 = UiImageButton.LoadTexture("scenes/UI/Assets/pack_100.png");
        _pack500 = UiImageButton.LoadTexture("scenes/UI/Assets/pack_500.png");
        _pack1000 = UiImageButton.LoadTexture("scenes/UI/Assets/pack_1000.png");
        _pack5000 = UiImageButton.LoadTexture("scenes/UI/Assets/pack_5000.png");
    }

    public void Start()
    {
        _message = "Select a coin pack to instantly refill your demo balance.";
        _messageColor = Color.LightGray;
    }

    public bool Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            return true;
        }

        // Back button
        Rectangle backRect = new(30, 14, 140, 44);
        if (UiImageButton.WasClicked(backRect))
        {
            return true;
        }

        // Pack cards bounds (y=240, 4 cards side by side)
        const float packW = 240;
        const float packH = 270;
        const float gap = 30;
        float startX = (GameCanvas.Width - (4 * packW + 3 * gap)) / 2f;
        float packY = 240;

        // Pack 100
        Rectangle r1 = new(startX, packY, packW, packH);
        if (UiImageButton.WasClicked(r1))
        {
            _currency.Add(100m);
            _message = "Success! +100 Demo Coins credited to your wallet!";
            _messageColor = Color.Lime;
        }

        // Pack 500
        Rectangle r2 = new(startX + packW + gap, packY, packW, packH);
        if (UiImageButton.WasClicked(r2))
        {
            _currency.Add(500m);
            _message = "Success! +500 Demo Coins credited to your wallet!";
            _messageColor = Color.Lime;
        }

        // Pack 1000
        Rectangle r3 = new(startX + 2 * (packW + gap), packY, packW, packH);
        if (UiImageButton.WasClicked(r3))
        {
            _currency.Add(1000m);
            _message = "Success! +1,000 Demo Coins credited to your wallet!";
            _messageColor = Color.Lime;
        }

        // Pack 5000
        Rectangle r4 = new(startX + 3 * (packW + gap), packY, packW, packH);
        if (UiImageButton.WasClicked(r4))
        {
            _currency.Add(5000m);
            _message = "VIP Pack! +5,000 Demo Coins credited to your wallet!";
            _messageColor = Color.Gold;
        }

        // Bottom Return Button
        Rectangle bottomReturn = new((GameCanvas.Width - 220) / 2f, 580, 220, 50);
        if (UiImageButton.WasClicked(bottomReturn))
        {
            return true;
        }

        return false;
    }

    public void Draw()
    {
        // Dark Casino Background
        Raylib.ClearBackground(new Color(14, 19, 34, 255));

        // Background subtle grid
        for (int x = 0; x < GameCanvas.Width; x += 60)
        {
            Raylib.DrawLine(x, 70, x, GameCanvas.Height, new Color(255, 255, 255, 4));
        }

        // --- TOP HEADER ---
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, 70, new Color(16, 21, 38, 255));
        Raylib.DrawLine(0, 69, GameCanvas.Width, 69, new Color(255, 180, 0, 120));

        // Back Button
        Rectangle backRect = new(30, 14, 140, 44);
        UiImageButton.Draw(_btnBack, backRect);

        // Header Title
        UiFont.DrawCenteredText("CASHIER & COIN SHOP", 20, 26, Color.White, isBold: true);

        // Wallet Pill
        Rectangle balanceRect = new(GameCanvas.Width - 250, 14, 220, 44);
        Raylib.DrawRectangleRec(balanceRect, new Color(24, 32, 54, 255));
        Raylib.DrawRectangleLinesEx(balanceRect, 1, new Color(255, 215, 0, 180));
        Raylib.DrawCircle((int)balanceRect.X + 25, 36, 13, Color.Gold);
        Raylib.DrawCircleLines((int)balanceRect.X + 25, 36, 13, Color.White);
        UiFont.DrawText("$", (int)balanceRect.X + 20, 27, 16, Color.Black, isBold: true);
        UiFont.DrawText("COINS", (int)balanceRect.X + 48, 17, 11, Color.LightGray, isBold: true);
        UiFont.DrawText($"{_currency.Balance:N2}", (int)balanceRect.X + 48, 30, 18, Color.Gold, isBold: true);

        // Cashier modal frame
        Rectangle modalRect = new(60, 100, GameCanvas.Width - 120, 560);
        Raylib.DrawRectangleRec(modalRect, new Color(20, 27, 46, 240));
        Raylib.DrawRectangleLinesEx(modalRect, 2, new Color(255, 255, 255, 25));

        UiFont.DrawCenteredText("FREE DEMO REFILLS", 125, 26, Color.Gold, isBold: true);
        UiFont.DrawCenteredText("Tap any package to instantly deposit demo credits into your account.", 165, 15, Color.LightGray, isBold: false);

        // Feedback / Success Message
        UiFont.DrawCenteredText(_message, 198, 18, _messageColor, isBold: true);

        // 4 Pack Cards
        const float packW = 240;
        const float packH = 270;
        const float gap = 30;
        float startX = (GameCanvas.Width - (4 * packW + 3 * gap)) / 2f;
        float packY = 240;

        UiImageButton.DrawCard(_pack100, new Rectangle(startX, packY, packW, packH));
        UiImageButton.DrawCard(_pack500, new Rectangle(startX + packW + gap, packY, packW, packH));
        UiImageButton.DrawCard(_pack1000, new Rectangle(startX + 2 * (packW + gap), packY, packW, packH));
        UiImageButton.DrawCard(_pack5000, new Rectangle(startX + 3 * (packW + gap), packY, packW, packH));

        // Return button at bottom
        Rectangle bottomReturn = new((GameCanvas.Width - 220) / 2f, 580, 220, 50);
        UiImageButton.Draw(_btnBack, bottomReturn);

        // Disclaimer
        UiFont.DrawCenteredText("DEMO CURRENCY ONLY -- NO REAL MONEY INVOLVED", GameCanvas.Height - 35, 13, Color.Gray, isBold: false);
    }
}
