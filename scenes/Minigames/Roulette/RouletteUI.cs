// All Roulette drawing + button/chip layout.
//
// ARTWORK: every visual below can be replaced by a PNG in scenes/Minigames/Roulette/assets/ (see ASSETS.md for
// names and sizes). If a file is missing, the same visual is drawn with Raylib shapes instead, so assets can be
// added one at a time. Dynamic things (chip amounts, labels, highlights, the ball path) are always drawn by code.

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class RouletteUI
{
    // --- Layout ---------------------------------------------------------------------------
    public static readonly Vector2 WheelCenter = new(225f, 238f);

    private const float ChipCenterY = 505f;
    private const float ChipRadius = 30f;

    public static readonly Rectangle BtnUndo = new(395, 570, 120, 56);
    public static readonly Rectangle BtnClear = new(525, 570, 120, 56);
    public static readonly Rectangle BtnRebet = new(655, 570, 120, 56);
    public static readonly Rectangle BtnSpin = new(800, 570, 439, 56);

    private static readonly Rectangle InfoPanel = new(60, 405, 320, 230);
    private static readonly Rectangle HistoryPanel = new(395, 398, 844, 40);
    private static readonly Rectangle PayoutPanel = new(830, 448, 409, 104);

    public static Vector2 ChipCenter(int index) => new(431f + index * 72f, ChipCenterY);

    public static Rectangle ChipBounds(int index)
    {
        Vector2 c = ChipCenter(index);
        return new Rectangle(c.X - ChipRadius, c.Y - ChipRadius, ChipRadius * 2, ChipRadius * 2);
    }

    // Wheel ring radii (pixels, for a 300x300 wheel texture drawn 1:1)
    private const float RimR = 150f;
    private const float PocketOuterR = 126f;
    private const float PocketInnerR = 92f;
    private const float NumberR = 109f;
    public const float BallPocketR = 118f;
    public const float BallTrackR = 134f;
    private const float BallDrawSize = 16f;

    private static readonly Color Felt = new(12, 92, 56, 255);
    private static readonly Color PanelBg = new(14, 22, 36, 235);

    // --- Optional artwork -----------------------------------------------------------------
    private const string ArtDir = "scenes/Minigames/Roulette/assets/";

    private static Texture2D _texBg, _texWheelRim, _texWheelDisc, _texBall, _texBoard, _texWinFrame;
    private static Texture2D _texPocketRed, _texPocketBlack, _texPocketGreen;
    private static Texture2D _texPanelInfo, _texPanelHistory, _texPanelPayouts;
    private static Texture2D _texBtnUndo, _texBtnClear, _texBtnRebet, _texBtnSpin;

    public static void LoadArt()
    {
        _texBg = CasinoArt.Load(ArtDir + "bg_roulette.png");
        _texWheelRim = CasinoArt.Load(ArtDir + "wheel_rim.png");
        _texWheelDisc = CasinoArt.Load(ArtDir + "wheel_disc.png");
        _texBall = CasinoArt.Load(ArtDir + "ball.png");
        _texBoard = CasinoArt.Load(ArtDir + "table_board.png");
        _texWinFrame = CasinoArt.Load(ArtDir + "cell_win.png");
        _texPocketRed = CasinoArt.Load(ArtDir + "pocket_red.png");
        _texPocketBlack = CasinoArt.Load(ArtDir + "pocket_black.png");
        _texPocketGreen = CasinoArt.Load(ArtDir + "pocket_green.png");
        _texPanelInfo = CasinoArt.Load(ArtDir + "panel_info.png");
        _texPanelHistory = CasinoArt.Load(ArtDir + "panel_history.png");
        _texPanelPayouts = CasinoArt.Load(ArtDir + "panel_payouts.png");
        _texBtnUndo = CasinoArt.Load(ArtDir + "btn_undo.png");
        _texBtnClear = CasinoArt.Load(ArtDir + "btn_clear.png");
        _texBtnRebet = CasinoArt.Load(ArtDir + "btn_rebet.png");
        _texBtnSpin = CasinoArt.Load(ArtDir + "btn_spin.png");

        CasinoChips.LoadTextures();
        OutOfCoinsModal.LoadArt();
    }

    private static Color PocketColor(int number) => RouletteRules.ColorOf(number) switch
    {
        RouletteColor.Green => new Color(0, 128, 64, 255),
        RouletteColor.Red => new Color(176, 28, 38, 255),
        _ => new Color(22, 22, 28, 255)
    };

    // Round coloured disc behind a number (result badge, history). Number text is drawn by the caller.
    private static void DrawPocketDisc(Vector2 center, float radius, int number)
    {
        Texture2D tex = RouletteRules.ColorOf(number) switch
        {
            RouletteColor.Green => _texPocketGreen,
            RouletteColor.Red => _texPocketRed,
            _ => _texPocketBlack
        };

        if (CasinoArt.Has(tex)) CasinoArt.DrawCentered(tex, center, radius * 2f);
        else Raylib.DrawCircleV(center, radius, PocketColor(number));
    }

    // --- Background -----------------------------------------------------------------------
    public static void DrawBackground()
    {
        if (CasinoArt.Has(_texBg))
        {
            CasinoArt.Draw(_texBg, new Rectangle(0, 0, GameCanvas.Width, GameCanvas.Height));
            return;
        }

        Raylib.DrawRectangleGradientV(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(10, 46, 32, 255), new Color(5, 20, 16, 255));
    }

    // --- Wheel ----------------------------------------------------------------------------
    // wheelAngle / ballAngle are in degrees (0 = 3 o'clock, increasing clockwise on screen).
    public static void DrawWheel(float wheelAngle, float ballAngle, float ballRadius, bool showBall)
    {
        Vector2 c = WheelCenter;
        float step = 360f / RouletteRules.PocketCount;

        // Static rim + ball track
        if (CasinoArt.Has(_texWheelRim))
        {
            CasinoArt.DrawCentered(_texWheelRim, c, RimR * 2f);
        }
        else
        {
            Raylib.DrawCircleV(c, RimR, new Color(92, 56, 30, 255));
            Raylib.DrawCircleV(c, RimR - 4f, new Color(140, 92, 50, 255));
            Raylib.DrawRing(c, RimR - 8f, RimR - 4f, 0f, 360f, 72, new Color(255, 200, 80, 140));
            Raylib.DrawCircleV(c, RimR - 12f, new Color(28, 18, 12, 255));
        }

        // Rotating disc: pockets, spokes, hub
        if (CasinoArt.Has(_texWheelDisc))
        {
            CasinoArt.DrawCentered(_texWheelDisc, c, RimR * 2f, wheelAngle);
        }
        else
        {
            for (int i = 0; i < RouletteRules.PocketCount; i++)
            {
                float start = wheelAngle + i * step;
                Raylib.DrawCircleSector(c, PocketOuterR, start, start + step, 4, PocketColor(RouletteRules.WheelOrder[i]));
            }

            for (int i = 0; i < RouletteRules.PocketCount; i++)
            {
                float a = (wheelAngle + i * step) * MathF.PI / 180f;
                Vector2 dir = new(MathF.Cos(a), MathF.Sin(a));
                Raylib.DrawLineEx(c + dir * PocketInnerR, c + dir * PocketOuterR, 1.5f, new Color(230, 200, 120, 190));
            }

            Raylib.DrawCircleV(c, PocketInnerR, new Color(60, 38, 20, 255));
            Raylib.DrawCircleV(c, PocketInnerR - 8f, new Color(110, 72, 38, 255));
            for (int i = 0; i < 4; i++)
            {
                float a = (wheelAngle + i * 90f) * MathF.PI / 180f;
                Vector2 dir = new(MathF.Cos(a), MathF.Sin(a));
                Raylib.DrawLineEx(c + dir * 18f, c + dir * (PocketInnerR - 10f), 6f, new Color(230, 190, 70, 255));
            }

            Raylib.DrawCircleV(c, 22f, new Color(230, 190, 70, 255));
            Raylib.DrawCircleV(c, 10f, new Color(250, 232, 160, 255));

            for (int i = 0; i < RouletteRules.PocketCount; i++)
            {
                float a = (wheelAngle + (i + 0.5f) * step) * MathF.PI / 180f;
                Vector2 pos = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * NumberR;
                UiFont.DrawCenteredInRect(
                    RouletteRules.WheelOrder[i].ToString(),
                    new Rectangle(pos.X - 12f, pos.Y - 8f, 24f, 16f),
                    11,
                    Color.White,
                    isBold: true,
                    spacing: 0f);
            }
        }

        // Ball
        if (showBall)
        {
            float a = ballAngle * MathF.PI / 180f;
            Vector2 ballPos = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * ballRadius;
            if (CasinoArt.Has(_texBall))
            {
                CasinoArt.DrawCentered(_texBall, ballPos, BallDrawSize);
            }
            else
            {
                Raylib.DrawCircleV(new Vector2(ballPos.X + 1.5f, ballPos.Y + 2f), 6.5f, new Color(0, 0, 0, 110));
                Raylib.DrawCircleV(ballPos, 6.5f, new Color(245, 245, 245, 255));
                Raylib.DrawCircleV(new Vector2(ballPos.X - 2f, ballPos.Y - 2f), 2f, Color.White);
            }
        }
    }

    // --- Table ----------------------------------------------------------------------------
    public static void DrawTable(RouletteBetBook book, RouletteBetSpot? hover, int? winningNumber)
    {
        Rectangle bounds = RouletteTable.TableBounds;
        Rectangle frame = new(bounds.X - 6, bounds.Y - 6, bounds.Width + 12, bounds.Height + 12);

        if (CasinoArt.Has(_texBoard))
        {
            // The whole board (frame, felt, cells, numbers, labels) comes from the texture.
            CasinoArt.Draw(_texBoard, frame);
        }
        else
        {
            DrawTableFallback(bounds, frame);
        }

        // Hover highlight (shows every number the clicked bet would cover)
        if (hover != null)
        {
            foreach (int n in hover.Numbers)
            {
                Raylib.DrawRectangleRec(RouletteTable.CellRect(n), new Color(255, 255, 255, 55));
            }

            foreach (var (rect, spot) in RouletteTable.OutsideSpots)
            {
                if (spot.Key == hover.Key)
                {
                    Raylib.DrawRectangleRec(rect, new Color(255, 255, 255, 55));
                }
            }

            Raylib.DrawRing(hover.Anchor, 13f, 16f, 0f, 360f, 24, Color.Gold);
        }

        // Winning number marker
        if (winningNumber is int win)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * 7f);
            int alpha = (int)(150 + 105 * pulse);
            Rectangle r = RouletteTable.CellRect(win);
            if (CasinoArt.Has(_texWinFrame))
            {
                CasinoArt.Draw(_texWinFrame, r, new Color(255, 255, 255, alpha));
            }
            else
            {
                Raylib.DrawRectangleLinesEx(new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), 4, new Color(255, 215, 0, alpha));
            }
        }

        // Chips already on the table
        foreach (RouletteBet bet in book.Bets)
        {
            bool won = winningNumber is int w && bet.Spot.Covers(w);
            Vector2 anchor = bet.Spot.Anchor;
            CasinoChips.DrawChip(anchor, 15f, bet.Amount, CasinoChips.Format(bet.Amount));
            if (won)
            {
                Raylib.DrawRing(anchor, 16f, 19f, 0f, 360f, 24, Color.Gold);
            }
        }
    }

    private static void DrawTableFallback(Rectangle bounds, Rectangle frame)
    {
        Raylib.DrawRectangleRec(frame, new Color(92, 56, 30, 255));
        Raylib.DrawRectangleRec(bounds, Felt);

        Color line = new(255, 255, 255, 170);

        Rectangle zero = RouletteTable.ZeroRect;
        Raylib.DrawRectangleRec(zero, PocketColor(0));
        Raylib.DrawRectangleLinesEx(zero, 1, line);
        UiFont.DrawCenteredInRect("0", zero, 28, Color.White, isBold: true);

        for (int n = 1; n <= 36; n++)
        {
            Rectangle r = RouletteTable.CellRect(n);
            Raylib.DrawRectangleRec(r, PocketColor(n));
            Raylib.DrawRectangleLinesEx(r, 1, line);
            UiFont.DrawCenteredInRect(n.ToString(), r, 22, Color.White, isBold: true);
        }

        foreach (var (rect, spot) in RouletteTable.OutsideSpots)
        {
            Color fill = spot.Label switch
            {
                "RED" => PocketColor(1),
                "BLACK" => PocketColor(2),
                _ => Felt
            };

            Raylib.DrawRectangleRec(rect, fill);
            Raylib.DrawRectangleLinesEx(rect, 1, line);

            string text = spot.Label.StartsWith("COLUMN") ? "2 to 1" : spot.Label;
            if (text.EndsWith("DOZEN")) text = text.Replace("DOZEN", "12");
            UiFont.DrawCenteredInRect(text, rect, spot.Label.StartsWith("COLUMN") ? 15 : 18, Color.White, isBold: true);
        }
    }

    // --- History strip --------------------------------------------------------------------
    public static void DrawHistory(IReadOnlyList<int> history)
    {
        CasinoArt.DrawPanel(_texPanelHistory, HistoryPanel, PanelBg, new Color(255, 255, 255, 25));
        UiFont.DrawText("HISTORY", 408, 410, 13, Color.LightGray, isBold: true);

        if (history.Count == 0)
        {
            UiFont.DrawText("No spins yet", 500, 411, 13, new Color(150, 160, 180, 255));
            return;
        }

        for (int i = 0; i < history.Count; i++)
        {
            Vector2 center = new(512f + i * 36f, 418f);
            DrawPocketDisc(center, 14f, history[i]);
            Raylib.DrawRing(center, 14f, 15.5f, 0f, 360f, 24, i == 0 ? Color.Gold : new Color(255, 255, 255, 90));
            UiFont.DrawCenteredInRect(history[i].ToString(), new Rectangle(center.X - 14, center.Y - 10, 28, 20), 12, Color.White, isBold: true, spacing: 0f);
        }
    }

    // --- Chip rack ------------------------------------------------------------------------
    public static void DrawChipRack(int selectedIndex, bool enabled)
    {
        UiFont.DrawText("SELECT CHIP", 395, 448, 13, Color.LightGray, isBold: true);

        for (int i = 0; i < CasinoChips.Values.Length; i++)
        {
            Vector2 center = ChipCenter(i);
            bool selected = i == selectedIndex;
            if (selected) center.Y -= 5f;

            CasinoChips.DrawChip(center, ChipRadius, CasinoChips.Values[i], selected: selected && enabled);
        }
    }

    // --- Buttons --------------------------------------------------------------------------
    public static void DrawButtons(bool canEdit, bool canSpin, string spinLabel)
    {
        Color idle = new(51, 65, 85, 255);
        Color off = new(40, 44, 54, 255);

        CasinoArt.DrawButton(_texBtnUndo, BtnUndo, "UNDO", canEdit, canEdit ? idle : off);
        CasinoArt.DrawButton(_texBtnClear, BtnClear, "CLEAR", canEdit, canEdit ? idle : off);
        CasinoArt.DrawButton(_texBtnRebet, BtnRebet, "REBET", canEdit, canEdit ? new Color(30, 90, 150, 255) : off);
        CasinoArt.DrawButton(_texBtnSpin, BtnSpin, spinLabel, canSpin, canSpin ? new Color(35, 103, 83, 255) : off);
    }

    // --- Info panel under the wheel -------------------------------------------------------
    public static void DrawInfoPanel(decimal totalBet, decimal lastWin, int? lastNumber, string message, Color messageColor)
    {
        CasinoArt.DrawPanel(_texPanelInfo, InfoPanel, PanelBg, new Color(255, 180, 0, 90));

        UiFont.DrawText("WINNING NUMBER", 80, 420, 12, Color.LightGray, isBold: true);
        Vector2 badge = new(135f, 486f);
        if (lastNumber is int n)
        {
            DrawPocketDisc(badge, 40f, n);
            Raylib.DrawRing(badge, 40f, 43f, 0f, 360f, 36, Color.Gold);
            UiFont.DrawCenteredInRect(n.ToString(), new Rectangle(badge.X - 40, badge.Y - 28, 80, 56), 38, Color.White, isBold: true);
        }
        else
        {
            Raylib.DrawCircleV(badge, 40f, new Color(30, 38, 56, 255));
            Raylib.DrawRing(badge, 40f, 42f, 0f, 360f, 36, new Color(255, 255, 255, 60));
            UiFont.DrawCenteredInRect("--", new Rectangle(badge.X - 40, badge.Y - 28, 80, 56), 32, Color.Gray, isBold: true);
        }

        UiFont.DrawText("TOTAL BET", 210, 440, 11, Color.LightGray, isBold: true);
        UiFont.DrawText($"{totalBet:N2}", 210, 455, 22, Color.Gold, isBold: true);

        UiFont.DrawText("LAST WIN", 210, 497, 11, Color.LightGray, isBold: true);
        UiFont.DrawText($"{lastWin:N2}", 210, 512, 22, lastWin > 0m ? Color.Lime : Color.Gray, isBold: true);

        UiFont.DrawWrappedText(message, 80, 565, 280, 15, messageColor, isBold: true, lineSpacing: 3f);
    }

    // --- Payout hint + footer -------------------------------------------------------------
    public static void DrawPayoutTable()
    {
        CasinoArt.DrawPanel(_texPanelPayouts, PayoutPanel, PanelBg, new Color(255, 255, 255, 25));
        UiFont.DrawText("PAYOUTS (TO 1)", 844, 456, 12, Color.Gold, isBold: true);

        string[] left = ["Straight (1 number)  35", "Split (2)  17", "Street / Trio (3)  11", "Corner (4)  8"];
        string[] right = ["Six line (6)  5", "Dozen / Column  2", "Red/Black, Odd/Even,", "1-18 / 19-36  1"];
        for (int i = 0; i < 4; i++)
        {
            UiFont.DrawText(left[i], 844, 475 + i * 17, 12, new Color(210, 220, 240, 255));
            UiFont.DrawText(right[i], 1030, 475 + i * 17, 12, new Color(210, 220, 240, 255));
        }
    }

    public static void DrawFooterHint(RouletteBetSpot? hover, bool bettingOpen)
    {
        if (hover != null && bettingOpen)
        {
            UiFont.DrawText($"{hover.Label}  -  pays {hover.PayoutRatio} to 1", 395, 648, 15, Color.Gold, isBold: true);
            return;
        }

        UiFont.DrawText(
            "Left click: place chip   |   Right click: remove bet   |   Click near edges or corners for split, street and corner bets",
            395, 648, 13, new Color(170, 180, 200, 255));
        UiFont.DrawText("Single zero (European). Zero loses all outside bets.", 395, 670, 12, new Color(130, 140, 160, 255));
    }
}
