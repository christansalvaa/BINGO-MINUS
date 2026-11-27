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
                float bw = UI.Measure(GameManager.Banner, 30).X + 54f;
                Rectangle bannerRect = new((W - bw) / 2f, 99f, bw, 48f);
                Raylib.DrawRectangleRounded(bannerRect, 0.4f, 8, Theme.Alpha(Theme.Panel, 0.92f * a));
                UI.RoundedOutline(bannerRect, 0.4f, 2.25f, Theme.Alpha(GameManager.BannerColor, a));
                UI.TextCenter(GameManager.Banner, W / 2f, 108f, 30, Theme.Alpha(GameManager.BannerColor, a));
            }
        }

        // ------------------------------------------------------------ background
        private static void DrawBackground()
        {
            // drawn bigger than the window so the shake never shows an empty edge
            Raylib.DrawRectangleGradientV(-150, -150, W + 300, H + 300, Theme.BgTop, Theme.BgBottom);
        }

        private static void Panel(Rectangle r)
        {
            Raylib.DrawRectangleRounded(r, 0.04f, 10, Theme.Panel);
            UI.RoundedOutline(r, 0.04f, 3f, Theme.PanelEdge);
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
            UI.RoundedOutline(r, 0.18f, 3f, focused ? Theme.Cyan : Theme.FieldEdge);
        }

        private static void Coin(Vector2 center)
        {
            Raylib.DrawCircleV(center, 16.5f, Theme.Gold);
            UI.TextCenter("$", center.X, center.Y - 13.5f, 22.5f, new Color(120, 80, 0, 255));
        }

        // -------------------------------------------------------------- left panel
        private static void DrawLeftPanel()
        {
            Rectangle lp = Layout.LeftPanel;
            Panel(lp);

            float lx = lp.X + 24f;
            float lright = lp.X + lp.Width - 24f;
            bool idle = GameManager.State == GameState.Idle;
            bool playing = GameManager.State == GameState.Playing;
            Vector2 m = BINGO_MINUS.GameCanvas.GetMousePosition();

            // --- bet amount
            UI.Text("Bet Amount", lx, 141, 21, Theme.Muted);

            Rectangle bf = Layout.BetField;
            FieldBox(bf, idle, GameManager.EditingBet);

            string betText = GameManager.BetText;
            float tx = bf.X + 18f;
            float ty = bf.Y + (bf.Height - 30f) / 2f;
            if (GameManager.EditingBet && GameManager.BetSelected && betText.Length > 0)
            {
                Vector2 sz = UI.Measure(betText, 30);
                Raylib.DrawRectangle((int)tx - 3, (int)bf.Y + 12, (int)sz.X + 6, (int)bf.Height - 24, Theme.Alpha(Theme.Cyan, 0.35f));
            }
            bool caret = GameManager.EditingBet && !GameManager.BetSelected && ((int)(Raylib.GetTime() * 2.0) % 2 == 0);
            UI.Text(betText + (caret ? "|" : ""), tx, ty, 30, idle ? Theme.Text : Theme.Muted);
            Coin(new Vector2(bf.X + bf.Width - 30f, bf.Y + bf.Height / 2f));

            Btn(Layout.HalfBtn, "1/2", idle, 24);
            Btn(Layout.DoubleBtn, "2x", idle, 24);
            Btn(Layout.MinBtn, "Min", idle, 21);
            Btn(Layout.MaxBtn, "Max", idle, 21);

            // --- mines / gems
            UI.Text("Mines", lx, 342, 21, Theme.Muted);
            UI.Text("Gems", Layout.GemsBox.X, 342, 21, Theme.Muted);

            FieldBox(Layout.MinesBox, idle);
            UI.TextIn(Layout.MinesBox, $"{(playing ? BoardManager.MineCount : GameManager.Mines)}", 30, idle ? Theme.Text : Theme.Muted);
            Btn(Layout.MinesMinus, "-", idle, 30);
            Btn(Layout.MinesPlus, "+", idle, 30);

            FieldBox(Layout.GemsBox, idle);
            int gems = BoardManager.CardCount - (playing ? BoardManager.MineCount : GameManager.Mines);
            UI.TextIn(new Rectangle(Layout.GemsBox.X + 33, Layout.GemsBox.Y, Layout.GemsBox.Width - 33, Layout.GemsBox.Height), $"{gems}", 30, Theme.Cyan);
            Texture2D gem = AssetManager.Gem;
            if (gem.Id != 0)
            {
                float gw = 33f, gh = gw * gem.Height / gem.Width;
                Raylib.DrawTexturePro(gem, new Rectangle(0, 0, gem.Width, gem.Height),
                    new Rectangle(Layout.GemsBox.X + 15, Layout.GemsBox.Y + (Layout.GemsBox.Height - gh) / 2f, gw, gh),
                    Vector2.Zero, 0f, Color.White);
            }

            int selectedMines = GameManager.Mines;
            for (int i = 0; i < Layout.Presets.Length; i++)
                Btn(Layout.Presets[i], $"{Layout.PresetValues[i]}", idle, 21, idle && selectedMines == Layout.PresetValues[i]);

            // --- total profit
            double mult = playing ? GameManager.CurrentMultiplier : 1.00;
            double profit = playing ? Math.Round(GameManager.RoundBet * mult - GameManager.RoundBet, 2) : 0.0;
            UI.Text($"Total profit ({mult:F2}x)", lx, 531, 21, Theme.Muted);
            FieldBox(Layout.ProfitBox, true);
            UI.Text($"${profit:F2}", Layout.ProfitBox.X + 18f, Layout.ProfitBox.Y + (Layout.ProfitBox.Height - 30f) / 2f, 30,
                profit > 0 ? Theme.Green : Theme.Text);
            Coin(new Vector2(Layout.ProfitBox.X + Layout.ProfitBox.Width - 30f, Layout.ProfitBox.Y + Layout.ProfitBox.Height / 2f));

            // --- odds info
            int picks = playing ? BoardManager.GemsFound : 0;
            int mines = playing ? BoardManager.MineCount : GameManager.Mines;
            if (BoardManager.CardCount - mines - picks > 0)
            {
                double next = BoardManager.Multiplier(mines, picks + 1);
                double chance = mines * 100.0 / (BoardManager.CardCount - picks);
                UI.Text("Next gem pays", lx, 660, 21, Theme.Muted);
                UI.TextRight($"{next:F2}x", lright, 657, 25.5f, Theme.Gold);
                UI.Text("Mine chance", lx, 702, 21, Theme.Muted);
                UI.TextRight($"{chance:F1}%", lright, 699, 25.5f, Theme.Text);
            }

            // --- random pick
            Btn(Layout.RandomBtn, "Random Pick", playing, 25.5f);

            // --- BET / CASHOUT
            Rectangle mb = Layout.MainBtn;
            bool hov = Raylib.CheckCollisionPointRec(m, mb);
            float mcx = mb.X + mb.Width / 2f;
            if (idle)
            {
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, hov ? Theme.GreenHover : Theme.Green);
                UI.TextIn(mb, "BET", 42, DarkText);
            }
            else if (playing)
            {
                bool can = BoardManager.GemsFound > 0;
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, !can ? Theme.GreenOff : hov ? Theme.GreenHover : Theme.Green);
                if (can)
                {
                    double payout = Math.Round(GameManager.RoundBet * GameManager.CurrentMultiplier, 2);
                    UI.TextCenter("CASHOUT", mcx, mb.Y + 12, 33, DarkText);
                    UI.TextCenter($"${payout:F2}", mcx, mb.Y + 51, 27, DarkText);
                }
                else
                {
                    UI.TextIn(mb, "PICK A CARD", 30, Theme.Muted);
                }
            }
            else
            {
                Raylib.DrawRectangleRounded(mb, 0.2f, 10, new Color(92, 24, 46, 255));
                UI.TextIn(mb, "BOOM!", 42, Theme.Red);
            }

            UI.TextCenter("SPACE: bet / cash out     R: random pick", mcx, 1018, 18, Theme.Muted);
        }

        // -------------------------------------------------------------------- table
        private static void DrawTable()
        {
            Rectangle t = Layout.Table;
            Raylib.DrawRectangleRounded(t, 0.04f, 10, new Color(18, 15, 40, 255));
            UI.RoundedOutline(t, 0.04f, 3f, Theme.PanelEdge);

            foreach (Card c in BoardManager.Cards) DrawCard(c);
        }

        private static void DrawCard(Card c)
        {
            // Flip animation: the card narrows to a line (back side), then widens (front side).
            float sx = MathF.Abs(MathF.Cos(c.Flip * MathF.PI));
            float pop = 1f + 0.10f * MathF.Sin(c.Flip * MathF.PI);

            float cx = c.Bounds.X + c.Bounds.Width / 2f;
            float cy = c.Bounds.Y + c.Bounds.Height / 2f - c.Hover * 6f;
            float w = MathF.Max(3f, c.Bounds.Width * sx * pop);
            float h = c.Bounds.Height * pop;
            Rectangle r = new Rectangle(cx - w / 2f, cy - h / 2f, w, h);

            Raylib.DrawRectangleRounded(new Rectangle(r.X + 3, r.Y + 7.5f, r.Width, r.Height), 0.12f, 8, new Color(0, 0, 0, 80));

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
                UI.TextIn(r, "?", 60, Theme.Gold);
            }

            if (c.Hover > 0.01f) UI.RoundedOutline(r, 0.12f, 3f, Theme.Alpha(Theme.Cyan, c.Hover));
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
                    Raylib.DrawCircleV(center, 54f - i * 13.5f, Theme.Alpha(edge, 0.10f * dim));
            }

            Texture2D t = bomb ? AssetManager.Bomb : AssetManager.Gem;
            if (t.Id != 0)
            {
                float baseW = bomb ? 93f : 90f;
                float iw = MathF.Max(1f, baseW * sx * pop);
                float ih = baseW * t.Height / t.Width * pop;
                Raylib.DrawTexturePro(t, new Rectangle(0, 0, t.Width, t.Height),
                    new Rectangle(center.X - iw / 2f, center.Y - ih / 2f, iw, ih),
                    Vector2.Zero, 0f, Theme.Alpha(Color.White, dim));
            }
            else
            {
                UI.TextIn(r, bomb ? "BOMB" : "GEM", 24, Theme.Text);
            }

            UI.RoundedOutline(r, 0.12f, 3.75f, Theme.Alpha(edge, dim));
            if (!c.IsRevealed) Raylib.DrawRectangleRounded(r, 0.12f, 8, new Color(8, 6, 20, 110));
        }

        // ------------------------------------------------------------- right panel
        private static void DrawRightPanel()
        {
            Rectangle p = Layout.RightPanel;
            Panel(p);

            float x = p.X + 24f;
            float right = p.X + p.Width - 24f;

            UI.Text("Live Stats", x, 135, 30, Theme.Text);

            StatBox(new Rectangle(x, 192, 195, 84), "PROFIT", Signed(Stats.Profit), Stats.Profit >= 0 ? Theme.Green : Theme.Red);
            StatBox(new Rectangle(x + 207, 192, 195, 84), "WAGERED", $"${Stats.Wagered:F2}", Theme.Text);
            StatBox(new Rectangle(x, 288, 195, 84), "WINS", $"{Stats.Wins}", Theme.Green);
            StatBox(new Rectangle(x + 207, 288, 195, 84), "LOSSES", $"{Stats.Losses}", Theme.Red);

            UI.Text("Recent Rounds", x, 399, 27, Theme.Text);
            UI.Text("BET", x, 447, 18, Theme.Muted);
            UI.Text("MULT", x + 150, 447, 18, Theme.Muted);
            UI.TextRight("PROFIT", right, 447, 18, Theme.Muted);

            float y = 483f;
            for (int i = 0; i < Stats.History.Count; i++)
            {
                RoundResult rr = Stats.History[i];
                if (i % 2 == 0)
                    Raylib.DrawRectangleRounded(new Rectangle(x - 9, y - 3, 420, 42), 0.3f, 6, Theme.Alpha(Theme.Field, 0.7f));

                bool won = rr.Payout > rr.Bet;
                UI.Text($"${rr.Bet:F2}", x, y + 4.5f, 22.5f, Theme.Text);
                UI.Text($"{rr.Multiplier:F2}x", x + 150, y + 4.5f, 22.5f, won ? Theme.Gold : Theme.Muted);
                UI.TextRight(Signed(rr.Profit), right, y + 4.5f, 22.5f, won ? Theme.Green : Theme.Red);
                y += 45f;
            }

            if (Stats.History.Count == 0)
                UI.Text("Place a bet to start!", x, 489, 22.5f, Theme.Muted);
        }

        private static void StatBox(Rectangle r, string label, string value, Color valueColor)
        {
            Raylib.DrawRectangleRounded(r, 0.18f, 8, Theme.Field);
            UI.RoundedOutline(r, 0.18f, 3f, Theme.FieldEdge);
            UI.Text(label, r.X + 18, r.Y + 12, 18, Theme.Muted);
            UI.Text(value, r.X + 18, r.Y + 39, 27, valueColor);
        }

        private static string Signed(double v)
        {
            v = Math.Round(v, 2);
            return v >= 0 ? $"+${v:F2}" : $"-${Math.Abs(v):F2}";
        }
    }
}
