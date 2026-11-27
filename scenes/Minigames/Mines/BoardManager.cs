using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>The 5x5 board (this was "TileManager" in the memory game).</summary>
    public static class BoardManager
    {
        public const int Columns = 5;
        public const int Rows = 5;
        public const int CardCount = Columns * Rows;

        private const float CardWidth = 117f;
        private const float CardHeight = 174f;
        private const float Gap = 13.5f;
        private const float FlipSeconds = 0.30f;

        public static readonly List<Card> Cards = new();
        public static int MineCount = 3;   // mines in the CURRENT round
        public static int GemsFound;       // safe cards picked this round

        private static readonly Random Rng = new();

        /// <summary>Builds the 25 face-down cards, centred inside the table panel.</summary>
        public static void Build()
        {
            Cards.Clear();
            float gridW = Columns * CardWidth + (Columns - 1) * Gap;
            float gridH = Rows * CardHeight + (Rows - 1) * Gap;
            float startX = Layout.Table.X + (Layout.Table.Width - gridW) / 2f;
            float startY = Layout.Table.Y + (Layout.Table.Height - gridH) / 2f;

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    float x = startX + col * (CardWidth + Gap);
                    float y = startY + row * (CardHeight + Gap);
                    Cards.Add(new Card(new Rectangle(x, y, CardWidth, CardHeight)));
                }
            }
        }

        /// <summary>Starts a round: Fisher-Yates shuffle (same idea as your memory game), first N are bombs.</summary>
        public static void NewRound(int mines)
        {
            MineCount = mines;
            GemsFound = 0;

            int[] order = new int[CardCount];
            for (int i = 0; i < CardCount; i++) order[i] = i;

            int n = CardCount;
            while (n > 1)
            {
                n--;
                int k = Rng.Next(n + 1);
                (order[k], order[n]) = (order[n], order[k]);
            }

            foreach (Card c in Cards)
            {
                c.IsBomb = false;
                c.IsRevealed = false;
                c.IsExposed = false;
                c.FlipDelay = 0f;
            }
            for (int i = 0; i < mines; i++) Cards[order[i]].IsBomb = true;
        }

        /// <summary>Player picks a card. Returns true if it was a bomb.</summary>
        public static bool Reveal(Card card)
        {
            card.IsRevealed = true;
            card.ShownBomb = card.IsBomb;
            card.FlipDelay = 0f;
            if (card.IsBomb) return true;

            GemsFound++;
            return false;
        }

        /// <summary>Flips every remaining card face up (dimmed), one after another.</summary>
        public static void ExposeRest(float firstDelay, float stagger)
        {
            int n = 0;
            foreach (Card c in Cards)
            {
                if (c.FaceUp) continue;
                c.IsExposed = true;
                c.ShownBomb = c.IsBomb;
                c.FlipDelay = firstDelay + n * stagger;
                n++;
            }
        }

        public static Card? CardAt(Vector2 point)
        {
            foreach (Card c in Cards)
                if (Raylib.CheckCollisionPointRec(point, c.Bounds)) return c;
            return null;
        }

        public static void Update(float dt, Vector2 mouse, bool interactive)
        {
            foreach (Card c in Cards)
            {
                bool hovered = interactive && !c.FaceUp && Raylib.CheckCollisionPointRec(mouse, c.Bounds);
                c.Hover = Approach(c.Hover, hovered ? 1f : 0f, dt * 10f);

                if (c.FaceUp)
                {
                    if (c.FlipDelay > 0f) c.FlipDelay -= dt;
                    else c.Flip = MathF.Min(1f, c.Flip + dt / FlipSeconds);
                }
                else
                {
                    c.Flip = MathF.Max(0f, c.Flip - dt / FlipSeconds);
                }
            }
        }

        /// <summary>
        /// Payout multiplier after N safe picks: 1 / (chance of surviving N picks),
        /// minus a 1% house edge. Same formula as the first Mines version.
        /// </summary>
        public static double Multiplier(int mines, int picks)
        {
            if (picks <= 0) return 1.00;

            int safe = CardCount - mines;
            double survive = 1.0;
            for (int i = 0; i < picks; i++)
                survive *= (double)(safe - i) / (CardCount - i);

            return Math.Floor(0.99 / survive * 100.0) / 100.0;
        }

        private static float Approach(float value, float target, float step)
        {
            if (value < target) return MathF.Min(value + step, target);
            return MathF.Max(value - step, target);
        }
    }
}
