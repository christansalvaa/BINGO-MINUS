using System;
using Raylib_cs;

namespace BINGO_MINUS;

public enum PlinkoRisk
{
    Low,
    Medium,
    High
}

internal static class PlinkoTables
{
    public static readonly int[] SupportedRows = [8, 12, 16];

    // Authentic ~99% RTP binomial Plinko multiplier tables
    private static readonly decimal[] Low8 = [5.6m, 2.1m, 1.1m, 1.0m, 0.5m, 1.0m, 1.1m, 2.1m, 5.6m];
    private static readonly decimal[] Med8 = [13m, 3m, 1.3m, 0.7m, 0.4m, 0.7m, 1.3m, 3m, 13m];
    private static readonly decimal[] High8 = [29m, 4m, 1.5m, 0.3m, 0.2m, 0.3m, 1.5m, 4m, 29m];

    private static readonly decimal[] Low12 = [10m, 3m, 1.6m, 1.4m, 1.1m, 1.0m, 0.5m, 1.0m, 1.1m, 1.4m, 1.6m, 3m, 10m];
    private static readonly decimal[] Med12 = [33m, 11m, 4m, 2m, 1.1m, 0.6m, 0.3m, 0.6m, 1.1m, 2m, 4m, 11m, 33m];
    private static readonly decimal[] High12 = [170m, 24m, 8.1m, 2m, 0.7m, 0.2m, 0.2m, 0.2m, 0.7m, 2m, 8.1m, 24m, 170m];

    private static readonly decimal[] Low16 = [16m, 9m, 2m, 1.4m, 1.4m, 1.2m, 1.1m, 1.0m, 0.5m, 1.0m, 1.1m, 1.2m, 1.4m, 1.4m, 2m, 9m, 16m];
    private static readonly decimal[] Med16 = [110m, 41m, 10m, 5m, 3m, 1.5m, 1.0m, 0.5m, 0.3m, 0.5m, 1.0m, 1.5m, 3m, 5m, 10m, 41m, 110m];
    private static readonly decimal[] High16 = [1000m, 130m, 26m, 9m, 4m, 2m, 0.2m, 0.2m, 0.2m, 0.2m, 0.2m, 2m, 4m, 9m, 26m, 130m, 1000m];

    public static decimal[] GetMultipliers(int rows, PlinkoRisk risk)
    {
        return (rows, risk) switch
        {
            (8, PlinkoRisk.Low) => Low8,
            (8, PlinkoRisk.Medium) => Med8,
            (8, PlinkoRisk.High) => High8,
            (12, PlinkoRisk.Low) => Low12,
            (12, PlinkoRisk.Medium) => Med12,
            (12, PlinkoRisk.High) => High12,
            (16, PlinkoRisk.Low) => Low16,
            (16, PlinkoRisk.Medium) => Med16,
            (16, PlinkoRisk.High) => High16,
            _ => Med12
        };
    }

    public static double GetBucketProbability(int rows, int bucketIndex)
    {
        if (bucketIndex < 0 || bucketIndex > rows) return 0.0;
        double coeff = BinomialCoefficient(rows, bucketIndex);
        return coeff / Math.Pow(2.0, rows);
    }

    private static double BinomialCoefficient(int n, int k)
    {
        if (k < 0 || k > n) return 0.0;
        if (k > n - k) k = n - k;
        double res = 1.0;
        for (int i = 1; i <= k; i++)
        {
            res = res * (n - k + i) / i;
        }
        return res;
    }

    public static Color GetBucketColor(int index, int totalBuckets)
    {
        if (totalBuckets <= 1) return Color.Gold;
        float mid = (totalBuckets - 1) * 0.5f;
        float t = Math.Clamp(MathF.Abs(index - mid) / mid, 0f, 1f);

        // Smooth heat gradient: Center Gold -> Orange -> Hot Coral -> Deep Crimson
        if (t < 0.35f)
        {
            float u = t / 0.35f;
            return LerpColor(new Color(234, 179, 8, 255), new Color(245, 130, 18, 255), u);
        }
        if (t < 0.72f)
        {
            float u = (t - 0.35f) / 0.37f;
            return LerpColor(new Color(245, 130, 18, 255), new Color(239, 68, 68, 255), u);
        }
        else
        {
            float u = (t - 0.72f) / 0.28f;
            return LerpColor(new Color(239, 68, 68, 255), new Color(210, 20, 75, 255), u);
        }
    }

    public static string FormatMultiplier(decimal m)
    {
        if (m >= 100m) return $"{m:0}x";
        if (m >= 10m && m == Math.Truncate(m)) return $"{m:0}x";
        if (m == Math.Truncate(m) && m >= 2m) return $"{m:0}x";
        return $"{m:0.#}x";
    }

    public static Color GetRiskAccentColor(PlinkoRisk risk) => risk switch
    {
        PlinkoRisk.Low => new Color(34, 197, 94, 255),
        PlinkoRisk.Medium => new Color(245, 158, 11, 255),
        PlinkoRisk.High => new Color(239, 68, 68, 255),
        _ => Color.Gold
    };

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)255);
    }
}
