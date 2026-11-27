using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>One card on the 5x5 board (this was "Tile" in the memory game).</summary>
    public class Card
    {
        public Rectangle Bounds;

        public bool IsBomb;       // what is really under the card
        public bool IsRevealed;   // the player picked it
        public bool IsExposed;    // shown automatically when the round ends (dimmed)
        public bool ShownBomb;    // snapshot of IsBomb taken when the face turned up,
                                  // so a card flipping back never leaks the next round
        public float Flip;        // 0 = face down, 1 = face up
        public float FlipDelay;   // seconds to wait before flipping (staggered reveal)
        public float Hover;       // 0..1 hover glow

        public bool FaceUp => IsRevealed || IsExposed;

        public Card(Rectangle bounds)
        {
            Bounds = bounds;
        }
    }
}
