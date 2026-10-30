using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public class CrossyroadUI
{
    private const string AssetDir = "scenes/Minigames/Crossy Road/Assets/";
    private const string SharedChipDir = "scenes/Minigames/Blackjack/Assets/Chips/";

    private static readonly Color BgDark = new(20, 24, 30, 255);
    private static readonly Color PanelBg = new(28, 34, 44, 235);
    private static readonly Color PanelBorder = new(50, 60, 78, 255);

    private static readonly Color Green = new(35, 130, 70, 255);
    private static readonly Color Red = new(160, 45, 45, 255);
    private static readonly Color Amber = new(210, 130, 20, 255);
    private static readonly Color Gold = new(255, 205, 50, 255);
    private static readonly Color Dimmed = new(100, 100, 100, 140);

    private Texture2D _background;
    private Texture2D _chicken;
    private Texture2D _car;
    private readonly Dictionary<int, Texture2D> _chips = [];

    public readonly int[] ChipValues = [1, 5, 25, 50, 100, 500, 1000];

    public static int CenterX => GameCanvas.Width / 2;

    // Top buttons
    public static Rectangle BtnBack => new(20, 20, 100, 40);

    // Betting phase buttons
    public static Rectangle BtnEasy => new(CenterX - 165, 442, 100, 32);
    public static Rectangle BtnMedium => new(CenterX - 50, 442, 100, 32);
    public static Rectangle BtnHard => new(CenterX + 65, 442, 100, 32);

    public static Rectangle BtnStart => new(CenterX - 105, 510, 100, 42);
    public static Rectangle BtnClear => new(CenterX + 5, 510, 100, 42);

    // In-game playing buttons
    public static Rectangle BtnStep => new(CenterX - 180, 545, 170, 52);
    public static Rectangle BtnCashOut => new(CenterX + 10, 545, 170, 52);

    // Game over buttons
    public static Rectangle BtnPlayAgain => new(CenterX - 100, 550, 200, 48);

    public static Rectangle GetChipBounds(int i, int total) =>
        new(CenterX - total * 28 + i * 56, 568, 48, 48);

    private static Texture2D Load(string file, bool smooth = false)
    {
        string primaryPath = AssetDir + file;
        string? path = File.Exists(primaryPath) ? primaryPath : null;

        if (path == null && file.StartsWith("Chips/"))
        {
            string chipFileName = Path.GetFileName(file);
            string sharedPath = SharedChipDir + chipFileName;
            if (File.Exists(sharedPath)) path = sharedPath;
        }

        if (path == null) return default;

        var tex = Raylib.LoadTexture(path);
        if (tex.Id == 0) return default;

        if (smooth)
        {
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
        }
        return tex;
    }

    public void LoadAssets()
    {
        _background = Load("background.png", smooth: true);
        _chicken = Load("chicken.png", smooth: true);
        _car = Load("car.png", smooth: true);

        foreach (int v in ChipValues)
        {
            _chips[v] = Load($"Chips/Chip_{v}.png");
        }
    }

    public void UnloadAssets()
    {
        if (_background.Id != 0) Raylib.UnloadTexture(_background);
        if (_chicken.Id != 0) Raylib.UnloadTexture(_chicken);
        if (_car.Id != 0) Raylib.UnloadTexture(_car);

        foreach (var t in _chips.Values)
        {
            if (t.Id != 0) Raylib.UnloadTexture(t);
        }

        _chips.Clear();
        _background = default;
        _chicken = default;
        _car = default;
    }

    public void DrawBackground()
    {
        int w = GameCanvas.Width, h = GameCanvas.Height;
        if (_background.Id != 0)
        {
            float s = MathF.Max((float)w / _background.Width, (float)h / _background.Height);
            float dw = _background.Width * s, dh = _background.Height * s;
            Raylib.DrawTexturePro(_background,
                new Rectangle(0, 0, _background.Width, _background.Height),
                new Rectangle((w - dw) / 2f, (h - dh) / 2f, dw, dh),
                Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Raylib.ClearBackground(BgDark);
        }
    }

    public static void DrawTopBar(decimal balance)
    {
        UiButton.Draw(BtnBack, "BACK", new Color(150, 50, 50, 255));
        DrawCentered("CROSSY ROAD", CenterX, 22, 28, Color.RayWhite);

        string balText = $"COINS  {balance:0.00}";
        DrawText(balText, GameCanvas.Width - 20 - Raylib.MeasureText(balText, 22), 26, 22, Gold);
    }

    public void DrawRoadGrid(int totalLanes, int currentLane, int crashedLane, CrossyDifficulty difficulty)
    {
        int roadX = 180;
        int roadWidth = 600;
        int laneHeight = 34;
        int gridTopY = 70;
        int totalRows = totalLanes + 2; // Goal (Row 0) + Road Lanes (1..TotalLanes) + Start (Row TotalLanes + 1)

        // Road Outer Shoulders / Curbs
        Raylib.DrawRectangle(roadX - 12, gridTopY - 4, roadWidth + 24, totalRows * laneHeight + 8, new Color(15, 18, 22, 255));
        Raylib.DrawRectangleLines(roadX - 12, gridTopY - 4, roadWidth + 24, totalRows * laneHeight + 8, new Color(40, 48, 60, 255));

        // 1. Goal Sidewalk (Row 0)
        int goalY = gridTopY;
        Raylib.DrawRectangle(roadX, goalY, roadWidth, laneHeight, new Color(30, 95, 55, 255));
        Raylib.DrawRectangleLines(roadX, goalY, roadWidth, laneHeight, new Color(45, 140, 80, 255));
        DrawText("★ FINISH LINE - SAFETY GOAL ★", roadX + 20, goalY + 8, 18, Color.RayWhite);
        string maxMultStr = $"{Crossyroad.GetMultiplier(difficulty, totalLanes):0.00}x JACKPOT";
        DrawText(maxMultStr, roadX + roadWidth - 20 - Raylib.MeasureText(maxMultStr, 18), goalY + 8, 18, Gold);

        // 2. Dangerous Road Lanes (Lanes TotalLanes down to 1)
        for (int lane = totalLanes; lane >= 1; lane--)
        {
            int rowIndex = totalLanes - lane + 1;
            int laneY = gridTopY + rowIndex * laneHeight;

            // Alternating dark asphalt
            Color asphaltColor = (lane % 2 == 0)
                ? new Color(35, 39, 48, 255)
                : new Color(42, 46, 56, 255);

            Raylib.DrawRectangle(roadX, laneY, roadWidth, laneHeight, asphaltColor);

            // Dashed center road stripes
            for (int dashX = roadX + 110; dashX < roadX + roadWidth - 110; dashX += 40)
            {
                Raylib.DrawRectangle(dashX, laneY + laneHeight / 2 - 1, 20, 2, new Color(220, 220, 220, 90));
            }

            // Lane border line
            Raylib.DrawLine(roadX, laneY + laneHeight - 1, roadX + roadWidth, laneY + laneHeight - 1, new Color(25, 28, 35, 255));

            // Lane Number on Left
            string laneName = $"LANE {lane}";
            DrawText(laneName, roadX + 16, laneY + 9, 16, new Color(170, 180, 195, 255));

            // Multiplier Badge on Right
            decimal laneMult = Crossyroad.GetMultiplier(difficulty, lane);
            Rectangle badgeRect = new(roadX + roadWidth - 110, laneY + 5, 96, 24);

            if (lane == crashedLane)
            {
                Raylib.DrawRectangleRec(badgeRect, Red);
                Raylib.DrawRectangleLinesEx(badgeRect, 1, Color.White);
                string bustText = "BUST 💥";
                DrawCentered(bustText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 4, 14, Color.White);
            }
            else if (lane < currentLane)
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(25, 80, 45, 200));
                Raylib.DrawRectangleLinesEx(badgeRect, 1, new Color(50, 160, 90, 255));
                string clearedText = $"{laneMult:0.00}x ✓";
                DrawCentered(clearedText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 4, 14, Color.RayWhite);
            }
            else if (lane == currentLane)
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(130, 100, 15, 220));
                Raylib.DrawRectangleLinesEx(badgeRect, 2, Gold);
                string currText = $"{laneMult:0.00}x";
                DrawCentered(currText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 4, 14, Gold);
            }
            else
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(22, 26, 34, 160));
                Raylib.DrawRectangleLinesEx(badgeRect, 1, new Color(60, 70, 85, 180));
                string upcomingText = $"{laneMult:0.00}x";
                DrawCentered(upcomingText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 4, 14, new Color(160, 170, 180, 200));
            }

            // Hazard crash effect on crashed lane
            if (lane == crashedLane)
            {
                DrawCrashHazard(CenterX, laneY + laneHeight / 2);
            }
        }

        // 3. Start Sidewalk (Row TotalLanes + 1)
        int startY = gridTopY + (totalLanes + 1) * laneHeight;
        Raylib.DrawRectangle(roadX, startY, roadWidth, laneHeight, new Color(55, 62, 72, 255));
        Raylib.DrawRectangleLines(roadX, startY, roadWidth, laneHeight, new Color(75, 85, 100, 255));
        DrawText("STARTING PAVEMENT (SAFE)", roadX + 20, startY + 8, 18, new Color(200, 210, 225, 255));
        DrawText("1.00x", roadX + roadWidth - 65, startY + 8, 18, Color.Gray);

        // 4. Draw Player Character Token
        int playerY;
        if (currentLane == 0)
        {
            playerY = startY + laneHeight / 2;
        }
        else if (currentLane >= 1 && currentLane <= totalLanes)
        {
            int row = totalLanes - currentLane + 1;
            playerY = gridTopY + row * laneHeight + laneHeight / 2;
        }
        else
        {
            playerY = goalY + laneHeight / 2;
        }

        // Only draw player marker if not crashed at this exact frame
        if (crashedLane != currentLane || currentLane == 0)
        {
            DrawPlayerToken(CenterX, playerY);
        }
    }

    public void DrawBettingPhase(decimal bet, decimal balance, CrossyDifficulty difficulty)
    {
        Rectangle panel = new(170, 432, 620, 196);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 2, PanelBorder);

        // Difficulty selection buttons
        DrawText("DIFFICULTY:", (int)panel.X + 20, 448, 16, Color.RayWhite);

        UiButton.Draw(BtnEasy, "EASY (4%)", difficulty == CrossyDifficulty.Easy ? Green : new Color(30, 50, 40, 255));
        UiButton.Draw(BtnMedium, "MED (12%)", difficulty == CrossyDifficulty.Medium ? Amber : new Color(60, 45, 25, 255));
        UiButton.Draw(BtnHard, "HARD (20%)", difficulty == CrossyDifficulty.Hard ? Red : new Color(60, 30, 30, 255));

        // Bet display
        string betText = $"CURRENT BET:  {bet:0.00} COINS";
        DrawCentered(betText, CenterX, 484, 20, Gold);

        // Bet action buttons
        if (bet > 0)
        {
            UiButton.Draw(BtnStart, "CROSS!", Green);
            UiButton.Draw(BtnClear, "CLEAR", Red);
        }
        else
        {
            UiButton.Draw(BtnStart, "CROSS!", Dimmed);
            UiButton.Draw(BtnClear, "CLEAR", Dimmed);
        }

        // Chips
        for (int i = 0; i < ChipValues.Length; i++)
        {
            int val = ChipValues[i];
            var b = GetChipBounds(i, ChipValues.Length);
            bool affordable = bet + val <= balance;

            if (_chips.TryGetValue(val, out var tex) && tex.Id != 0)
            {
                Raylib.DrawTexture(tex, (int)b.X, (int)b.Y, affordable ? Color.White : Dimmed);
            }
            else
            {
                DrawProceduralChip(b, val, affordable);
            }
        }
    }

    public static void DrawPlayingPhase(decimal bet, decimal currentMultiplier, decimal nextMultiplier, int currentLane, int totalLanes)
    {
        Rectangle panel = new(170, 435, 620, 190);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 2, PanelBorder);

        decimal currentPayout = Math.Round(bet * currentMultiplier, 2);
        decimal nextPayout = Math.Round(bet * nextMultiplier, 2);

        // Multiplier & Payout Info
        string multStr = $"CURRENT MULTIPLIER: {currentMultiplier:0.00}x";
        DrawCentered(multStr, CenterX, 452, 24, Gold);

        string cashoutStr = $"CASHOUT VALUE: {currentPayout:0.00} COINS";
        DrawCentered(cashoutStr, CenterX, 482, 18, new Color(80, 220, 120, 255));

        if (currentLane < totalLanes)
        {
            string nextStr = $"Next Step: {nextMultiplier:0.00}x (Value: {nextPayout:0.00} coins)";
            DrawCentered(nextStr, CenterX, 508, 16, new Color(180, 190, 205, 255));
        }

        // Action Buttons
        UiButton.Draw(BtnStep, "STEP FORWARD", Green);

        if (currentLane > 0)
        {
            UiButton.Draw(BtnCashOut, $"CASH OUT ({currentPayout:0.00})", Amber);
        }
        else
        {
            UiButton.Draw(BtnCashOut, "CASH OUT", Dimmed);
        }
    }

    public static void DrawGameOverPhase(string message, bool isWin, decimal bet, decimal finalMultiplier)
    {
        Rectangle panel = new(170, 435, 620, 190);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 2, isWin ? Gold : Red);

        if (isWin)
        {
            DrawCentered("SAFE ON THE OTHER SIDE!", CenterX, 450, 24, Gold);
            DrawCentered(message, CenterX, 482, 18, new Color(80, 220, 120, 255));
        }
        else
        {
            DrawCentered("SPLAT! HIT BY TRAFFIC!", CenterX, 450, 24, Red);
            DrawCentered(message, CenterX, 482, 18, new Color(220, 180, 180, 255));
        }

        string stats = $"Bet: {bet:0.00}  |  Final Multiplier: {finalMultiplier:0.00}x";
        DrawCentered(stats, CenterX, 510, 16, Color.RayWhite);

        UiButton.Draw(BtnPlayAgain, "PLAY AGAIN", Green);
    }

    private void DrawPlayerToken(int x, int y)
    {
        if (_chicken.Id != 0)
        {
            Raylib.DrawTexture(_chicken, x - _chicken.Width / 2, y - _chicken.Height / 2, Color.White);
            return;
        }

        // Procedural Chicken / Character Token
        // Shadow
        Raylib.DrawEllipse(x, y + 10, 14, 6, new Color(0, 0, 0, 120));

        // Chicken Body (Yellow oval)
        Raylib.DrawCircle(x, y, 12, new Color(255, 220, 50, 255));
        Raylib.DrawCircleLines(x, y, 12, new Color(180, 140, 20, 255));

        // Chicken Comb (Red crown)
        Raylib.DrawCircle(x - 3, y - 12, 3, Red);
        Raylib.DrawCircle(x + 2, y - 12, 3, Red);

        // Beak (Orange)
        Raylib.DrawTriangle(
            new Vector2(x + 16, y),
            new Vector2(x + 8, y - 4),
            new Vector2(x + 8, y + 4),
            Amber
        );

        // Eye (White with black pupil)
        Raylib.DrawCircle(x + 5, y - 3, 3, Color.White);
        Raylib.DrawCircle(x + 6, y - 3, 1, Color.Black);
    }

    private static void DrawCrashHazard(int x, int y)
    {
        // Red collision burst
        Raylib.DrawCircle(x, y, 16, new Color(220, 40, 40, 200));
        Raylib.DrawCircleLines(x, y, 18, Color.Yellow);

        // Car chassis shape
        Raylib.DrawRectangle(x - 22, y - 8, 44, 16, new Color(180, 30, 30, 255));
        Raylib.DrawRectangle(x - 14, y - 12, 28, 6, new Color(220, 220, 255, 200)); // Windshield
        Raylib.DrawCircle(x - 14, y + 8, 4, Color.Black); // Wheel 1
        Raylib.DrawCircle(x + 14, y + 8, 4, Color.Black); // Wheel 2

        // Crash text
        DrawCentered("CRASH!", x, y - 26, 14, Color.Yellow);
    }

    private static void DrawProceduralChip(Rectangle b, int val, bool affordable)
    {
        int cx = (int)(b.X + b.Width / 2);
        int cy = (int)(b.Y + b.Height / 2);
        int radius = (int)(b.Width / 2);

        Color mainColor = val switch
        {
            1 => new Color(200, 200, 200, 255),
            5 => Red,
            25 => Green,
            50 => new Color(40, 100, 180, 255),
            100 => Color.Black,
            500 => new Color(128, 0, 128, 255),
            1000 => Gold,
            _ => Amber
        };

        if (!affordable)
        {
            mainColor = new Color(mainColor.R, mainColor.G, mainColor.B, (byte)90);
        }

        Raylib.DrawCircle(cx, cy, radius, mainColor);
        Raylib.DrawCircleLines(cx, cy, radius, Color.White);
        Raylib.DrawCircleLines(cx, cy, radius - 5, new Color(255, 255, 255, 120));

        string s = val >= 1000 ? $"{val / 1000}k" : val.ToString();
        int fontSize = 14;
        Color textColor = (val == 1 || val == 1000) ? Color.Black : Color.White;
        Raylib.DrawText(s, cx - Raylib.MeasureText(s, fontSize) / 2, cy - fontSize / 2, fontSize, textColor);
    }

    private static void DrawText(string text, int x, int y, int size, Color color)
    {
        Raylib.DrawText(text, x + 2, y + 2, size, new Color(0, 0, 0, 180));
        Raylib.DrawText(text, x, y, size, color);
    }

    private static void DrawCentered(string text, int cx, int y, int size, Color color) =>
        DrawText(text, cx - Raylib.MeasureText(text, size) / 2, y, size, color);
}
