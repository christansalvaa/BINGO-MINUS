// Tiny helpers for OPTIONAL artwork in the betting minigames (Roulette, Color Game).
// Every visual asks for a texture file first; if the file is missing the game falls back to
// the shapes it draws in code, so artwork can be dropped in one file at a time.
// Textures are loaded through UiImageButton (cached, unloaded by Program on exit).

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class CasinoArt
{
    // Returns default(Texture2D) (Id == 0) if the file does not exist. Call once, not every frame.
    public static Texture2D Load(string relativePath) => UiImageButton.LoadTexture(relativePath);

    public static bool Has(Texture2D texture) => texture.Id != 0;

    // Stretches the texture over `dest`.
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

    // Draws a square texture centred on `center`, optionally rotated clockwise (degrees).
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

    // Button with a blank texture plate + a label drawn by code (labels change, e.g. SPIN / NEW ROUND).
    // Falls back to the shared flat UiButton when there is no texture.
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

    // Panel background: texture if present, otherwise a flat dark box with a thin outline.
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
