using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

// ==================== SCREEN CONSTANTS & INPUT ====================

internal static class GameCanvas
{
    public const int Width = 1920;
    public const int Height = 1080;

    public static Vector2 GetMousePosition() => Raylib.GetMousePosition();
}

// ==================== BUTTONS & 9-PATCH ====================

internal static class UiButton
{
    private static Texture2D _btnTex;
    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _btnTex = UiImageButton.LoadTexture("scenes/UI/assets/btn_resizable.png");
        _loaded = true;
    }

    public static void DrawNinePatch(Rectangle bounds, Color tint, Color? borderColor = null, float borderThickness = 2f)
    {
        EnsureLoaded();
        if (_btnTex.Id != 0)
        {
            var patch = new NPatchInfo
            {
                Source = new Rectangle(0, 0, _btnTex.Width, _btnTex.Height),
                Left = 12,
                Top = 12,
                Right = 12,
                Bottom = 12,
                Layout = NPatchLayout.NinePatch
            };
            Raylib.DrawTextureNPatch(_btnTex, patch, bounds, Vector2.Zero, 0f, tint);
        }
        else
        {
            Raylib.DrawRectangleRec(bounds, tint);
        }

        if (borderColor is Color border)
        {
            Raylib.DrawRectangleLinesEx(bounds, borderThickness, border);
        }
    }

    public static void Draw(Rectangle bounds, string label, Color fillColor)
    {
        EnsureLoaded();
        Vector2 mousePosition = GameCanvas.GetMousePosition();
        bool isHovered = Raylib.CheckCollisionPointRec(mousePosition, bounds);

        Color color = isHovered
            ? new Color(
                (byte)Math.Min(fillColor.R + 20, 255),
                (byte)Math.Min(fillColor.G + 20, 255),
                (byte)Math.Min(fillColor.B + 20, 255),
                fillColor.A)
            : fillColor;

        DrawNinePatch(bounds, color, new Color(255, 255, 255, 45), 3f);

        int fontSize = 30;
        int textX = (int)(bounds.X + (bounds.Width - Raylib.MeasureText(label, fontSize)) / 2);
        int textY = (int)(bounds.Y + (bounds.Height - fontSize) / 2);
        Raylib.DrawText(label, textX, textY, fontSize, Color.RayWhite);
    }

    public static bool WasClicked(Rectangle bounds)
    {
        Vector2 mousePosition = GameCanvas.GetMousePosition();
        return Raylib.CheckCollisionPointRec(mousePosition, bounds) && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }
}

// ==================== TEXTURE CACHE, HOVER & CARDS ====================

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
        return Raylib.CheckCollisionPointRec(GameCanvas.GetMousePosition(), bounds);
    }

    public static bool WasClicked(Rectangle bounds)
    {
        return IsHovered(bounds) && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    public static void Draw(Texture2D texture, Rectangle destRect, bool enabled = true)
    {
        if (texture.Id == 0)
        {
            Raylib.DrawRectangleRec(destRect, new Color(40, 50, 70, 255));
            Raylib.DrawRectangleLinesEx(destRect, 1, Color.Gold);
            return;
        }

        bool hovered = enabled && IsHovered(destRect);
        Color tint = !enabled
            ? new Color(130, 130, 130, 180)
            : hovered
                ? new Color(210, 210, 210, 255)
                : Color.White;

        Rectangle srcRect = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, srcRect, destRect, Vector2.Zero, 0f, tint);
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
        Rectangle drawRect = hovered
            ? new Rectangle(destRect.X - 4.5f, destRect.Y - 6f, destRect.Width + 9f, destRect.Height + 12f)
            : destRect;

        Color tint = hovered ? new Color(210, 210, 210, 255) : Color.White;
        Rectangle srcRect = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, srcRect, drawRect, Vector2.Zero, 0f, tint);
    }
}

// ==================== OPTIONAL CASINO ARTWORK ====================

internal static class CasinoArt
{
    public static Texture2D Load(string relativePath) => UiImageButton.LoadTexture(relativePath);

    public static bool Has(Texture2D texture) => texture.Id != 0;

    public static void Draw(Texture2D texture, Rectangle dest, Color? tint = null)
    {
        Raylib.DrawTexturePro(
            texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            dest,
            Vector2.Zero,
            0f,
            tint ?? Color.White);
    }

    public static void DrawCentered(Texture2D texture, Vector2 center, float size, float rotationDegrees = 0f, Color? tint = null)
    {
        Raylib.DrawTexturePro(
            texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            new Rectangle(center.X, center.Y, size, size),
            new Vector2(size / 2f, size / 2f),
            rotationDegrees,
            tint ?? Color.White);
    }

    public static void DrawButton(Texture2D plate, Rectangle bounds, string label, bool enabled, Color fallbackFill, float fontSize = 20f)
    {
        if (!Has(plate))
        {
            UiButton.Draw(bounds, label, fallbackFill);
            return;
        }

        UiImageButton.Draw(plate, bounds, enabled);
        UiFont.DrawCenteredInRect(label, bounds, fontSize, enabled ? Color.White : new Color(170, 170, 170, 255), isBold: true);
    }

    public static void DrawPanel(Texture2D texture, Rectangle bounds, Color fill, Color outline)
    {
        if (Has(texture))
        {
            Draw(texture, bounds);
            return;
        }

        Raylib.DrawRectangleRec(bounds, fill);
        Raylib.DrawRectangleLinesEx(bounds, 1, outline);
    }
}

// ==================== SHARED MINIGAME HEADER ====================

internal static class BingoPlusHeader
{
    public static bool Draw(string gameTitle, CurrencyManager currency)
    {
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, 93, Color.White);
        Raylib.DrawLine(0, 92, GameCanvas.Width, 92, new Color(220, 102, 38, 180));

        Rectangle backRect = new(30, 17, 195, 60);
        bool backHovered = UiImageButton.IsHovered(backRect);
        bool backClicked = UiImageButton.WasClicked(backRect) || Raylib.IsKeyPressed(KeyboardKey.Escape);
        Rectangle drawBack = backHovered
            ? new Rectangle(backRect.X - 3f, backRect.Y - 4f, backRect.Width + 6f, backRect.Height + 8f)
            : backRect;
        UiButton.DrawNinePatch(
            drawBack,
            backHovered ? new Color(210, 210, 210, 255) : Color.White);
        UiFont.DrawCenteredInRect("< BACK", drawBack, 22, Color.White, isBold: true);

        UiFont.DrawCenteredText(gameTitle, 26, 33, new Color(30, 34, 42, 255), isBold: true);

        Rectangle balanceRect = new(GameCanvas.Width - 345, 17, 315, 60);
        Raylib.DrawRectangleRec(balanceRect, new Color(248, 248, 250, 255));
        Raylib.DrawRectangleLinesEx(balanceRect, 1.5f, new Color(220, 102, 38, 200));

        Raylib.DrawCircle((int)balanceRect.X + 33, 47, 16.5f, new Color(220, 102, 38, 255));
        UiFont.DrawText("$", (int)balanceRect.X + 27, 35, 21, Color.White, isBold: true);

        string balStr = $"{currency.Balance:N2}";
        UiFont.DrawText("COINS", (int)balanceRect.X + 66, 23, 15, new Color(100, 105, 115, 255), isBold: true);
        UiFont.DrawText(balStr, (int)balanceRect.X + 66, 39, 24, new Color(200, 82, 24, 255), isBold: true);

        return backClicked;
    }
}
