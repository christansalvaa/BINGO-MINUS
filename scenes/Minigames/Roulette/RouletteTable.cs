// Betting-table geometry and mouse hit-testing for the roulette layout.
//
//        +----+---+---+---+-- ... --+---+---+------+
//        |    | 3 | 6 | 9 |         |36 |2:1| row 0 (top)
//        | 0  | 2 | 5 | 8 |         |35 |2:1| row 1
//        |    | 1 | 4 | 7 |         |34 |2:1| row 2 (bottom)
//        +----+---+---+---+-- ... --+---+---+
//             |  1st 12   | 2nd 12  | 3rd 12 |
//             | 1-18 | EVEN | RED | BLACK | ODD | 19-36 |
//
// Clicking the centre of a number = straight bet. Clicking near an edge/corner places the
// matching inside bet (split, street, corner, six line, or a trio with the zero).

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class RouletteTable
{
    public const float X0 = 593f;
    public const float Y0 = 162f;
    public const float ZeroW = 93f;
    public const float CellW = 90f;
    public const float CellH = 93f;
    public const float ColBetW = 93f;
    public const float OutsideH = 69f;
    public const float NumX0 = X0 + ZeroW;

    // How close to a cell border (in pixels) a click counts as an edge/corner bet.
    private const float Edge = 16.5f;

    public static Rectangle ZeroRect => new(X0, Y0, ZeroW, CellH * 3);

    public static Rectangle TableBounds => new(X0, Y0, ZeroW + CellW * 12 + ColBetW, CellH * 3 + OutsideH * 2);

    // col 0..11, row 0 (top) .. 2 (bottom)
    public static int NumberAt(int col, int row) => 3 * col + 3 - row;

    public static Rectangle CellRect(int number)
    {
        if (number == 0) return ZeroRect;

        int col = (number - 1) / 3;
        int row = 2 - (number - 1) % 3;
        return new Rectangle(NumX0 + col * CellW, Y0 + row * CellH, CellW, CellH);
    }

    public static Vector2 CellCenter(int number)
    {
        Rectangle r = CellRect(number);
        return new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
    }

    public static Rectangle ColumnBetRect(int row) => new(NumX0 + 12 * CellW, Y0 + row * CellH, ColBetW, CellH);

    public static Rectangle DozenRect(int index) => new(NumX0 + index * 4 * CellW, Y0 + 3 * CellH, 4 * CellW, OutsideH);

    public static Rectangle EvenMoneyRect(int index) => new(NumX0 + index * 2 * CellW, Y0 + 3 * CellH + OutsideH, 2 * CellW, OutsideH);

    // Outside bets are fixed, so build them once. Order matches the index used by the Rect helpers.
    public static readonly (Rectangle Rect, RouletteBetSpot Spot)[] OutsideSpots = BuildOutsideSpots();

    private static (Rectangle, RouletteBetSpot)[] BuildOutsideSpots()
    {
        var list = new List<(Rectangle, RouletteBetSpot)>();

        // Columns ("2 to 1"): top row = 3,6,9..., bottom row = 1,4,7...
        for (int row = 0; row < 3; row++)
        {
            int rowFromBottom = 2 - row;
            var numbers = Enumerable.Range(1, 36).Where(n => (n - 1) % 3 == rowFromBottom).ToArray();
            Rectangle r = ColumnBetRect(row);
            list.Add((r, new RouletteBetSpot($"COLUMN {rowFromBottom + 1}", numbers, Center(r))));
        }

        string[] dozenNames = ["1ST DOZEN", "2ND DOZEN", "3RD DOZEN"];
        for (int i = 0; i < 3; i++)
        {
            Rectangle r = DozenRect(i);
            list.Add((r, new RouletteBetSpot(dozenNames[i], RouletteRules.Range(i * 12 + 1, i * 12 + 12), Center(r))));
        }

        int[] all = RouletteRules.Range(1, 36);
        (string, int[])[] evenMoney =
        [
            ("1-18", RouletteRules.Range(1, 18)),
            ("EVEN", all.Where(n => n % 2 == 0).ToArray()),
            ("RED", all.Where(n => RouletteRules.ColorOf(n) == RouletteColor.Red).ToArray()),
            ("BLACK", all.Where(n => RouletteRules.ColorOf(n) == RouletteColor.Black).ToArray()),
            ("ODD", all.Where(n => n % 2 == 1).ToArray()),
            ("19-36", RouletteRules.Range(19, 36))
        ];
        for (int i = 0; i < 6; i++)
        {
            Rectangle r = EvenMoneyRect(i);
            list.Add((r, new RouletteBetSpot(evenMoney[i].Item1, evenMoney[i].Item2, Center(r))));
        }

        return list.ToArray();
    }

    public static RouletteBetSpot? HitTest(Vector2 p)
    {
        foreach (var (rect, spot) in OutsideSpots)
        {
            if (Contains(rect, p)) return spot;
        }

        Rectangle zero = ZeroRect;
        if (Contains(zero, p))
        {
            return HitZero(p);
        }

        Rectangle region = new(NumX0, Y0, 12 * CellW, 3 * CellH);
        return Contains(region, p) ? HitNumbers(p) : null;
    }

    private static RouletteBetSpot HitZero(Vector2 p)
    {
        float lx = p.X - X0;
        if (lx < ZeroW - Edge) return Straight(0);

        // Right edge of the zero cell: split with the neighbouring number, or a trio at a row border.
        float ly = p.Y - Y0;
        int row = Math.Clamp((int)(ly / CellH), 0, 2);
        float inRow = ly - row * CellH;

        if (inRow < Edge && row > 0) return ZeroTrio(row - 1, row);
        if (inRow > CellH - Edge && row < 2) return ZeroTrio(row, row + 1);
        return Split(0, NumberAt(0, row), new Vector2(NumX0, Y0 + row * CellH + CellH / 2f));
    }

    private static RouletteBetSpot HitNumbers(Vector2 p)
    {
        float lx = p.X - NumX0;
        float ly = p.Y - Y0;
        int col = Math.Clamp((int)(lx / CellW), 0, 11);
        int row = Math.Clamp((int)(ly / CellH), 0, 2);
        float cx = lx - col * CellW;
        float cy = ly - row * CellH;

        bool left = cx < Edge;
        bool right = cx > CellW - Edge;
        bool top = cy < Edge && row > 0;      // nothing above the top row
        bool bottom = cy > CellH - Edge;       // bottom row: street / six line
        if (col == 11) right = false;          // the 2:1 column bet is its own cell

        int number = NumberAt(col, row);
        float centerX = NumX0 + col * CellW + CellW / 2f;
        float centerY = Y0 + row * CellH + CellH / 2f;
        float tableBottom = Y0 + 3 * CellH;

        // Bottom edge of the bottom row: street, or six line when also near a vertical border.
        if (bottom && row == 2)
        {
            if (left && col > 0) return SixLine(col - 1, col, tableBottom);
            if (right) return SixLine(col, col + 1, tableBottom);
            return Street(col, centerX, tableBottom);
        }

        // Corners (inside the number grid, or with the zero when next to the zero cell).
        if ((left || right) && (top || bottom))
        {
            int col2 = left ? col - 1 : col + 1;
            int row2 = top ? row - 1 : row + 1;
            float borderY = Y0 + Math.Max(row, row2) * CellH;

            if (col2 < 0) return ZeroTrio(Math.Min(row, row2), Math.Max(row, row2));

            int[] corner = [number, NumberAt(col2, row), NumberAt(col, row2), NumberAt(col2, row2)];
            float borderX = NumX0 + Math.Max(col, col2) * CellW;
            return new RouletteBetSpot($"CORNER {string.Join("-", corner.OrderBy(n => n))}", corner, new Vector2(borderX, borderY));
        }

        // Single edges: splits.
        if (left)
        {
            return col == 0
                ? Split(0, number, new Vector2(NumX0, centerY))
                : Split(number, NumberAt(col - 1, row), new Vector2(NumX0 + col * CellW, centerY));
        }

        if (right) return Split(number, NumberAt(col + 1, row), new Vector2(NumX0 + (col + 1) * CellW, centerY));
        if (top) return Split(number, NumberAt(col, row - 1), new Vector2(centerX, Y0 + row * CellH));
        if (bottom) return Split(number, NumberAt(col, row + 1), new Vector2(centerX, Y0 + (row + 1) * CellH));

        return Straight(number);
    }

    private static RouletteBetSpot Straight(int number) => new($"{number}", [number], CellCenter(number));

    private static RouletteBetSpot Split(int a, int b, Vector2 anchor)
    {
        int lo = Math.Min(a, b);
        int hi = Math.Max(a, b);
        return new RouletteBetSpot($"SPLIT {lo}-{hi}", [lo, hi], anchor);
    }

    private static RouletteBetSpot Street(int col, float anchorX, float anchorY)
    {
        int[] numbers = [3 * col + 1, 3 * col + 2, 3 * col + 3];
        return new RouletteBetSpot($"STREET {numbers[0]}-{numbers[2]}", numbers, new Vector2(anchorX, anchorY));
    }

    private static RouletteBetSpot SixLine(int colA, int colB, float anchorY)
    {
        int[] numbers = [3 * colA + 1, 3 * colA + 2, 3 * colA + 3, 3 * colB + 1, 3 * colB + 2, 3 * colB + 3];
        return new RouletteBetSpot($"LINE {numbers[0]}-{numbers[5]}", numbers, new Vector2(NumX0 + colB * CellW, anchorY));
    }

    // Trio with zero: 0-1-2 (rows 1,2) or 0-2-3 (rows 0,1).
    private static RouletteBetSpot ZeroTrio(int rowA, int rowB)
    {
        int[] numbers = [0, NumberAt(0, rowA), NumberAt(0, rowB)];
        string label = $"TRIO {string.Join("-", numbers.OrderBy(n => n))}";
        return new RouletteBetSpot(label, numbers, new Vector2(NumX0, Y0 + Math.Max(rowA, rowB) * CellH));
    }

    private static Vector2 Center(Rectangle r) => new(r.X + r.Width / 2f, r.Y + r.Height / 2f);

    private static bool Contains(Rectangle r, Vector2 p) =>
        p.X >= r.X && p.X < r.X + r.Width && p.Y >= r.Y && p.Y < r.Y + r.Height;
}
