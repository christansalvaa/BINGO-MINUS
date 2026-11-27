using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>
    /// Every clickable rectangle. GameManager uses them for clicks and Renderer uses
    /// the very same ones for drawing, so the two can never disagree.
    /// </summary>
    public static class Layout
    {
        private const float OffsetX = 40f;

        public static readonly Rectangle LeftPanel  = new(24 + OffsetX, 76, 300, 620);
        public static readonly Rectangle Table      = new(336 + OffsetX, 76, 528, 620);
        public static readonly Rectangle RightPanel = new(876 + OffsetX, 76, 300, 620);

        // Bet amount
        public static readonly Rectangle BetField  = new(40 + OffsetX, 118, 148, 44);
        public static readonly Rectangle HalfBtn   = new(192 + OffsetX, 118, 56, 44);
        public static readonly Rectangle DoubleBtn = new(252 + OffsetX, 118, 56, 44);
        public static readonly Rectangle MinBtn    = new(40 + OffsetX, 168, 130, 30);
        public static readonly Rectangle MaxBtn    = new(178 + OffsetX, 168, 130, 30);

        // Mines / gems
        public static readonly Rectangle MinesBox   = new(40 + OffsetX, 252, 164, 44);
        public static readonly Rectangle MinesMinus = new(40 + OffsetX, 252, 40, 44);
        public static readonly Rectangle MinesPlus  = new(164 + OffsetX, 252, 40, 44);
        public static readonly Rectangle GemsBox    = new(212 + OffsetX, 252, 96, 44);

        public static readonly int[] PresetValues = { 1, 3, 5, 10, 24 };
        public static readonly Rectangle[] Presets = BuildPresets();

        // Profit + actions
        public static readonly Rectangle ProfitBox = new(40 + OffsetX, 378, 268, 44);
        public static readonly Rectangle RandomBtn = new(40 + OffsetX, 556, 268, 46);
        public static readonly Rectangle MainBtn   = new(40 + OffsetX, 612, 268, 62);

        private static Rectangle[] BuildPresets()
        {
            var list = new Rectangle[PresetValues.Length];
            for (int i = 0; i < list.Length; i++)
                list[i] = new Rectangle(40 + OffsetX + i * 55f, 304f, 48f, 30f);
            return list;
        }
    }
}
