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
    public static Rectangle BtnBack => new(30, 17, 195, 60);

    // Betting phase buttons
    public static Rectangle BtnEasy => new(CenterX - 248, 663, 150, 48);
    public static Rectangle BtnMedium => new(CenterX - 75, 663, 150, 48);
    public static Rectangle BtnHard => new(CenterX + 98, 663, 150, 48);

    public static Rectangle BtnStart => new(CenterX - 158, 765, 150, 63);
    public static Rectangle BtnClear => new(CenterX + 8, 765, 150, 63);

    // In-game playing buttons
    public static Rectangle BtnStep => new(CenterX - 270, 818, 255, 78);
    public static Rectangle BtnCashOut => new(CenterX + 15, 818, 255, 78);

    // Game over buttons
    public static Rectangle BtnPlayAgain => new(CenterX - 150, 825, 300, 72);

    public static Rectangle GetChipBounds(int i, int total) =>
        new(CenterX - total * 42 + i * 84, 852, 72, 72);

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
        DrawCentered("CROSSY ROAD", CenterX, 33, 42, Color.RayWhite);

        string balText = $"COINS  {balance:0.00}";
        DrawText(balText, GameCanvas.Width - 30 - Raylib.MeasureText(balText, 33), 39, 33, Gold);
    }

    public void DrawRoadGrid(int totalLanes, int currentLane, int crashedLane, CrossyDifficulty difficulty)
    {
        int roadX = 270;
        int roadWidth = 900;
        int laneHeight = 51;
        int gridTopY = 105;
        int totalRows = totalLanes + 2; // Goal (Row 0) + Road Lanes (1..TotalLanes) + Start (Row TotalLanes + 1)

        // Road Outer Shoulders / Curbs
        Raylib.DrawRectangle(roadX - 18, gridTopY - 6, roadWidth + 36, totalRows * laneHeight + 12, new Color(15, 18, 22, 255));
        Raylib.DrawRectangleLines(roadX - 18, gridTopY - 6, roadWidth + 36, totalRows * laneHeight + 12, new Color(40, 48, 60, 255));

        // 1. Goal Sidewalk (Row 0)
        int goalY = gridTopY;
        Raylib.DrawRectangle(roadX, goalY, roadWidth, laneHeight, new Color(30, 95, 55, 255));
        Raylib.DrawRectangleLines(roadX, goalY, roadWidth, laneHeight, new Color(45, 140, 80, 255));
        DrawText("★ FINISH LINE - SAFETY GOAL ★", roadX + 30, goalY + 12, 27, Color.RayWhite);
        string maxMultStr = $"{Crossyroad.GetMultiplier(difficulty, totalLanes):0.00}x JACKPOT";
        DrawText(maxMultStr, roadX + roadWidth - 30 - Raylib.MeasureText(maxMultStr, 27), goalY + 12, 27, Gold);

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
            for (int dashX = roadX + 165; dashX < roadX + roadWidth - 165; dashX += 60)
            {
                Raylib.DrawRectangle(dashX, laneY + laneHeight / 2 - 2, 30, 3, new Color(220, 220, 220, 90));
            }

            // Lane border line
            Raylib.DrawLine(roadX, laneY + laneHeight - 1, roadX + roadWidth, laneY + laneHeight - 1, new Color(25, 28, 35, 255));

            // Lane Number on Left
            string laneName = $"LANE {lane}";
            DrawText(laneName, roadX + 24, laneY + 14, 24, new Color(170, 180, 195, 255));

            // Multiplier Badge on Right
            decimal laneMult = Crossyroad.GetMultiplier(difficulty, lane);
            Rectangle badgeRect = new(roadX + roadWidth - 165, laneY + 8, 144, 36);

            if (lane == crashedLane)
            {
                Raylib.DrawRectangleRec(badgeRect, Red);
                Raylib.DrawRectangleLinesEx(badgeRect, 1.5f, Color.White);
                string bustText = "BUST 💥";
                DrawCentered(bustText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 6, 21, Color.White);
            }
            else if (lane < currentLane)
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(25, 80, 45, 200));
                Raylib.DrawRectangleLinesEx(badgeRect, 1.5f, new Color(50, 160, 90, 255));
                string clearedText = $"{laneMult:0.00}x ✓";
                DrawCentered(clearedText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 6, 21, Color.RayWhite);
            }
            else if (lane == currentLane)
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(130, 100, 15, 220));
                Raylib.DrawRectangleLinesEx(badgeRect, 3, Gold);
                string currText = $"{laneMult:0.00}x";
                DrawCentered(currText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 6, 21, Gold);
            }
            else
            {
                Raylib.DrawRectangleRec(badgeRect, new Color(22, 26, 34, 160));
                Raylib.DrawRectangleLinesEx(badgeRect, 1.5f, new Color(60, 70, 85, 180));
                string upcomingText = $"{laneMult:0.00}x";
                DrawCentered(upcomingText, (int)(badgeRect.X + badgeRect.Width / 2), (int)badgeRect.Y + 6, 21, new Color(160, 170, 180, 200));
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
        DrawText("STARTING PAVEMENT (SAFE)", roadX + 30, startY + 12, 27, new Color(200, 210, 225, 255));
        DrawText("1.00x", roadX + roadWidth - 98, startY + 12, 27, Color.Gray);

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
        Rectangle panel = new(255, 648, 930, 294);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 3, PanelBorder);

        // Difficulty selection buttons
        DrawText("DIFFICULTY:", (int)panel.X + 30, 672, 24, Color.RayWhite);

        UiButton.Draw(BtnEasy, "EASY (4%)", difficulty == CrossyDifficulty.Easy ? Green : new Color(30, 50, 40, 255));
        UiButton.Draw(BtnMedium, "MED (12%)", difficulty == CrossyDifficulty.Medium ? Amber : new Color(60, 45, 25, 255));
        UiButton.Draw(BtnHard, "HARD (20%)", difficulty == CrossyDifficulty.Hard ? Red : new Color(60, 30, 30, 255));

        // Bet display
        string betText = $"CURRENT BET:  {bet:0.00} COINS";
        DrawCentered(betText, CenterX, 726, 30, Gold);

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
                Raylib.DrawTextureEx(tex, new Vector2(b.X, b.Y), 0f, 1.5f, affordable ? Color.White : Dimmed);
            }
            else
            {
                DrawProceduralChip(b, val, affordable);
            }
        }
    }

    public static void DrawPlayingPhase(decimal bet, decimal currentMultiplier, decimal nextMultiplier, int currentLane, int totalLanes)
    {
        Rectangle panel = new(255, 653, 930, 285);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 3, PanelBorder);

        decimal currentPayout = Math.Round(bet * currentMultiplier, 2);
        decimal nextPayout = Math.Round(bet * nextMultiplier, 2);

        // Multiplier & Payout Info
        string multStr = $"CURRENT MULTIPLIER: {currentMultiplier:0.00}x";
        DrawCentered(multStr, CenterX, 678, 36, Gold);

        string cashoutStr = $"CASHOUT VALUE: {currentPayout:0.00} COINS";
        DrawCentered(cashoutStr, CenterX, 723, 27, new Color(80, 220, 120, 255));

        if (currentLane < totalLanes)
        {
            string nextStr = $"Next Step: {nextMultiplier:0.00}x (Value: {nextPayout:0.00} coins)";
            DrawCentered(nextStr, CenterX, 762, 24, new Color(180, 190, 205, 255));
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
        Rectangle panel = new(255, 653, 930, 285);
        Raylib.DrawRectangleRec(panel, PanelBg);
        Raylib.DrawRectangleLinesEx(panel, 3, isWin ? Gold : Red);

        if (isWin)
        {
            DrawCentered("SAFE ON THE OTHER SIDE!", CenterX, 675, 36, Gold);
            DrawCentered(message, CenterX, 723, 27, new Color(80, 220, 120, 255));
        }
        else
        {
            DrawCentered("SPLAT! HIT BY TRAFFIC!", CenterX, 675, 36, Red);
            DrawCentered(message, CenterX, 723, 27, new Color(220, 180, 180, 255));
        }

        string stats = $"Bet: {bet:0.00}  |  Final Multiplier: {finalMultiplier:0.00}x";
        DrawCentered(stats, CenterX, 765, 24, Color.RayWhite);

        UiButton.Draw(BtnPlayAgain, "PLAY AGAIN", Green);
    }

    private void DrawPlayerToken(int x, int y)
    {
        if (_chicken.Id != 0)
        {
            Raylib.DrawTextureEx(_chicken, new Vector2(x - _chicken.Width * 0.75f, y - _chicken.Height * 0.75f), 0f, 1.5f, Color.White);
            return;
        }

        // Procedural Chicken / Character Token
        // Shadow
        Raylib.DrawEllipse(x, y + 15, 21, 9, new Color(0, 0, 0, 120));

        // Chicken Body (Yellow oval)
        Raylib.DrawCircle(x, y, 18, new Color(255, 220, 50, 255));
        Raylib.DrawCircleLines(x, y, 18, new Color(180, 140, 20, 255));

        // Chicken Comb (Red crown)
        Raylib.DrawCircle(x - 4, y - 18, 4.5f, Red);
        Raylib.DrawCircle(x + 3, y - 18, 4.5f, Red);

        // Beak (Orange)
        Raylib.DrawTriangle(
            new Vector2(x + 24, y),
            new Vector2(x + 12, y - 6),
            new Vector2(x + 12, y + 6),
            Amber
        );

        // Eye (White with black pupil)
        Raylib.DrawCircle(x + 7, y - 4, 4.5f, Color.White);
        Raylib.DrawCircle(x + 9, y - 4, 1.5f, Color.Black);
    }

    private static void DrawCrashHazard(int x, int y)
    {
        // Red collision burst
        Raylib.DrawCircle(x, y, 24, new Color(220, 40, 40, 200));
        Raylib.DrawCircleLines(x, y, 27, Color.Yellow);

        // Car chassis shape
        Raylib.DrawRectangle(x - 33, y - 12, 66, 24, new Color(180, 30, 30, 255));
        Raylib.DrawRectangle(x - 21, y - 18, 42, 9, new Color(220, 220, 255, 200)); // Windshield
        Raylib.DrawCircle(x - 21, y + 12, 6, Color.Black); // Wheel 1
        Raylib.DrawCircle(x + 21, y + 12, 6, Color.Black); // Wheel 2

        // Crash text
        DrawCentered("CRASH!", x, y - 39, 21, Color.Yellow);
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
        Raylib.DrawCircleLines(cx, cy, radius - 7.5f, new Color(255, 255, 255, 120));

        string s = val >= 1000 ? $"{val / 1000}k" : val.ToString();
        int fontSize = 21;
        Color textColor = (val == 1 || val == 1000) ? Color.Black : Color.White;
        Raylib.DrawText(s, cx - Raylib.MeasureText(s, fontSize) / 2, cy - fontSize / 2, fontSize, textColor);
    }

    private static void DrawText(string text, int x, int y, int size, Color color)
    {
        Raylib.DrawText(text, x + 3, y + 3, size, new Color(0, 0, 0, 180));
        Raylib.DrawText(text, x, y, size, color);
    }

    private static void DrawCentered(string text, int cx, int y, int size, Color color) =>
        DrawText(text, cx - Raylib.MeasureText(text, size) / 2, y, size, color);
}
