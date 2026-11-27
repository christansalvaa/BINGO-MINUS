// All Color Game drawing + button/tile/chip layout.
//
// ARTWORK: every visual below can be replaced by a PNG in scenes/Minigames/ColorGame/assets/ (see ASSETS.md for
// names and sizes). If a file is missing, the same visual is drawn with Raylib shapes instead, so assets can be
// added one at a time. Dynamic things (chip amounts, x1/x2/x3 values, labels, highlights) are always drawn by code.

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class ColorGameUI
{
    // --- Layout ---------------------------------------------------------------------------
    private const float DiceSize = 150f;
    private const float ChipCenterY = 530f;
    private const float ChipRadius = 28f;

    public static readonly Rectangle BtnUndo = new(65, 580, 130, 56);
    public static readonly Rectangle BtnClear = new(205, 580, 130, 56);
    public static readonly Rectangle BtnRebet = new(345, 580, 130, 56);
    public static readonly Rectangle BtnRoll = new(495, 580, 720, 56);

    public static Rectangle DieRect(int index) => new(365f + index * 200f, 95f, DiceSize, DiceSize);

    public static Rectangle TileBounds(int colorIndex) => new(65f + colorIndex * 194f, 322f, 180f, 148f);

    public static Vector2 ChipCenter(int index) => new(95f + index * 68f, ChipCenterY);

    public static Rectangle ChipBounds(int index)
    {
        Vector2 c = ChipCenter(index);
        return new Rectangle(c.X - ChipRadius, c.Y - ChipRadius, ChipRadius * 2, ChipRadius * 2);
    }

    private static readonly Color PanelBg = new(14, 22, 36, 235);

    public static Color ColorOf(DiceColor color) => color switch
    {
        DiceColor.Yellow => new Color(250, 204, 21, 255),
        DiceColor.White => new Color(240, 240, 245, 255),
        DiceColor.Pink => new Color(244, 114, 182, 255),
        DiceColor.Blue => new Color(59, 130, 246, 255),
        DiceColor.Red => new Color(239, 68, 68, 255),
        _ => new Color(34, 197, 94, 255)
    };

    // Dark text on light colours, white text on dark ones.
    private static Color TextOn(DiceColor color) =>
        color is DiceColor.Yellow or DiceColor.White or DiceColor.Pink ? new Color(30, 30, 40, 255) : Color.White;

    // --- Optional artwork -----------------------------------------------------------------
    private const string ArtDir = "scenes/Minigames/ColorGame/assets/";

    private static Texture2D _texBg, _texMatchBadge, _texPanelValue, _texPanelRolls;
    private static Texture2D _texBtnUndo, _texBtnClear, _texBtnRebet, _texBtnRoll;
    private static readonly Texture2D[] DieTextures = new Texture2D[ColorGameRules.All.Length];
    private static readonly Texture2D[] TileTextures = new Texture2D[ColorGameRules.All.Length];

    public static void LoadArt()
    {
        _texBg = CasinoArt.Load(ArtDir + "bg_colorgame.png");
        _texMatchBadge = CasinoArt.Load(ArtDir + "match_badge.png");
        _texPanelValue = CasinoArt.Load(ArtDir + "panel_value.png");
        _texPanelRolls = CasinoArt.Load(ArtDir + "panel_rolls.png");
        _texBtnUndo = CasinoArt.Load(ArtDir + "btn_undo.png");
        _texBtnClear = CasinoArt.Load(ArtDir + "btn_clear.png");
        _texBtnRebet = CasinoArt.Load(ArtDir + "btn_rebet.png");
        _texBtnRoll = CasinoArt.Load(ArtDir + "btn_roll.png");

        foreach (DiceColor color in ColorGameRules.All)
        {
            string name = color.ToString().ToLowerInvariant();
            DieTextures[(int)color] = CasinoArt.Load($"{ArtDir}die_{name}.png");
            TileTextures[(int)color] = CasinoArt.Load($"{ArtDir}tile_{name}.png");
        }

        CasinoChips.LoadTextures();
        OutOfCoinsModal.LoadArt();
    }

    // --- Background -----------------------------------------------------------------------
    public static void DrawBackground()
    {
        if (CasinoArt.Has(_texBg))
        {
            CasinoArt.Draw(_texBg, new Rectangle(0, 0, GameCanvas.Width, GameCanvas.Height));
            return;
        }

        Raylib.DrawRectangleGradientV(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(40, 14, 52, 255), new Color(14, 8, 28, 255));

        // Carnival-style bunting along the top
        for (int x = 0; x < GameCanvas.Width; x += 64)
        {
            DiceColor c = ColorGameRules.All[(x / 64) % ColorGameRules.All.Length];
            Color col = ColorOf(c);
            col.A = 110;
            Raylib.DrawTriangle(new Vector2(x, 62), new Vector2(x + 32, 84), new Vector2(x + 64, 62), col);
        }
    }

    // --- Dice -----------------------------------------------------------------------------
    // shake: 0..1 (how much the die is still tumbling). time: animation clock in seconds.
    public static void DrawDie(int index, DiceColor face, float shake, float time)
    {
        Rectangle home = DieRect(index);
        Rectangle r = home;
        r.X += MathF.Sin(time * 31f + index * 2.3f) * 6f * shake;
        r.Y += MathF.Abs(MathF.Sin(time * 17f + index * 1.7f)) * -16f * shake;

        // Shadow stays on the table while the die hops
        Raylib.DrawRectangleRounded(new Rectangle(home.X + 6, home.Y + 12, DiceSize, DiceSize), 0.18f, 8, new Color(0, 0, 0, 90));

        Texture2D art = DieTextures[(int)face];
        if (CasinoArt.Has(art))
        {
            // The die face (colour, name/symbol) comes entirely from the texture.
            CasinoArt.Draw(art, r);
            return;
        }

        Raylib.DrawRectangleRounded(new Rectangle(r.X - 4, r.Y - 4, r.Width + 8, r.Height + 8), 0.18f, 8, new Color(255, 255, 255, 230));
        Raylib.DrawRectangleRounded(r, 0.18f, 8, ColorOf(face));

        // Glossy highlight + inner bevel
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 12, r.Y + 10, r.Width - 24, 26), 0.5f, 8, new Color(255, 255, 255, 60));
        Raylib.DrawRectangleLinesEx(new Rectangle(r.X + 14, r.Y + 14, r.Width - 28, r.Height - 28), 3, new Color(0, 0, 0, 45));

        UiFont.DrawCenteredInRect(ColorGameRules.NameOf(face), r, 26, TextOn(face), isBold: true);
    }

    // --- Betting board --------------------------------------------------------------------
    // matches: per-colour match count from the last roll (null while betting/rolling).
    public static void DrawBoard(ColorGameBetBook book, int[]? matches, bool interactive)
    {
        for (int i = 0; i < ColorGameRules.All.Length; i++)
        {
            DiceColor color = ColorGameRules.All[i];
            Rectangle tile = TileBounds(i);
            decimal stake = book.StakeOn(color);

            bool hovered = interactive && UiImageButton.IsHovered(tile);
            int matched = matches?[i] ?? 0;
            bool hasResult = matches != null;
            bool lostBet = hasResult && stake > 0m && matched == 0;
            bool dimmed = hasResult && stake == 0m;

            Texture2D art = TileTextures[i];
            bool textured = CasinoArt.Has(art);

            if (textured)
            {
                // Tile art (colour, name, "1 to 1 per die") comes from the texture; state is shown with a tint.
                Color tint = lostBet ? new Color(80, 80, 80, 255)
                    : dimmed ? new Color(140, 140, 140, 255)
                    : hovered ? Color.White
                    : new Color(235, 235, 235, 255);
                CasinoArt.Draw(art, tile, tint);
            }
            else
            {
                Color fill = ColorOf(color);
                if (lostBet) fill = new Color(fill.R / 3, fill.G / 3, fill.B / 3, 255);
                else if (dimmed) fill = new Color(fill.R / 2, fill.G / 2, fill.B / 2, 255);
                else if (hovered) fill = new Color(Math.Min(fill.R + 25, 255), Math.Min(fill.G + 25, 255), Math.Min(fill.B + 25, 255), 255);

                Raylib.DrawRectangleRounded(tile, 0.08f, 8, fill);
            }

            Color outline = hasResult && matched > 0 ? Color.Gold : new Color(255, 255, 255, hovered ? 230 : 90);
            Raylib.DrawRectangleLinesEx(tile, hasResult && matched > 0 ? 4 : 2, outline);

            if (!textured)
            {
                Color text = TextOn(color);
                UiFont.DrawCenteredInRect(ColorGameRules.NameOf(color), new Rectangle(tile.X, tile.Y + 8, tile.Width, 30), 22, text, isBold: true);
                UiFont.DrawCenteredInRect("1 TO 1 PER DIE", new Rectangle(tile.X, tile.Y + 36, tile.Width, 18), 11, text, isBold: false);
            }

            if (stake > 0m)
            {
                CasinoChips.DrawChip(new Vector2(tile.X + tile.Width / 2f, tile.Y + 96f), 26f, stake, CasinoChips.Format(stake));
            }

            if (hasResult && matched > 0)
            {
                // Match badge in the corner (x1 / x2 / x3)
                Rectangle badge = new(tile.X + tile.Width - 48, tile.Y + 6, 42, 24);
                if (CasinoArt.Has(_texMatchBadge)) CasinoArt.Draw(_texMatchBadge, badge);
                else Raylib.DrawRectangleRounded(badge, 0.4f, 6, Color.Gold);
                UiFont.DrawCenteredInRect($"x{matched}", badge, 16, Color.Black, isBold: true);

                if (stake > 0m)
                {
                    UiFont.DrawCenteredInRect($"+{stake * matched:N0}", new Rectangle(tile.X, tile.Y + tile.Height - 22, tile.Width, 20), 15, Color.Black, isBold: true);
                }
            }
            else if (lostBet)
            {
                UiFont.DrawCenteredInRect($"-{stake:N0}", new Rectangle(tile.X, tile.Y + tile.Height - 22, tile.Width, 20), 15, new Color(255, 160, 160, 255), isBold: true);
            }
        }
    }

    // --- Chip rack ------------------------------------------------------------------------
    public static void DrawChipRack(int selectedIndex, bool enabled)
    {
        UiFont.DrawText("SELECT CHIP", 65, 474, 13, Color.LightGray, isBold: true);

        for (int i = 0; i < CasinoChips.Values.Length; i++)
        {
            Vector2 center = ChipCenter(i);
            bool selected = i == selectedIndex;
            if (selected) center.Y -= 5f;

            CasinoChips.DrawChip(center, ChipRadius, CasinoChips.Values[i], selected: selected && enabled);
        }
    }

    // --- Buttons --------------------------------------------------------------------------
    public static void DrawButtons(bool canEdit, bool canRoll, string rollLabel)
    {
        Color idle = new(51, 65, 85, 255);
        Color off = new(40, 44, 54, 255);

        CasinoArt.DrawButton(_texBtnUndo, BtnUndo, "UNDO", canEdit, canEdit ? idle : off);
        CasinoArt.DrawButton(_texBtnClear, BtnClear, "CLEAR", canEdit, canEdit ? idle : off);
        CasinoArt.DrawButton(_texBtnRebet, BtnRebet, "REBET", canEdit, canEdit ? new Color(30, 90, 150, 255) : off);
        CasinoArt.DrawButton(_texBtnRoll, BtnRoll, rollLabel, canRoll, canRoll ? new Color(35, 103, 83, 255) : off);
    }

    // --- Info panels ----------------------------------------------------------------------
    public static void DrawInfoPanels(decimal totalBet, decimal lastWin, IReadOnlyList<DiceColor[]> history)
    {
        DrawValuePanel(new Rectangle(500, 488, 240, 70), "TOTAL BET", $"{totalBet:N2}", Color.Gold);
        DrawValuePanel(new Rectangle(760, 488, 240, 70), "LAST WIN", $"{lastWin:N2}", lastWin > 0m ? Color.Lime : Color.Gray);

        // Last rolls (newest on top)
        Rectangle panel = new(1020, 488, 195, 70);
        CasinoArt.DrawPanel(_texPanelRolls, panel, PanelBg, new Color(255, 255, 255, 25));
        UiFont.DrawText("LAST ROLLS", panel.X + 12, panel.Y + 5, 11, Color.LightGray, isBold: true);

        for (int row = 0; row < Math.Min(history.Count, 3); row++)
        {
            for (int d = 0; d < ColorGameRules.DiceCount; d++)
            {
                Rectangle sq = new(panel.X + 14 + d * 20, panel.Y + 22 + row * 15, 16, 12);
                Raylib.DrawRectangleRec(sq, ColorOf(history[row][d]));
                Raylib.DrawRectangleLinesEx(sq, 1, new Color(255, 255, 255, 120));
            }
        }

        if (history.Count == 0)
        {
            UiFont.DrawText("No rolls yet", panel.X + 12, panel.Y + 34, 12, new Color(150, 160, 180, 255));
        }
    }

    private static void DrawValuePanel(Rectangle rect, string label, string value, Color valueColor)
    {
        CasinoArt.DrawPanel(_texPanelValue, rect, PanelBg, new Color(255, 180, 0, 90));
        UiFont.DrawText(label, rect.X + 14, rect.Y + 8, 11, Color.LightGray, isBold: true);
        UiFont.DrawText(value, rect.X + 14, rect.Y + 28, 26, valueColor, isBold: true);
    }

    public static void DrawStatusLine(string text, Color color)
    {
        UiFont.DrawCenteredInRect(text, new Rectangle(0, 262, GameCanvas.Width, 40), 24, color, isBold: true);
    }

    public static void DrawFooterHint()
    {
        UiFont.DrawText(
            "Left click a colour: place chip   |   Right click: remove bet   |   Each die that matches your colour pays 1 to 1 (stake back + win)",
            65, 655, 13, new Color(170, 180, 200, 255));
        UiFont.DrawText("Example: 10 on RED and two dice land RED = 10 stake back + 20 win = 30 returned. Triple RED returns 40.", 65, 677, 12, new Color(130, 140, 160, 255));
    }
}
