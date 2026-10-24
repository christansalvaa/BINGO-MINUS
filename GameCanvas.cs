// Scales the to fullscreen display
// DO NOT EDIT

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class GameCanvas
{
    public const int Width = 960;
    public const int Height = 640;

    public static void Begin()
    {
        Raylib.BeginMode2D(GetCamera());
    }

    public static void End()
    {
        Raylib.EndMode2D();
    }

    public static Vector2 GetMousePosition()
    {
        return Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), GetCamera());
    }

    private static Camera2D GetCamera()
    {
        float scale = Math.Min(
            (float)Raylib.GetScreenWidth() / Width,
            (float)Raylib.GetScreenHeight() / Height);

        return new Camera2D
        {
            Offset = new Vector2(
                Raylib.GetScreenWidth() / 2f,
                Raylib.GetScreenHeight() / 2f),
            Target = new Vector2(Width / 2f, Height / 2f),
            Rotation = 0,
            Zoom = scale
        };
    }
}
