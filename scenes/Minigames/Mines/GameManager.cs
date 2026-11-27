using System.Globalization;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    public enum GameState
    {
        Idle,      // choose bet / mines, press BET
        Playing,   // round in progress: pick cards or cash out
        BombHit    // 2 second explosion: smoke, shake, red fog (input locked)
    }

    /// <summary>Rules, input and the state machine (same job as your old GameManager).</summary>
    public static class GameManager
    {
        public const double MinBet = 1.00;

        private static BINGO_MINUS.CurrencyManager? _currency;

        public static GameState State = GameState.Idle;

        public static double Bet = 10.00;
        public static string BetText = "10.00";
        public static bool EditingBet;
        public static bool BetSelected;        // text is "selected": first key replaces it

        public static int Mines = 3;           // what the player picked in the panel
        public static double RoundBet;         // bet locked in for the current round
        public static double CurrentMultiplier = 1.00;

        public static string Banner = "";
        public static Color BannerColor = Theme.Text;
        public static float BannerTimer;

        private static double CurrentBalance => _currency != null ? (double)_currency.Balance : 0.0;

        public static void Start(BINGO_MINUS.CurrencyManager currency)
        {
            _currency = currency;
            State = GameState.Idle;
            EditingBet = false;
            BetSelected = false;
            Banner = "";
            BannerTimer = 0f;
            BoardManager.Build();
            SetBet(Bet);
        }

        public static void Update(float dt)
        {
            AudioManager.Update();

            Vector2 mouse = BINGO_MINUS.GameCanvas.GetMousePosition();

            BoardManager.Update(dt, mouse, State == GameState.Playing);
            Effects.Update(dt);
            if (BannerTimer > 0f) BannerTimer -= dt;

            if (State == GameState.BombHit)
            {
                // input is locked until the 2 second effect is over
                if (!Effects.BombActive) State = GameState.Idle;
                return;
            }

            bool click = Raylib.IsMouseButtonPressed(MouseButton.Left);

            if (State == GameState.Idle) UpdateIdle(mouse, click);
            else if (State == GameState.Playing) UpdatePlaying(mouse, click);
        }

        // ------------------------------------------------------------------ Idle
        private static void UpdateIdle(Vector2 mouse, bool click)
        {
            UpdateBetTyping();

            if (click)
            {
                bool inField = Hit(Layout.BetField, mouse);
                if (EditingBet && !inField) CommitBetText();
                if (inField && !EditingBet) { EditingBet = true; BetSelected = true; }
                else if (!inField) { EditingBet = false; BetSelected = false; }

                if (Hit(Layout.HalfBtn, mouse))   SetBet(Bet / 2.0);
                if (Hit(Layout.DoubleBtn, mouse)) SetBet(Bet * 2.0);
                if (Hit(Layout.MinBtn, mouse))    SetBet(MinBet);
                if (Hit(Layout.MaxBtn, mouse))    SetBet(CurrentBalance);

                if (Hit(Layout.MinesMinus, mouse)) Mines = Math.Max(1, Mines - 1);
                if (Hit(Layout.MinesPlus, mouse))  Mines = Math.Min(BoardManager.CardCount - 1, Mines + 1);
                for (int i = 0; i < Layout.Presets.Length; i++)
                    if (Hit(Layout.Presets[i], mouse)) Mines = Layout.PresetValues[i];

                if (Hit(Layout.MainBtn, mouse)) StartRound();
            }

            if (!EditingBet && Raylib.IsKeyPressed(KeyboardKey.Space)) StartRound();
        }

        private static void UpdateBetTyping()
        {
            if (!EditingBet) return;

            int ch;
            while ((ch = Raylib.GetCharPressed()) > 0)
            {
                bool digit = ch >= '0' && ch <= '9';
                if (!digit && ch != '.') continue;

                if (BetSelected) { BetText = ""; BetSelected = false; }

                int dot = BetText.IndexOf('.');
                if (ch == '.' && dot >= 0) continue;                 // only one dot
                if (dot >= 0 && BetText.Length - dot > 2) continue;  // max 2 decimals
                if (BetText.Length >= 9) continue;

                BetText += (char)ch;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace))
            {
                if (BetSelected) { BetText = ""; BetSelected = false; }
                else if (BetText.Length > 0) BetText = BetText.Substring(0, BetText.Length - 1);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
            {
                CommitBetText();
            }
        }

        private static void CommitBetText()
        {
            if (double.TryParse(BetText, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                SetBet(v);
            else
                SetBet(Bet);   // empty / invalid: put the old value back

            EditingBet = false;
            BetSelected = false;
        }

        private static void SetBet(double value)
        {
            double max = Math.Max(MinBet, CurrentBalance);
            Bet = Math.Round(Math.Clamp(value, MinBet, max), 2);
            BetText = Bet.ToString("F2", CultureInfo.InvariantCulture);
        }

        // --------------------------------------------------------------- Playing
        private static void UpdatePlaying(Vector2 mouse, bool click)
        {
            if (click)
            {
                if (Hit(Layout.MainBtn, mouse))
                {
                    if (BoardManager.GemsFound > 0) CashOut();
                    return;
                }
                if (Hit(Layout.RandomBtn, mouse))
                {
                    RandomPick();
                    return;
                }

                Card? card = BoardManager.CardAt(mouse);
                if (card != null && !card.FaceUp) PickCard(card);
            }

            if (State != GameState.Playing) return;

            if (Raylib.IsKeyPressed(KeyboardKey.Space) && BoardManager.GemsFound > 0) CashOut();
            else if (Raylib.IsKeyPressed(KeyboardKey.R)) RandomPick();
        }

        private static void StartRound()
        {
            if (EditingBet) CommitBetText();

            decimal betAmount = (decimal)Math.Round(Bet, 2);
            if (_currency == null || betAmount <= 0 || !_currency.TrySpend(betAmount))
            {
                ShowBanner("Not enough balance", Theme.Red);
                return;
            }

            AudioManager.StopBomb();
            RoundBet = Bet;
            CurrentMultiplier = 1.00;
            BoardManager.NewRound(Mines);
            BannerTimer = 0f;
            State = GameState.Playing;
        }

        private static void RandomPick()
        {
            var hidden = new List<Card>();
            foreach (Card c in BoardManager.Cards)
                if (!c.FaceUp) hidden.Add(c);

            if (hidden.Count == 0) return;
            PickCard(hidden[Random.Shared.Next(hidden.Count)]);
        }

        private static void PickCard(Card card)
        {
            bool hitBomb = BoardManager.Reveal(card);
            AudioManager.PlayFlip();

            if (hitBomb)
            {
                AudioManager.PlayBomb();
                State = GameState.BombHit;
                Stats.Record(RoundBet, 0.0, 0.0);

                Vector2 center = new Vector2(card.Bounds.X + card.Bounds.Width / 2f, card.Bounds.Y + card.Bounds.Height / 2f);
                Effects.TriggerBomb(center);
                BoardManager.ExposeRest(0.55f, 0.03f);

                bool refilled = AfterRound();
                string msg = $"BOOM!  -${RoundBet:F2}";
                if (refilled) msg += "  (balance reset)";
                ShowBanner(msg, Theme.Red);
            }
            else
            {
                AudioManager.PlayDiamond();
                CurrentMultiplier = BoardManager.Multiplier(BoardManager.MineCount, BoardManager.GemsFound);

                // found every gem -> automatic cash out
                if (BoardManager.GemsFound >= BoardManager.CardCount - BoardManager.MineCount) CashOut();
            }
        }

        private static void CashOut()
        {
            double payout = Math.Round(RoundBet * CurrentMultiplier, 2);
            if (payout > 0) _currency?.Add((decimal)payout);
            Stats.Record(RoundBet, payout, CurrentMultiplier);
            BoardManager.ExposeRest(0.25f, 0.025f);

            State = GameState.Idle;
            AfterRound();
            ShowBanner($"CASHED OUT  {CurrentMultiplier:F2}x   +${payout - RoundBet:F2}", Theme.Green);
        }

        /// <summary>Keeps the bet valid after a round finishes.</summary>
        private static bool AfterRound()
        {
            SetBet(Bet);
            return false;
        }

        private static void ShowBanner(string text, Color color)
        {
            Banner = text;
            BannerColor = color;
            BannerTimer = 4f;
        }

        private static bool Hit(Rectangle r, Vector2 p) => Raylib.CheckCollisionPointRec(p, r);
    }
}