using System;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class UiFont
{
    private static Font _regular;
    private static Font _bold;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        string regularPath = ResolvePath(baseDir, "scenes/UI/Assets/Fonts/segoeui.ttf");
        string boldPath = ResolvePath(baseDir, "scenes/UI/Assets/Fonts/segoeuib.ttf");

        if (File.Exists(regularPath))
        {
            _regular = Raylib.LoadFontEx(regularPath, 64, null, 0);
            Raylib.SetTextureFilter(_regular.Texture, TextureFilter.Bilinear);
        }

        if (File.Exists(boldPath))
        {
            _bold = Raylib.LoadFontEx(boldPath, 64, null, 0);
            Raylib.SetTextureFilter(_bold.Texture, TextureFilter.Bilinear);
        }

        _initialized = true;
    }

    private static string ResolvePath(string baseDir, string relativePath)
    {
        string p = Path.Combine(baseDir, relativePath);
        if (File.Exists(p)) return p;

        p = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (File.Exists(p)) return p;

        return relativePath;
    }

    public static void DrawText(string text, float x, float y, float fontSize, Color color, bool isBold = false, float spacing = 1f)
    {
        Initialize();

        Font font = isBold ? (_bold.Texture.Id != 0 ? _bold : _regular) : (_regular.Texture.Id != 0 ? _regular : _bold);

        if (font.Texture.Id != 0)
        {
            Raylib.DrawTextEx(font, text, new Vector2(x, y), fontSize, spacing, color);
        }
        else
        {
            Raylib.DrawText(text, (int)x, (int)y, (int)fontSize, color);
        }
    }

    public static void DrawCenteredText(string text, float y, float fontSize, Color color, bool isBold = false, float spacing = 1f)
    {
        float textWidth = MeasureText(text, fontSize, isBold, spacing).X;
        float x = (GameCanvas.Width - textWidth) / 2f;
        DrawText(text, x, y, fontSize, color, isBold, spacing);
    }

    public static void DrawCenteredInRect(string text, Rectangle rect, float fontSize, Color color, bool isBold = false, float spacing = 1f)
    {
        Vector2 size = MeasureText(text, fontSize, isBold, spacing);
        float x = rect.X + (rect.Width - size.X) / 2f;
        float y = rect.Y + (rect.Height - size.Y) / 2f;
        DrawText(text, x, y, fontSize, color, isBold, spacing);
    }

    public static float DrawWrappedText(string text, float x, float y, float maxWidth, float fontSize, Color color, bool isBold = false, float lineSpacing = 4f, float spacing = 1f)
    {
        Initialize();
        Font font = isBold ? (_bold.Texture.Id != 0 ? _bold : _regular) : (_regular.Texture.Id != 0 ? _regular : _bold);

        string[] paragraphs = text.Split('\n');
        float currentY = y;
        float fontHeight = fontSize;

        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                currentY += fontHeight * 0.6f;
                continue;
            }

            string[] words = paragraph.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float testWidth = font.Texture.Id != 0
                    ? Raylib.MeasureTextEx(font, testLine, fontSize, spacing).X
                    : Raylib.MeasureText(testLine, (int)fontSize);

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    DrawText(currentLine, x, currentY, fontSize, color, isBold, spacing);
                    currentY += fontHeight + lineSpacing;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                DrawText(currentLine, x, currentY, fontSize, color, isBold, spacing);
                currentY += fontHeight + lineSpacing;
            }
        }

        return currentY - y;
    }

    public static Vector2 MeasureText(string text, float fontSize, bool isBold = false, float spacing = 1f)
    {
        Initialize();

        Font font = isBold ? (_bold.Texture.Id != 0 ? _bold : _regular) : (_regular.Texture.Id != 0 ? _regular : _bold);

        if (font.Texture.Id != 0)
        {
            return Raylib.MeasureTextEx(font, text, fontSize, spacing);
        }

        return new Vector2(Raylib.MeasureText(text, (int)fontSize), fontSize);
    }

    public static void Unload()
    {
        if (_regular.Texture.Id != 0) Raylib.UnloadFont(_regular);
        if (_bold.Texture.Id != 0) Raylib.UnloadFont(_bold);
        _initialized = false;
    }
}

