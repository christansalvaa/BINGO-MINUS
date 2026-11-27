using System.Numerics;
using Raylib_cs;

namespace MinesCasino
{
    /// <summary>Splash art + a progress bar at the bottom centre.</summary>
    public static class LoadingScreen
    {
        // The bar never fills faster than this, so the player can actually see it.
        private const float MinDuration = 2.6f;
        private const float HoldAtEnd = 0.35f;

        private static int _stepIndex;
        private static float _hold;

        public static float Progress { get; private set; }
        public static string Label { get; private set; } = "Starting...";
        public static bool Done { get; private set; }

        public static void Update(float dt)
        {
            int total = AssetManager.Steps.Count;

            // One REAL load step per frame, so the bar reflects actual work.
            if (_stepIndex < total)
            {
                AssetManager.Steps[_stepIndex].Load();
                _stepIndex++;
            }

            float target = total == 0 ? 1f : (float)_stepIndex / total;
            Progress = MathF.Min(target, Progress + dt / MinDuration);

            if (total > 0)
            {
                int shown = Math.Min(total - 1, (int)(Progress * total));
                Label = Progress >= 1f ? "Ready!" : AssetManager.Steps[shown].Label;
            }

            if (Progress >= 1f)
            {
                _hold += dt;
                if (_hold >= HoldAtEnd) Done = true;
            }
        }

        public static void Draw()
        {
            int w = Theme.WindowWidth;
            int h = Theme.WindowHeight;

            Raylib.ClearBackground(new Color(30, 26, 62, 255));

            Texture2D bg = AssetManager.LoadingBg;
            if (bg.Id != 0)
            {
                // "cover" scaling: fill the window, crop whatever sticks out
                float s = MathF.Max(w / (float)bg.Width, h / (float)bg.Height);
                float dw = bg.Width * s;
                float dh = bg.Height * s;
                Raylib.DrawTexturePro(bg,
                    new Rectangle(0, 0, bg.Width, bg.Height),
                    new Rectangle((w - dw) / 2f, (h - dh) / 2f, dw, dh),
                    Vector2.Zero, 0f, Color.White);
            }
            else
            {
                UI.TextCenter("MINES CASINO", w / 2f, h / 2f - 40, 64, Theme.Gold);
            }

            // dark fade so the bar is readable on any artwork
            Raylib.DrawRectangleGradientV(0, h - 260, w, 260, new Color(8, 6, 20, 0), new Color(8, 6, 20, 235));

            // ---- progress bar (bottom centre) ----
            float barW = 480f, barH = 20f;
            float bx = (w - barW) / 2f;
            float by = h - 84f;
            Rectangle track = new Rectangle(bx, by, barW, barH);

            Raylib.DrawRectangleRounded(new Rectangle(bx - 3, by - 3, barW + 6, barH + 6), 0.5f, 12, new Color(0, 0, 0, 150));
            Raylib.DrawRectangleRounded(track, 0.5f, 12, new Color(34, 29, 70, 255));

            float fillW = (barW - 6f) * Progress;
            if (fillW > 6f)
            {
                Rectangle fill = new Rectangle(bx + 3, by + 3, fillW, barH - 6);
                Raylib.DrawRectangleRounded(fill, 0.5f, 12, Theme.Orange);
                Raylib.DrawRectangleRounded(new Rectangle(fill.X, fill.Y, fill.Width, fill.Height * 0.5f), 0.5f, 12, Theme.Alpha(Theme.Gold, 0.55f));

                // moving diagonal stripes, clipped to the fill
                Raylib.BeginScissorMode((int)fill.X, (int)fill.Y, (int)fill.Width, (int)fill.Height);
                float offset = (float)(Raylib.GetTime() * 40.0 % 24.0);
                for (float x = fill.X - 24f + offset; x < fill.X + fill.Width + 24f; x += 24f)
                {
                    Raylib.DrawLineEx(new Vector2(x, fill.Y + fill.Height), new Vector2(x + fill.Height, fill.Y), 6f, new Color(255, 255, 255, 40));
                }
                Raylib.EndScissorMode();
            }

            UI.RoundedOutline(track, 0.5f, 2f, new Color(120, 105, 200, 255));

            UI.TextCenter(Label, w / 2f, by - 36f, 18, Theme.Text);
            UI.TextCenter($"{(int)(Progress * 100f)}%", w / 2f, by + barH + 6f, 18, Theme.Gold);
            UI.TextCenter("Play-money demo  -  no real gambling", w / 2f, h - 26f, 13, Theme.Muted);
        }
    }
}
