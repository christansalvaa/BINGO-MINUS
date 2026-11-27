using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>
    /// Every clickable rectangle. GameManager uses them for clicks and Renderer uses
    /// the very same ones for drawing, so the two can never disagree.
    /// </summary>
    public static class Layout
    {
        private const float OffsetX = 60f;

        public static readonly Rectangle LeftPanel  = new(36 + OffsetX, 114, 450, 930);
        public static readonly Rectangle Table      = new(504 + OffsetX, 114, 792, 930);
        public static readonly Rectangle RightPanel = new(1314 + OffsetX, 114, 450, 930);

        // Bet amount
        public static readonly Rectangle BetField  = new(60 + OffsetX, 177, 222, 66);
        public static readonly Rectangle HalfBtn   = new(288 + OffsetX, 177, 84, 66);
        public static readonly Rectangle DoubleBtn = new(378 + OffsetX, 177, 84, 66);
        public static readonly Rectangle MinBtn    = new(60 + OffsetX, 252, 195, 45);
        public static readonly Rectangle MaxBtn    = new(267 + OffsetX, 252, 195, 45);

        // Mines / gems
        public static readonly Rectangle MinesBox   = new(60 + OffsetX, 378, 246, 66);
        public static readonly Rectangle MinesMinus = new(60 + OffsetX, 378, 60, 66);
        public static readonly Rectangle MinesPlus  = new(246 + OffsetX, 378, 60, 66);
        public static readonly Rectangle GemsBox    = new(318 + OffsetX, 378, 144, 66);

        public static readonly int[] PresetValues = { 1, 3, 5, 10, 24 };
        public static readonly Rectangle[] Presets = BuildPresets();

        // Profit + actions
        public static readonly Rectangle ProfitBox = new(60 + OffsetX, 567, 402, 66);
        public static readonly Rectangle RandomBtn = new(60 + OffsetX, 834, 402, 69);
        public static readonly Rectangle MainBtn   = new(60 + OffsetX, 918, 402, 93);

        private static Rectangle[] BuildPresets()
        {
            var list = new Rectangle[PresetValues.Length];
            for (int i = 0; i < list.Length; i++)
                list[i] = new Rectangle(60 + OffsetX + i * 82.5f, 456f, 72f, 45f);
            return list;
        }
    }
}
