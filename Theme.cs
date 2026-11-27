using Raylib_cs;

namespace MinesCasino
{
    /// <summary>Window size + every colour used by the game, in one place.</summary>
    public static class Theme
    {
        public const int WindowWidth = 1200;
        public const int WindowHeight = 720;

        public static readonly Color BgTop       = new(27, 23, 56, 255);
        public static readonly Color BgBottom    = new(13, 11, 30, 255);
        public static readonly Color Panel       = new(33, 29, 68, 255);
        public static readonly Color PanelEdge   = new(62, 56, 112, 255);
        public static readonly Color Field       = new(20, 17, 44, 255);
        public static readonly Color FieldOff    = new(26, 23, 52, 255);
        public static readonly Color FieldEdge   = new(74, 67, 130, 255);
        public static readonly Color Button      = new(54, 48, 100, 255);
        public static readonly Color ButtonHover = new(76, 68, 132, 255);
        public static readonly Color ButtonOff   = new(36, 32, 70, 255);
        public static readonly Color Accent      = new(112, 72, 230, 255);

        public static readonly Color Text   = new(240, 238, 255, 255);
        public static readonly Color Muted  = new(146, 140, 192, 255);
        public static readonly Color Green  = new(34, 197, 94, 255);
        public static readonly Color GreenHover = new(74, 222, 128, 255);
        public static readonly Color GreenOff   = new(24, 92, 56, 255);
        public static readonly Color Gold   = new(252, 196, 25, 255);
        public static readonly Color Orange = new(249, 115, 22, 255);
        public static readonly Color Red    = new(244, 63, 94, 255);
        public static readonly Color Cyan   = new(56, 189, 248, 255);

        /// <summary>Same colour with a different opacity (0..1).</summary>
        public static Color Alpha(Color c, float a)
        {
            a = Math.Clamp(a, 0f, 1f);
            return new Color((int)c.R, (int)c.G, (int)c.B, (int)(a * 255f));
        }
    }
}
