using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>All drawing (this was "TextureHandler.DrawGame" in the memory game).</summary>
    public static class Renderer
    {
        private const int W = Theme.WindowWidth;
        private const int H = Theme.WindowHeight;
        private static readonly Color DarkText = new(6, 40, 20, 255);

        public static void Draw()
        {
            Raylib.ClearBackground(Theme.BgBottom);

            // Apply screen shake via rlgl so GameCanvas's outer Camera2D scaling stays intact.
            Rlgl.PushMatrix();
            Rlgl.Translatef(W / 2f + Effects.ShakeOffset.X, H / 2f + Effects.ShakeOffset.Y, 0f);
            Rlgl.Rotatef(Effects.ShakeRotation, 0f, 0f, 1f);
            Rlgl.Translatef(-W / 2f, -H / 2f, 0f);

            DrawBackground();
            DrawLeftPanel();
            DrawTable();
            DrawRightPanel();
            Effects.DrawSmoke();

            Rlgl.PopMatrix();

            Effects.DrawRedSmoke(W, H);   // full-screen, not shaken

            if (GameManager.BannerTimer > 0f && GameManager.Banner.Length > 0)
            {
                float a = Math.Clamp(GameManager.BannerTimer / 0.5f, 0f, 1f);
                float bw = UI.Measure(GameManager.Banner, 20).X + 36f;
                Rectangle bannerRect = new((W - bw) / 2f, 66f, bw, 32f);
                Raylib.DrawRectangleRounded(bannerRect, 0.4f, 8, Theme.Alpha(Theme.Panel, 0.92f * a));
                UI.RoundedOutline(bannerRect, 0.4f, 1.5f, Theme.Alpha(GameManager.BannerColor, a));
                UI.TextCenter(GameManager.Banner, W / 2f, 72f, 20, Theme.Alpha(GameManager.BannerColor, a));
            }
        }

        // ------------------------------------------------------------ background
        private static void DrawBackground()
        {
            // drawn bigger than the window so the shake never shows an empty edge
            Raylib.DrawRectangleGradientV(-100, -100, W + 200, H + 200, Theme.BgTop, Theme.BgBottom);
        }

        private static void Panel(Rectangle r)
        {
            Raylib.DrawRectangleRounded(r, 0.04f, 10, Theme.Panel);
            UI.RoundedOutline(r, 0.04f, 2f, Theme.PanelEdge);
        }

        private static void Btn(Rectangle r, string text, bool enabled, float size, bool selected = false)
        {
            Vector2 m = BINGO_MINUS.GameCanvas.GetMousePosition();
            bool hover = enabled && Raylib.CheckCollisionPointRec(m, r);
            Color bg = !enabled ? Theme.ButtonOff : selected ? Theme.Accent : hover ? Theme.ButtonHover : Theme.Button;
            Raylib.DrawRectangleRounded(r, 0.25f, 8, bg);
            UI.TextIn(r, text, size, enabled ? Theme.Text : Theme.Muted);
        }

        private static void FieldBox(Rectangle r, bool enabled = true, bool focused = false)
        {
            Raylib.DrawRectangleRounded(r, 0.18f, 8, enabled ? Theme.Field : Theme.FieldOff);
            UI.RoundedOutline(r, 0.18f, 2f, focused ? Theme.Cyan : Theme.FieldEdge);
        }

        private static void Coin(Vector2 center)
        {
            Raylib.DrawCircleV(center, 11f, Theme.Gold);
            UI.TextCenter("$", center.X, center.Y - 9f, 15, new Color(120, 80, 0, 255));
        }

        // -------------------------------------------------------------- left panel
        private static void DrawLeftPanel()
        {
            Panel(Layout.LeftPanel);

            bool idle = GameManager.State == GameState.Idle;
            bool playing = GameManager.State == GameState.Playing;
            Vector2 m = BINGO_MINUS.GameCanvas.GetMousePosition();

            // --- bet amount
            UI.Text("Bet Amount", 40, 94, 14, Theme.Muted);

            Rectangle bf = Layout.BetField;
            FieldBox(bf, idle, GameManager.EditingBet);

            string betText = GameManager.BetText;
            float tx = bf.X + 12f;
            float ty = bf.Y + (bf.Height - 20f) / 2f;
            if (GameManager.EditingBet && GameManager.BetSelected && betText.Length > 0)
            {
                Vector2 sz = UI.Measure(betText, 20);
                Raylib.DrawRectangle((int)tx - 2, (int)bf.Y + 8, (int)sz.X + 4, (int)bf.Height - 16, Theme.Alpha(Theme.Cyan, 0.35f));
            }
            bool caret = GameManager.EditingBet && !GameManager.BetSelected && ((int)(Raylib.GetTime() * 2.0) % 2 == 0);
            UI.Text(betText + (caret ? "|" : ""), tx, ty, 20, idle ? Theme.Text : Theme.Muted);
            Coin(new Vector2(bf.X + bf.Width - 20f, bf.Y + bf.Height / 2f));

            Btn(Layout.HalfBtn, "1/2", idle, 16);
            Btn(Layout.DoubleBtn, "2x", idle, 16);
            Btn(Layout.MinBtn, "Min", idle, 14);
            Btn(Layout.MaxBtn, "Max", idle, 14);

            // --- mines / gems
            UI.Text("Mines", 40, 228, 14, Theme.Muted);
            UI.Text("Gems", 212, 228, 14, Theme.Muted);

            FieldBox(Layout.MinesBox, idle);
            UI.TextIn(Layout.MinesBox, $"{(playing ? BoardManager.MineCount : GameManager.Mines)}", 20, idle ? Theme.Text : Theme.Muted);
            Btn(Layout.MinesMinus, "-", idle, 20);
            Btn(Layout.MinesPlus, "+", idle, 20);

            FieldBox(Layout.GemsBox, idle);
            int gems = BoardManager.CardCount - (playing ? BoardManager.MineCount : GameManager.Mines);
            UI.TextIn(new Rectangle(Layout.GemsBox.X + 22, Layout.GemsBox.Y, Layout.GemsBox.Width - 22, Layout.GemsBox.Height), $"{gems}", 20, Theme.Cyan);
            Texture2D gem = AssetManager.Gem;
            if (gem.Id != 0)
            {
                float gw = 22f, gh = gw * gem.Height / gem.Width;
                Raylib.DrawTexturePro(gem, new Rectangle(0, 0, gem.Width, gem.Height),
                    new Rectangle(Layout.GemsBox.X + 10, Layout.GemsBox.Y + (Layout.GemsBox.Height - gh) / 2f, gw, gh),
                    Vector2.Zero, 0f, Color.White);
            }

            int selectedMines = GameManager.Mines;
            for (int i = 0; i < Layout.Presets.Length; i++)
                Btn(Layout.Presets[i], $"{Layout.PresetValues[i]}", idle, 14, idle && selectedMines == Layout.PresetValues[i]);

            // --- total profit
            double mult = playing ? GameManager.CurrentMultiplier : 1.00;
            double profit = playing ? Math.Round(GameManager.RoundBet * mult - GameManager.RoundBet, 2) : 0.0;
            UI.Text($"Total profit ({mult:F2}x)", 40, 354, 14, Theme.Muted);
            FieldBox(Layout.ProfitBox, true);
            UI.Text($"${profit:F2}", Layout.ProfitBox.X + 12f, Layout.ProfitBox.Y + (Layout.ProfitBox.Height - 20f) / 2f, 20,
                profit > 0 ? Theme.Green : Theme.Text);
            Coin(new Vector2(Layout.ProfitBox.X + Layout.ProfitBox.Width - 20f, Layout.ProfitBox.Y + Layout.ProfitBox.Height / 2f));

            // --- odds info
            int picks = playing ? BoardManager.GemsFound : 0;
            int mines = playing ? BoardManager.MineCount : GameManager.Mines;
            if (BoardManager.CardCount - mines - picks > 0)
            {
                double next = BoardManager.Multiplier(mines, picks + 1);
                double chance = mines * 100.0 / (BoardManager.CardCount - picks);
                UI.Text("Next gem pays", 40, 440, 14, Theme.Muted);
                UI.TextRight($"{next:F2}x", 308, 438, 17, Theme.Gold);
                UI.Text("Mine chance", 40, 468, 14, Theme.Muted);
                UI.TextRight($"{chance:F1}%", 308, 466, 17, Theme.Text);
            }

            // --- random pick
            Btn(Layout.RandomBtn, "Random Pick", playing, 17);

            // --- BET / CASHOUT
            Rectangle mb = Layout.MainBtn;
            bool hov = Raylib.CheckCollisionPointRec(m, mb);
            float mcx = mb.X + mb.Width / 2f;
            if (idle)
            {
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, hov ? Theme.GreenHover : Theme.Green);
                UI.TextIn(mb, "BET", 28, DarkText);
            }
            else if (playing)
            {
                bool can = BoardManager.GemsFound > 0;
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, !can ? Theme.GreenOff : hov ? Theme.GreenHover : Theme.Green);
                if (can)
                {
                    double payout = Math.Round(GameManager.RoundBet * GameManager.CurrentMultiplier, 2);
                    UI.TextCenter("CASHOUT", mcx, mb.Y + 8, 22, DarkText);
                    UI.TextCenter($"${payout:F2}", mcx, mb.Y + 34, 18, DarkText);
                }
                else
                {
                    UI.TextIn(mb, "PICK A CARD", 20, Theme.Muted);
                }
            }
            else
            {
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, new Color(92, 24, 46, 255));
                UI.TextIn(mb, "BOOM!", 28, Theme.Red);
            }

            UI.TextCenter("SPACE: bet / cash out     R: random pick", mcx, 679, 12, Theme.Muted);
        }

        // -------------------------------------------------------------------- table
        private static void DrawTable()
        {
            Rectangle t = Layout.Table;
            Raylib.DrawRectangleRounded(t, 0.04f, 10, new Color(18, 15, 40, 255));
            UI.RoundedOutline(t, 0.04f, 2f, Theme.PanelEdge);

            foreach (Card c in BoardManager.Cards) DrawCard(c);
        }

        private static void DrawCard(Card c)
        {
            // Flip animation: the card narrows to a line (back side), then widens (front side).
            float sx = MathF.Abs(MathF.Cos(c.Flip * MathF.PI));
            float pop = 1f + 0.10f * MathF.Sin(c.Flip * MathF.PI);

            float cx = c.Bounds.X + c.Bounds.Width / 2f;
            float cy = c.Bounds.Y + c.Bounds.Height / 2f - c.Hover * 4f;
            float w = MathF.Max(2f, c.Bounds.Width * sx * pop);
            float h = c.Bounds.Height * pop;
            Rectangle r = new Rectangle(cx - w / 2f, cy - h / 2f, w, h);

            Raylib.DrawRectangleRounded(new Rectangle(r.X + 2, r.Y + 5, r.Width, r.Height), 0.12f, 8, new Color(0, 0, 0, 80));

            if (c.Flip < 0.5f) DrawCardBack(c, r);
            else DrawCardFace(c, r, sx, pop);
        }

        private static void DrawCardBack(Card c, Rectangle r)
        {
            Texture2D t = AssetManager.CardBack;
            if (t.Id != 0)
            {
                int v = (int)(225 + 30 * c.Hover);
                Raylib.DrawTexturePro(t, new Rectangle(0, 0, t.Width, t.Height), r, Vector2.Zero, 0f, new Color(v, v, v, 255));
            }
            else
            {
                Raylib.DrawRectangleRounded(r, 0.12f, 8, new Color(24, 60, 52, 255));
                UI.TextIn(r, "?", 40, Theme.Gold);
            }

            if (c.Hover > 0.01f) UI.RoundedOutline(r, 0.12f, 2f, Theme.Alpha(Theme.Cyan, c.Hover));
        }

        private static void DrawCardFace(Card c, Rectangle r, float sx, float pop)
        {
            bool bomb = c.ShownBomb;
            float dim = c.IsRevealed ? 1f : 0.55f;   // cards shown at round end are dimmed

            Color fill = bomb ? new Color(56, 16, 32, 255) : new Color(14, 32, 52, 255);
            Color edge = bomb ? Theme.Red : Theme.Cyan;
            Raylib.DrawRectangleRounded(r, 0.12f, 8, fill);

            Vector2 center = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
            if (sx > 0.5f)
            {
                for (int i = 0; i < 3; i++)
                    Raylib.DrawCircleV(center, 36f - i * 9f, Theme.Alpha(edge, 0.10f * dim));
            }

            Texture2D t = bomb ? AssetManager.Bomb : AssetManager.Gem;
            if (t.Id != 0)
            {
                float baseW = bomb ? 62f : 60f;
                float iw = MathF.Max(1f, baseW * sx * pop);
                float ih = baseW * t.Height / t.Width * pop;
                Raylib.DrawTexturePro(t, new Rectangle(0, 0, t.Width, t.Height),
                    new Rectangle(center.X - iw / 2f, center.Y - ih / 2f, iw, ih),
                    Vector2.Zero, 0f, Theme.Alpha(Color.White, dim));
            }
            else
            {
                UI.TextIn(r, bomb ? "BOMB" : "GEM", 16, Theme.Text);
            }

            UI.RoundedOutline(r, 0.12f, 2.5f, Theme.Alpha(edge, dim));
            if (!c.IsRevealed) Raylib.DrawRectangleRounded(r, 0.12f, 8, new Color(8, 6, 20, 110));
        }

        // ------------------------------------------------------------- right panel
        private static void DrawRightPanel()
        {
            Rectangle p = Layout.RightPanel;
            Panel(p);

            float x = p.X + 16f;
            float right = p.X + p.Width - 16f;

            UI.Text("Live Stats", x, 90, 20, Theme.Text);

            StatBox(new Rectangle(x, 128, 130, 56), "PROFIT", Signed(Stats.Profit), Stats.Profit >= 0 ? Theme.Green : Theme.Red);
            StatBox(new Rectangle(x + 138, 128, 130, 56), "WAGERED", $"${Stats.Wagered:F2}", Theme.Text);
            StatBox(new Rectangle(x, 192, 130, 56), "WINS", $"{Stats.Wins}", Theme.Green);
            StatBox(new Rectangle(x + 138, 192, 130, 56), "LOSSES", $"{Stats.Losses}", Theme.Red);

            UI.Text("Recent Rounds", x, 266, 18, Theme.Text);
            UI.Text("BET", x, 298, 12, Theme.Muted);
            UI.Text("MULT", x + 100, 298, 12, Theme.Muted);
            UI.TextRight("PROFIT", right, 298, 12, Theme.Muted);

            float y = 322f;
            for (int i = 0; i < Stats.History.Count; i++)
            {
                RoundResult rr = Stats.History[i];
                if (i % 2 == 0)
                    Raylib.DrawRectangleRounded(new Rectangle(x - 6, y - 2, 280, 28), 0.3f, 6, Theme.Alpha(Theme.Field, 0.7f));

                bool won = rr.Payout > rr.Bet;
                UI.Text($"${rr.Bet:F2}", x, y + 3, 15, Theme.Text);
                UI.Text($"{rr.Multiplier:F2}x", x + 100, y + 3, 15, won ? Theme.Gold : Theme.Muted);
                UI.TextRight(Signed(rr.Profit), right, y + 3, 15, won ? Theme.Green : Theme.Red);
                y += 30f;
            }

            if (Stats.History.Count == 0)
                UI.Text("Place a bet to start!", x, 326, 15, Theme.Muted);
        }

        private static void StatBox(Rectangle r, string label, string value, Color valueColor)
        {
            Raylib.DrawRectangleRounded(r, 0.18f, 8, Theme.Field);
            UI.RoundedOutline(r, 0.18f, 2f, Theme.FieldEdge);
            UI.Text(label, r.X + 12, r.Y + 8, 12, Theme.Muted);
            UI.Text(value, r.X + 12, r.Y + 26, 18, valueColor);
        }

        private static string Signed(double v)
        {
            v = Math.Round(v, 2);
            return v >= 0 ? $"+${v:F2}" : $"-${Math.Abs(v):F2}";
        }
    }
}
