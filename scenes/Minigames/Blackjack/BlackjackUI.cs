using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public class BlackjackUI
{
    private const string AssetDir = "scenes/Minigames/Blackjack/Assets/";
    private const int CardWidth = 71, CardOverlap = 30;

    private static readonly string[] Suits = { "Spades", "Hearts", "Diamonds", "Clubs" };
    private static readonly string[] RankNames = { "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King", "Ace" };
    private static readonly string[] RankCodes = { "02", "03", "04", "05", "06", "07", "08", "09", "10", "J", "Q", "K", "A" };

    private static readonly Color Green = new(35, 103, 83, 255);
    private static readonly Color Red = new(150, 50, 50, 255);
    private static readonly Color Dimmed = new(255, 255, 255, 90);

    private Texture2D _table, _cardBack;
    private readonly Dictionary<int, Texture2D> _chips = new();
    private readonly Dictionary<string, Texture2D> _cards = new(StringComparer.OrdinalIgnoreCase); 

    public readonly int[] ChipValues = { 1, 5, 25, 50, 100, 500, 1000, 5000 };

    public static int CenterX => GameCanvas.Width / 2;
    public static int BtnY => GameCanvas.Height - 70;

    public static Rectangle BtnBack => new(20, 20, 100, 40);
    public static Rectangle BtnDeal => new(CenterX - 105, GameCanvas.Height - 160, 100, 50);
    public static Rectangle BtnClear => new(CenterX + 5, GameCanvas.Height - 160, 100, 50);
    public static Rectangle BtnStand => new(CenterX - 160, BtnY, 100, 50);
    public static Rectangle BtnDouble => new(CenterX - 50, BtnY, 100, 50);
    public static Rectangle BtnHit => new(CenterX + 60, BtnY, 100, 50);
    public static Rectangle BtnSplit => new(CenterX + 170, BtnY, 100, 50);
    public static Rectangle BtnPlayAgain => new(CenterX - 100, BtnY - 20, 200, 50);

    public static Rectangle GetChipBounds(int i, int total) =>
        new(CenterX - total * 30 + i * 60, GameCanvas.Height - 80, 50, 50);


    private static Texture2D Load(string file, bool smooth = false)
    {
        string path = AssetDir + file;
        if (!File.Exists(path)) { Console.WriteLine($"[Blackjack] Missing: {Path.GetFullPath(path)}"); return default; }

        var tex = Raylib.LoadTexture(path);
        if (tex.Id == 0) { Console.WriteLine($"[Blackjack] Failed to decode: {path}"); return default; }

        if (smooth)
        {
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
        }
        return tex;
    }

    public void LoadAssets()
    {
        _table = Load("table.png", smooth: true);
        _cardBack = Load("Cards/card_back.png");

        foreach (int v in ChipValues) _chips[v] = Load($"Chips/chip_{v}.png");

        foreach (string suit in Suits)
            for (int r = 0; r < RankNames.Length; r++)
                _cards[$"{RankNames[r]} of {suit}"] = Load($"Cards/card_{suit.ToLower()}_{RankCodes[r]}.png");
    }

    public void UnloadAssets()
    {
        Raylib.UnloadTexture(_table);
        Raylib.UnloadTexture(_cardBack);
        foreach (var t in _chips.Values) Raylib.UnloadTexture(t);
        foreach (var t in _cards.Values) Raylib.UnloadTexture(t);
        _table = _cardBack = default;
        _chips.Clear();
        _cards.Clear();
    }

    public void DrawBackground()
    {
        int w = GameCanvas.Width, h = GameCanvas.Height;
        if (_table.Id == 0) { Raylib.ClearBackground(new Color(0, 70, 0, 255)); return; }

        float s = MathF.Max((float)w / _table.Width, (float)h / _table.Height);
        float dw = _table.Width * s, dh = _table.Height * s;

        Raylib.DrawTexturePro(_table,
            new Rectangle(0, 0, _table.Width, _table.Height),
            new Rectangle((w - dw) / 2f, (h - dh) / 2f, dw, dh),
            Vector2.Zero, 0f, Color.White);
    }

    public void DrawBalance(decimal balance)
    {
        string text = $"COINS  {balance:0.00}";
        DrawText(text, GameCanvas.Width - 20 - Raylib.MeasureText(text, 22), 28, 22, Color.Gold);
    }

    public void DrawBettingPhase(decimal bet, decimal balance)
    {
        int midY = GameCanvas.Height / 2;
        DrawCentered("Place Your Bet", CenterX, midY - 20, 30, Color.RayWhite);
        DrawCentered($"Current Bet: {bet}", CenterX, midY + 20, 20, Color.Gold);

        if (bet > 0)
        {
            UiButton.Draw(BtnDeal, "DEAL", Green);
            UiButton.Draw(BtnClear, "CLEAR", Red);
        }

        for (int i = 0; i < ChipValues.Length; i++)
        {
            var b = GetChipBounds(i, ChipValues.Length);
            bool affordable = bet + ChipValues[i] <= balance;
            DrawTex(_chips[ChipValues[i]], (int)b.X, (int)b.Y, affordable ? Color.White : Dimmed);
        }
    }

    public void DrawHands(Hand dealer, List<Hand> hands, List<decimal> bets, bool hideDealerCard, int activeHandIndex)
    {
        DrawScoreBubble(CenterX, 80, hideDealerCard ? dealer.Cards[0].GetValue() : dealer.GetScore());
        DrawHandCards(dealer, CenterX, 110, hideDealerCard);

        int firstX = CenterX - (hands.Count - 1) * 75;
        for (int i = 0; i < hands.Count; i++)
        {
            int x = firstX + i * 150;
            bool isActive = (i == activeHandIndex);

            if (isActive && _cardBack.Id != 0) 
            {
                int n = hands[i].Cards.Count;
                int w = CardWidth + (n - 1) * CardOverlap + 12;
                int h = _cardBack.Height + 12;
                Rectangle hlBounds = new Rectangle(x - w / 2, 340 - 6, w, h);
                
                Raylib.DrawRectangleRec(hlBounds, new Color(255, 215, 0, 25)); // Faint transparent fill
                Raylib.DrawRectangleLinesEx(hlBounds, 2, new Color(255, 215, 0, 180)); // Crisp gold border
            }

            DrawScoreBubble(x, 310, hands[i].GetScore(), isActive);
            DrawHandCards(hands[i], x, 340, false);
            DrawCentered($"Bet: {bets[i]}", x, 460, 16, Color.Gold);
            DrawTex(_chips[5], x - 25, 480, Color.White);
        }
    }

    public void DrawPlayerControls(bool canSplit, bool firstMove)
    {
        UiButton.Draw(BtnStand, "STAND", new Color(139, 0, 0, 255));
        UiButton.Draw(BtnHit, "HIT", new Color(34, 139, 34, 255));
        if (!firstMove) return;

        UiButton.Draw(BtnDouble, "DOUBLE", new Color(25, 25, 112, 255));
        if (canSplit) UiButton.Draw(BtnSplit, "SPLIT", new Color(150, 100, 50, 255));
    }

    public void DrawGameOver(string message)
    {
        DrawCentered(message, CenterX, 240, 40, Color.Gold);
        UiButton.Draw(BtnPlayAgain, "PLAY AGAIN", Green);
    }

    private void DrawHandCards(Hand hand, int centerX, int y, bool hideSecond)
    {
        int n = hand.Cards.Count;
        if (n == 0) return;
        
        int x = centerX - (CardWidth + (n - 1) * CardOverlap) / 2;

        for (int i = 0; i < n; i++)
        {
            var tex = (i == 1 && hideSecond) || !_cards.TryGetValue(hand.Cards[i].ToString(), out var t) ? _cardBack : t;
            DrawTex(tex, x + i * CardOverlap, y, Color.White);
        }
    }

    private static void DrawTex(Texture2D tex, int x, int y, Color tint)
    {
        if (tex.Id != 0) Raylib.DrawTexture(tex, x, y, tint);
    }

    private static void DrawScoreBubble(int cx, int y, int score, bool isActive = false)
    {
        Color bgColor = isActive ? new Color(50, 60, 40, 255) : new Color(30, 40, 30, 220);
        Color borderColor = isActive ? Color.Gold : new Color(200, 180, 100, 255);
        
        Raylib.DrawCircle(cx, y, 18, bgColor);
        Raylib.DrawCircleLines(cx, y, 18, borderColor);
        
        string s = score.ToString();
        Raylib.DrawText(s, cx - Raylib.MeasureText(s, 18) / 2, y - 9, 18, Color.RayWhite);
    }

    private static void DrawText(string text, int x, int y, int size, Color color)
    {
        Raylib.DrawText(text, x + 2, y + 2, size, new Color(0, 0, 0, 180));
        Raylib.DrawText(text, x, y, size, color);
    }

    private static void DrawCentered(string text, int cx, int y, int size, Color color) =>
        DrawText(text, cx - Raylib.MeasureText(text, size) / 2, y, size, color);
}