using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>Text + small drawing helpers (fonts are loaded at 4 sizes so text stays sharp).</summary>
    public static class UI
    {
        private static readonly int[] Sizes = { 16, 24, 40, 64 };
        private static readonly Font[] Fonts = new Font[Sizes.Length];
        private static bool _loaded;

        public static void LoadFonts(string path)
        {
            if (!File.Exists(path)) return;   // falls back to Raylib's default font

            for (int i = 0; i < Sizes.Length; i++)
            {
                Font f = Raylib.LoadFontEx(path, Sizes[i], null, 0);
                if (f.Texture.Id != 0) Raylib.SetTextureFilter(f.Texture, TextureFilter.Bilinear);
                Fonts[i] = f;
            }
            _loaded = true;
        }

        public static void UnloadFonts()
        {
            if (!_loaded) return;
            for (int i = 0; i < Fonts.Length; i++) Raylib.UnloadFont(Fonts[i]);
            _loaded = false;
        }

        private static Font Pick(float size)
        {
            if (!_loaded) return Raylib.GetFontDefault();
            for (int i = 0; i < Sizes.Length; i++)
                if (Sizes[i] >= size) return Fonts[i];
            return Fonts[Sizes.Length - 1];
        }

        private static float Spacing(float size) => MathF.Max(0.5f, size * 0.03f);

        public static Vector2 Measure(string text, float size)
        {
            return Raylib.MeasureTextEx(Pick(size), text, size, Spacing(size));
        }

        public static void Text(string text, float x, float y, float size, Color color)
        {
            Raylib.DrawTextEx(Pick(size), text, new Vector2(MathF.Round(x), MathF.Round(y)), size, Spacing(size), color);
        }

        public static void TextCenter(string text, float centerX, float y, float size, Color color)
        {
            Text(text, centerX - Measure(text, size).X / 2f, y, size, color);
        }

        public static void TextRight(string text, float rightX, float y, float size, Color color)
        {
            Text(text, rightX - Measure(text, size).X, y, size, color);
        }

        /// <summary>Text centred horizontally and vertically inside a rectangle.</summary>
        public static void TextIn(Rectangle r, string text, float size, Color color)
        {
            Vector2 m = Measure(text, size);
            Text(text, r.X + (r.Width - m.X) / 2f, r.Y + (r.Height - m.Y) / 2f, size, color);
        }

        public static void RoundedOutline(Rectangle r, float roundness, float thickness, Color color)
        {
            Raylib.DrawRectangleRoundedLinesEx(r, roundness, 8, thickness, color);
        }
    }
}
