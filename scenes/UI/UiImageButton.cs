using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class UiImageButton
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    public static Texture2D LoadTexture(string relativePath)
    {
        if (TextureCache.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string fullPath = Path.Combine(baseDir, relativePath);
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        }
        if (!File.Exists(fullPath))
        {
            fullPath = relativePath;
        }

        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"[UiImageButton] Warning: Texture not found at '{fullPath}'");
            return default;
        }

        var tex = Raylib.LoadTexture(fullPath);
        if (tex.Id != 0)
        {
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
            TextureCache[relativePath] = tex;
        }
        else
        {
            Console.WriteLine($"[UiImageButton] Warning: Failed to decode texture '{fullPath}'");
        }

        return tex;
    }

    public static void UnloadAll()
    {
        foreach (var kvp in TextureCache)
        {
            if (kvp.Value.Id != 0)
            {
                Raylib.UnloadTexture(kvp.Value);
            }
        }
        TextureCache.Clear();
    }

    public static bool IsHovered(Rectangle bounds)
    {
        Vector2 mousePos = GameCanvas.GetMousePosition();
        return Raylib.CheckCollisionPointRec(mousePos, bounds);
    }

    public static bool WasClicked(Rectangle bounds)
    {
        return IsHovered(bounds) && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    public static void Draw(Texture2D texture, Rectangle destRect, bool enabled = true)
    {
        if (texture.Id == 0)
        {
            // Fallback placeholder rectangle if texture missing
            Raylib.DrawRectangleRec(destRect, new Color(40, 50, 70, 255));
            Raylib.DrawRectangleLinesEx(destRect, 1, Color.Gold);
            return;
        }

        bool hovered = enabled && IsHovered(destRect);
        Color tint = !enabled
            ? new Color(130, 130, 130, 180)
            : hovered
                ? Color.White
                : new Color(225, 225, 225, 255);

        Rectangle srcRect = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, srcRect, destRect, Vector2.Zero, 0f, tint);

        if (hovered)
        {
            // Subtle glossy highlight border
            Raylib.DrawRectangleLinesEx(destRect, 2, new Color(255, 215, 0, 160));
        }
    }

    public static void DrawCard(Texture2D texture, Rectangle destRect, bool enabled = true)
    {
        if (texture.Id == 0)
        {
            Raylib.DrawRectangleRec(destRect, new Color(25, 30, 45, 255));
            Raylib.DrawRectangleLinesEx(destRect, 2, Color.Gold);
            return;
        }

        bool hovered = enabled && IsHovered(destRect);

        Rectangle drawRect = destRect;
        if (hovered)
        {
            // Subtle lift/scale up by 3px on hover
            drawRect = new Rectangle(
                destRect.X - 3,
                destRect.Y - 4,
                destRect.Width + 6,
                destRect.Height + 8);
        }

        Color tint = hovered ? Color.White : new Color(220, 220, 225, 255);
        Rectangle srcRect = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, srcRect, drawRect, Vector2.Zero, 0f, tint);

        if (hovered)
        {
            // Golden card glow border
            Raylib.DrawRectangleLinesEx(drawRect, 3, new Color(255, 195, 0, 220));
        }
    }
}

