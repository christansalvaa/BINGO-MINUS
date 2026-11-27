using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class BaccaratRenderer
{
    public const float CardWidth = 92f;
    public const float CardHeight = 130f;

    public static void DrawTableBackground()
    {
        // Deep mahogany / dark navy outer backdrop
        Raylib.ClearBackground(new Color(14, 18, 28, 255));

        // Luxury emerald-teal velvet table felt ellipse & inner frame
        Rectangle outerRail = new(18, 68, GameCanvas.Width - 36, 484);
        Raylib.DrawRectangleRounded(outerRail, 0.06f, 10, new Color(46, 22, 18, 255));
        Raylib.DrawRectangleRoundedLinesEx(outerRail, 0.06f, 10, 2.5f, new Color(186, 145, 58, 175));

        Rectangle feltRect = new(28, 76, GameCanvas.Width - 56, 468);
        Raylib.DrawRectangleRounded(feltRect, 0.055f, 10, new Color(13, 62, 48, 255));

        // Subtle spotlight sheen in center of felt
        Raylib.DrawEllipse(GameCanvas.Width / 2, 290, 440, 175, new Color(22, 92, 70, 95));
        Raylib.DrawEllipse(GameCanvas.Width / 2, 290, 290, 115, new Color(30, 112, 85, 65));

        // Inner gold decorative felt border
        Rectangle innerLine = new(38, 86, GameCanvas.Width - 76, 448);
        Raylib.DrawRectangleRoundedLinesEx(innerLine, 0.05f, 10, 1.5f, new Color(235, 195, 75, 95));

        // Subtle curved table center divider line
        Raylib.DrawLine(640, 155, 640, 355, new Color(235, 195, 75, 45));

        // Subtle embossed casino felt watermark
        UiFont.DrawCenteredText("PUNTO BANCO  •  8-DECK SHOE  •  TIE PAYS 8:1  •  PAIRS PAY 11:1", 352, 11, new Color(240, 215, 130, 85), isBold: true);
    }

    public static void DrawRoadmapPanel(Rectangle rect, IReadOnlyList<BaccaratRoundRecord> history)
    {
        Raylib.DrawRectangleRounded(rect, 0.14f, 6, new Color(11, 16, 28, 235));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.14f, 6, 1.5f, new Color(235, 195, 75, 130));

        // Left stats summary column inside roadmap
        int pWins = 0, bWins = 0, ties = 0;
        for (int i = 0; i < history.Count; i++)
        {
            switch (history[i].Outcome)
            {
                case BaccaratOutcome.PlayerWin: pWins++; break;
                case BaccaratOutcome.BankerWin: bWins++; break;
                case BaccaratOutcome.Tie: ties++; break;
            }
        }

        float sx = rect.X + 12;
        UiFont.DrawText("BEAD ROAD", sx, rect.Y + 7, 10, Color.Gold, isBold: true);
        DrawMiniCounter(sx, rect.Y + 22, "P", pWins, new Color(37, 99, 235, 255));
        DrawMiniCounter(sx + 48, rect.Y + 22, "B", bWins, new Color(220, 38, 38, 255));
        DrawMiniCounter(sx, rect.Y + 46, "T", ties, new Color(22, 163, 74, 255));
        UiFont.DrawText($"#{history.Count}", sx + 48, rect.Y + 48, 12, new Color(160, 175, 195, 255), isBold: true);

        // Bead Plate Grid: 4 rows x 22 columns (fills top-to-bottom per column)
        const int rows = 4;
        const int cols = 22;
        float cellSize = 15.5f;
        float gridX = rect.X + 108;
        float gridY = rect.Y + 6;

        // Grid background
        Raylib.DrawRectangle((int)gridX, (int)gridY, (int)(cols * cellSize), (int)(rows * cellSize), new Color(18, 24, 38, 255));
        for (int c = 0; c <= cols; c++)
        {
            float lx = gridX + c * cellSize;
            Raylib.DrawLine((int)lx, (int)gridY, (int)lx, (int)(gridY + rows * cellSize), new Color(255, 255, 255, 16));
        }
        for (int r = 0; r <= rows; r++)
        {
            float ly = gridY + r * cellSize;
            Raylib.DrawLine((int)gridX, (int)ly, (int)(gridX + cols * cellSize), (int)ly, new Color(255, 255, 255, 16));
        }

        // Show most recent (rows * cols) rounds in chronological left-to-right columns
        int maxCells = rows * cols;
        int startIndex = Math.Max(0, history.Count - maxCells);
        int visibleCount = history.Count - startIndex;

        for (int i = 0; i < visibleCount; i++)
        {
            var rec = history[startIndex + i];
            int col = i / rows;
            int row = i % rows;
            float cx = gridX + col * cellSize + cellSize * 0.5f;
            float cy = gridY + row * cellSize + cellSize * 0.5f;

            Color beadCol = rec.Outcome switch
            {
                BaccaratOutcome.PlayerWin => new Color(37, 99, 235, 255),
                BaccaratOutcome.BankerWin => new Color(220, 38, 38, 255),
                _ => new Color(22, 163, 74, 255)
            };

            string letter = rec.Outcome switch
            {
                BaccaratOutcome.PlayerWin => "P",
                BaccaratOutcome.BankerWin => "B",
                _ => "T"
            };

            Raylib.DrawCircleV(new Vector2(cx, cy), 6.2f, beadCol);
            if (rec.IsNatural)
            {
                Raylib.DrawCircleLines((int)cx, (int)cy, 6.5f, Color.Gold);
            }

            // Tiny pair dots (authentic Macau Bead Plate: top-left red = Banker Pair, bottom-right blue = Player Pair)
            if (rec.BankerPair)
            {
                Raylib.DrawCircleV(new Vector2(cx - 4.8f, cy - 4.8f), 2.3f, new Color(255, 90, 90, 255));
            }
            if (rec.PlayerPair)
            {
                Raylib.DrawCircleV(new Vector2(cx + 4.8f, cy + 4.8f), 2.3f, new Color(96, 165, 250, 255));
            }

            UiFont.DrawCenteredInRect(letter, new Rectangle(cx - 6, cy - 6, 12, 12), 9, Color.White, isBold: true);
        }
    }

    private static void DrawMiniCounter(float x, float y, string label, int count, Color color)
    {
        Raylib.DrawCircleV(new Vector2(x + 8, y + 9), 8, color);
        UiFont.DrawCenteredInRect(label, new Rectangle(x, y + 1, 16, 16), 10, Color.White, isBold: true);
        UiFont.DrawText(count.ToString(), x + 20, y + 2, 13, Color.White, isBold: true);
    }

    public static void DrawAnimatedCard(
        Card card,
        Vector2 centerPos,
        float flipProgress, // 0 = face down, 1 = face up
        bool rotatedSideways,
        bool highlightGold = false)
    {
        // Horizontal scale simulates 3D flip across flipProgress = 0.5
        float scaleX = MathF.Abs(MathF.Cos(Math.Clamp(flipProgress, 0f, 1f) * MathF.PI));
        scaleX = Math.Max(0.05f, scaleX);
        bool showFront = flipProgress >= 0.5f;

        float w = (rotatedSideways ? CardHeight : CardWidth) * (rotatedSideways ? 1f : scaleX);
        float h = (rotatedSideways ? CardWidth : CardHeight) * (rotatedSideways ? scaleX : 1f);
        Rectangle rect = new(centerPos.X - w * 0.5f, centerPos.Y - h * 0.5f, w, h);

        // Drop shadow
        Raylib.DrawRectangleRounded(
            new Rectangle(rect.X + 4, rect.Y + 6, rect.Width, rect.Height),
            0.12f, 8, new Color(0, 0, 0, 125));

        if (!showFront)
        {
            DrawCardBack(rect);
            return;
        }

        // Crisp ivory card face
        Raylib.DrawRectangleRounded(rect, 0.12f, 8, new Color(252, 250, 244, 255));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.12f, 8, highlightGold ? 2.8f : 1.5f,
            highlightGold ? Color.Gold : new Color(185, 178, 162, 255));

        // Only draw card ink once flip isn't razor-thin
        if (scaleX < 0.25f) return;

        bool isRed = card.Suit is Suit.Hearts or Suit.Diamonds;
        Color ink = isRed ? new Color(205, 28, 42, 255) : new Color(22, 28, 42, 255);
        string rankStr = GetRankSymbol(card.Rank);

        // Subtle inner pinstripe frame
        Rectangle inner = new(rect.X + 5, rect.Y + 5, Math.Max(2, rect.Width - 10), Math.Max(2, rect.Height - 10));
        Raylib.DrawRectangleRoundedLinesEx(inner, 0.08f, 6, 1f, new Color(210, 195, 160, 110));

        // Corner index (top-left)
        float rankSize = rotatedSideways ? 20f : 22f;
        UiFont.DrawText(rankStr, rect.X + 8, rect.Y + 6, rankSize, ink, isBold: true);
        DrawSuitShape(card.Suit, new Vector2(rect.X + 17, rect.Y + 35), 6.5f, ink);

        // Center large suit emblem
        Vector2 mid = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        DrawSuitShape(card.Suit, mid, rotatedSideways ? 16f : 21f, ink);

        // Bottom-right corner rank
        Vector2 rSize = UiFont.MeasureText(rankStr, rankSize, isBold: true);
        UiFont.DrawText(rankStr, rect.X + rect.Width - rSize.X - 8, rect.Y + rect.Height - rSize.Y - 6, rankSize, ink, isBold: true);
        DrawSuitShape(card.Suit, new Vector2(rect.X + rect.Width - 17, rect.Y + rect.Height - 34), 6.5f, ink);
    }

    private static void DrawCardBack(Rectangle rect)
    {
        // Royal crimson & gold casino back
        Raylib.DrawRectangleRounded(rect, 0.12f, 8, new Color(248, 246, 240, 255));
        Rectangle inner = new(rect.X + 5, rect.Y + 5, Math.Max(2, rect.Width - 10), Math.Max(2, rect.Height - 10));
        Raylib.DrawRectangleRounded(inner, 0.08f, 6, new Color(138, 18, 38, 255));
        Raylib.DrawRectangleRoundedLinesEx(inner, 0.08f, 6, 1.5f, new Color(238, 198, 80, 220));

        if (inner.Width > 26 && inner.Height > 26)
        {
            Rectangle inner2 = new(inner.X + 5, inner.Y + 5, inner.Width - 10, inner.Height - 10);
            Raylib.DrawRectangleRoundedLinesEx(inner2, 0.06f, 6, 1f, new Color(238, 198, 80, 120));

            Vector2 c = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            Raylib.DrawPoly(c, 4, Math.Min(inner2.Width, inner2.Height) * 0.28f, 0f, new Color(238, 198, 80, 185));
            Raylib.DrawPoly(c, 4, Math.Min(inner2.Width, inner2.Height) * 0.16f, 0f, new Color(138, 18, 38, 255));
        }
    }

    private static string GetRankSymbol(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        _ => ((int)rank).ToString()
    };

    private static void DrawSuitShape(Suit suit, Vector2 center, float size, Color color)
    {
        switch (suit)
        {
            case Suit.Diamonds:
                Raylib.DrawTriangle(
                    new Vector2(center.X, center.Y - size),
                    new Vector2(center.X - size * 0.72f, center.Y),
                    new Vector2(center.X + size * 0.72f, center.Y),
                    color);
                Raylib.DrawTriangle(
                    new Vector2(center.X - size * 0.72f, center.Y),
                    new Vector2(center.X, center.Y + size),
                    new Vector2(center.X + size * 0.72f, center.Y),
                    color);
                break;

            case Suit.Hearts:
                float r = size * 0.48f;
                Raylib.DrawCircleV(new Vector2(center.X - r * 0.82f, center.Y - r * 0.35f), r, color);
                Raylib.DrawCircleV(new Vector2(center.X + r * 0.82f, center.Y - r * 0.35f), r, color);
                Raylib.DrawTriangle(
                    new Vector2(center.X - size * 0.86f, center.Y - r * 0.1f),
                    new Vector2(center.X, center.Y + size * 0.95f),
                    new Vector2(center.X + size * 0.86f, center.Y - r * 0.1f),
                    color);
                break;

            case Suit.Spades:
                float sr = size * 0.44f;
                Raylib.DrawTriangle(
                    new Vector2(center.X, center.Y - size * 0.95f),
                    new Vector2(center.X - size * 0.84f, center.Y + sr * 0.25f),
                    new Vector2(center.X + size * 0.84f, center.Y + sr * 0.25f),
                    color);
                Raylib.DrawCircleV(new Vector2(center.X - sr * 0.82f, center.Y + sr * 0.35f), sr, color);
                Raylib.DrawCircleV(new Vector2(center.X + sr * 0.82f, center.Y + sr * 0.35f), sr, color);
                Raylib.DrawTriangle(
                    new Vector2(center.X, center.Y + sr * 0.25f),
                    new Vector2(center.X - size * 0.38f, center.Y + size * 0.95f),
                    new Vector2(center.X + size * 0.38f, center.Y + size * 0.95f),
                    color);
                break;

            case Suit.Clubs:
                float cr = size * 0.42f;
                Raylib.DrawCircleV(new Vector2(center.X, center.Y - cr * 0.95f), cr, color);
                Raylib.DrawCircleV(new Vector2(center.X - cr * 0.92f, center.Y + cr * 0.28f), cr, color);
                Raylib.DrawCircleV(new Vector2(center.X + cr * 0.92f, center.Y + cr * 0.28f), cr, color);
                Raylib.DrawCircleV(new Vector2(center.X, center.Y), cr * 0.45f, color);
                Raylib.DrawTriangle(
                    new Vector2(center.X, center.Y + cr * 0.2f),
                    new Vector2(center.X - size * 0.36f, center.Y + size * 0.95f),
                    new Vector2(center.X + size * 0.36f, center.Y + size * 0.95f),
                    color);
                break;
        }
    }

    public static void DrawCasinoChip(Vector2 center, float radius, int value, bool selected, bool affordable)
    {
        Color mainColor = value switch
        {
            1 => new Color(215, 222, 232, 255),
            5 => new Color(215, 42, 48, 255),
            25 => new Color(24, 148, 76, 255),
            50 => new Color(36, 102, 212, 255),
            100 => new Color(30, 34, 46, 255),
            500 => new Color(138, 43, 196, 255),
            1000 => new Color(224, 168, 22, 255),
            _ => Color.Gold
        };

        byte alpha = affordable ? (byte)255 : (byte)95;
        Color body = new(mainColor.R, mainColor.G, mainColor.B, alpha);

        // Selected glow ring
        if (selected)
        {
            Raylib.DrawCircleV(center, radius + 5.5f, new Color(255, 215, 0, 120));
            Raylib.DrawCircleLines((int)center.X, (int)center.Y, radius + 5.5f, Color.Gold);
        }

        // Shadow
        Raylib.DrawCircleV(center + new Vector2(0, 4), radius, new Color(0, 0, 0, 110));

        // Base chip disc
        Raylib.DrawCircleV(center, radius, body);

        // 6 white casino edge notches
        for (int i = 0; i < 6; i++)
        {
            float ang = i * (MathF.PI / 3f);
            Vector2 p1 = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radius - 5.5f);
            Vector2 p2 = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radius - 1f);
            Raylib.DrawLineEx(p1, p2, 4.5f, new Color(255, 255, 255, affordable ? 225 : 80));
        }

        // Inner ring & center recess
        Raylib.DrawCircleV(center, radius * 0.72f, new Color(
            (byte)Math.Max(0, mainColor.R - 18),
            (byte)Math.Max(0, mainColor.G - 18),
            (byte)Math.Max(0, mainColor.B - 18),
            alpha));
        Raylib.DrawCircleLines((int)center.X, (int)center.Y, radius * 0.72f, new Color(255, 255, 255, affordable ? 170 : 60));
        Raylib.DrawCircleLines((int)center.X, (int)center.Y, radius, new Color(255, 255, 255, affordable ? 200 : 75));

        string txt = value >= 1000 ? $"{value / 1000}K" : value.ToString();
        Color textCol = (value == 1 || value == 1000) ? new Color((byte)18, (byte)22, (byte)32, alpha) : new Color((byte)255, (byte)255, (byte)255, alpha);
        float fontSize = txt.Length >= 3 ? 13f : 15f;
        UiFont.DrawCenteredInRect(txt, new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2), fontSize, textCol, isBold: true);
    }

    public static void DrawBetStack(Vector2 center, decimal amount)
    {
        if (amount <= 0m) return;

        int stackCount = amount >= 500m ? 4 : amount >= 100m ? 3 : amount >= 25m ? 2 : 1;
        int topVal = amount >= 1000m ? 1000 : amount >= 500m ? 500 : amount >= 100m ? 100 : amount >= 50m ? 50 : amount >= 25m ? 25 : amount >= 5m ? 5 : 1;

        for (int i = 0; i < stackCount; i++)
        {
            Vector2 pos = new(center.X, center.Y - i * 4.5f);
            DrawCasinoChip(pos, 18f, topVal, selected: false, affordable: true);
        }

        // Crisp pill below chip stack showing exact bet
        string label = $"{amount:0.##}";
        float w = Math.Max(52f, UiFont.MeasureText(label, 13, isBold: true).X + 16f);
        Rectangle pill = new(center.X - w * 0.5f, center.Y + 18f, w, 22f);
        Raylib.DrawRectangleRounded(pill, 0.5f, 6, new Color(12, 16, 26, 235));
        Raylib.DrawRectangleRoundedLinesEx(pill, 0.5f, 6, 1.2f, Color.Gold);
        UiFont.DrawCenteredInRect(label, pill, 13, Color.Gold, isBold: true);
    }
}
