// shared clickable buttons, can be reused

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class UiButton
{
    public static void Draw(Rectangle bounds, string label, Color fillColor)
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        bool isHovered = Raylib.CheckCollisionPointRec(mousePosition, bounds);

        Color color = isHovered
            ? new Color(
                (byte)Math.Min(fillColor.R + 20, 255),
                (byte)Math.Min(fillColor.G + 20, 255),
                (byte)Math.Min(fillColor.B + 20, 255),
                fillColor.A)
            : fillColor;

        Raylib.DrawRectangleRec(bounds, color);
        Raylib.DrawRectangleLinesEx(bounds, 2, new Color(255, 255, 255, 45));

        int fontSize = 20;
        int textX = (int)(bounds.X + (bounds.Width - Raylib.MeasureText(label, fontSize)) / 2);
        int textY = (int)(bounds.Y + (bounds.Height - fontSize) / 2);
        Raylib.DrawText(label, textX, textY, fontSize, Color.RayWhite);
    }

    public static bool WasClicked(Rectangle bounds)
    {
        Vector2 mousePosition = GameCanvas.GetMousePosition();
        bool isHovered = Raylib.CheckCollisionPointRec(mousePosition, bounds);
        return isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }
}
