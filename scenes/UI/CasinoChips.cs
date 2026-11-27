// Shared betting-chip visuals and helpers (used by Roulette and Color Game).
// Chips are drawn in code so they work without any art files; swap in textures later if wanted.

using System.Globalization;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class CasinoChips
{
    public static readonly decimal[] Values = [1m, 5m, 25m, 50m, 100m, 500m];

    public static decimal MinChip => Values[0];

    // Optional artwork: scenes/UI/assets/casino/chip_<value>.png (blank chip, the amount is printed by code).
    private static readonly Texture2D[] ChipTextures = new Texture2D[Values.Length];

    public static void LoadTextures()
    {
        for (int i = 0; i < Values.Length; i++)
        {
            ChipTextures[i] = CasinoArt.Load($"scenes/UI/assets/casino/chip_{Values[i]:0}.png");
        }
    }

    // Index (into Values) of the biggest chip that fits inside the amount.
    private static int TierIndex(decimal amount)
    {
        int tier = 0;
        for (int i = 0; i < Values.Length; i++)
        {
            if (amount >= Values[i]) tier = i;
        }

        return tier;
    }

    // Picks the colour of the biggest chip that fits inside the amount (used for stacked table bets).
    public static Color ColorFor(decimal amount)
    {
        decimal tier = Values[0];
        foreach (decimal chip in Values)
        {
            if (amount >= chip) tier = chip;
        }

        return tier switch
        {
            >= 500m => new Color(140, 60, 200, 255),
            >= 100m => new Color(35, 35, 48, 255),
            >= 50m => new Color(40, 110, 220, 255),
            >= 25m => new Color(30, 160, 80, 255),
            >= 5m => new Color(210, 50, 50, 255),
            _ => new Color(235, 235, 240, 255)
        };
    }

    public static string Format(decimal amount)
    {
        if (amount >= 1_000_000m)
        {
            return (amount / 1_000_000m).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        }

        if (amount >= 1_000m)
        {
            return (amount / 1_000m).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        }

        return amount % 1m == 0m
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Draws one chip. `value` decides the colour; `label` overrides the printed text (defaults to value).
    public static void DrawChip(Vector2 center, float radius, decimal value, string? label = null, bool selected = false)
    {
        Texture2D art = ChipTextures[TierIndex(value)];
        if (CasinoArt.Has(art))
        {
            DrawTexturedChip(art, center, radius, label ?? Format(value), selected);
            return;
        }

        Color baseColor = ColorFor(value);
        bool lightChip = baseColor.R > 200 && baseColor.G > 200;

        // Soft shadow
        Raylib.DrawCircleV(new Vector2(center.X + 2f, center.Y + 3f), radius, new Color(0, 0, 0, 90));

        // Body + the dashed "edge spots" every real chip has
        Raylib.DrawCircleV(center, radius, baseColor);
        for (int i = 0; i < 8; i++)
        {
            float start = i * 45f;
            Raylib.DrawRing(center, radius * 0.80f, radius, start, start + 22.5f, 6,
                lightChip ? new Color(60, 60, 70, 255) : new Color(255, 255, 255, 235));
        }

        // Inner disc
        Color inner = new(
            (int)(baseColor.R * 0.78f),
            (int)(baseColor.G * 0.78f),
            (int)(baseColor.B * 0.78f),
            255);
        Raylib.DrawCircleV(center, radius * 0.68f, inner);
        Raylib.DrawRing(center, radius * 0.66f, radius * 0.70f, 0f, 360f, 24,
            lightChip ? new Color(60, 60, 70, 200) : new Color(255, 255, 255, 200));

        string text = label ?? Format(value);
        float fontSize = radius * (text.Length >= 4 ? 0.55f : text.Length == 3 ? 0.66f : 0.80f);
        fontSize = Math.Max(fontSize, 9f);
        Vector2 size = UiFont.MeasureText(text, fontSize, isBold: true);
        UiFont.DrawText(
            text,
            center.X - size.X / 2f,
            center.Y - size.Y / 2f,
            fontSize,
            lightChip ? new Color(30, 30, 40, 255) : Color.White,
            isBold: true);

        if (selected)
        {
            Raylib.DrawRing(center, radius + 3f, radius + 6f, 0f, 360f, 36, Color.Gold);
        }
    }

    private static void DrawTexturedChip(Texture2D art, Vector2 center, float radius, string text, bool selected)
    {
        Raylib.DrawCircleV(new Vector2(center.X + 2f, center.Y + 3f), radius, new Color(0, 0, 0, 90));
        CasinoArt.DrawCentered(art, center, radius * 2f);

        // White text with a dark outline stays readable on any chip artwork.
        float fontSize = Math.Max(radius * (text.Length >= 4 ? 0.55f : text.Length == 3 ? 0.66f : 0.80f), 9f);
        Vector2 size = UiFont.MeasureText(text, fontSize, isBold: true);
        float x = center.X - size.X / 2f;
        float y = center.Y - size.Y / 2f;
        Color outline = new(0, 0, 0, 220);
        UiFont.DrawText(text, x - 1.2f, y, fontSize, outline, isBold: true);
        UiFont.DrawText(text, x + 1.2f, y, fontSize, outline, isBold: true);
        UiFont.DrawText(text, x, y - 1.2f, fontSize, outline, isBold: true);
        UiFont.DrawText(text, x, y + 1.2f, fontSize, outline, isBold: true);
        UiFont.DrawText(text, x, y, fontSize, Color.White, isBold: true);

        if (selected)
        {
            Raylib.DrawRing(center, radius + 3f, radius + 6f, 0f, 360f, 36, Color.Gold);
        }
    }
}
