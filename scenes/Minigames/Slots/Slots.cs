//to be edited, draft only

using Raylib_cs;

namespace BINGO_MINUS;

public sealed class SlotsMinigame : IMinigame
{
    private const decimal MinimumBet = 1m;
    private const decimal StartingBet = 5m;
    private const float SpinDuration = 1.4f;
    private const float ReelStepDuration = 0.12f;

    private static readonly string[] Symbols = ["CHERRY", "LEMON", "BELL", "STAR", "SEVEN"];

    private readonly CurrencyManager _currency;
    private readonly string[] _reels = ["CHERRY", "LEMON", "BELL"];
    private decimal _bet;
    private float _spinTimer;
    private float _reelTimer;
    private bool _isSpinning;
    private string _resultMessage = "Choose a bet, then spin.";
    private Color _resultColor = Color.LightGray;

    public string Name => "Slots";

    public bool IsFinished { get; private set; }

    public SlotsMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public void Start()
    {
        IsFinished = false;
        _bet = Math.Min(StartingBet, _currency.Balance);
        _isSpinning = false;
        _spinTimer = 0;
        _reelTimer = 0;
        _resultMessage = "Choose a bet, then spin.";
        _resultColor = Color.LightGray;
    }

    public void Update(float deltaTime)
    {
        if (IsFinished)
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            IsFinished = true;
            return;
        }

        if (_isSpinning)
        {
            UpdateSpin(deltaTime);
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            _bet = Math.Max(MinimumBet, _bet - 1m);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            _bet = Math.Min(_currency.Balance, _bet + 1m);
        }

        if (UiButton.WasClicked(new Rectangle(275, 405, 54, 42)))
        {
            _bet = Math.Max(MinimumBet, _bet - 1m);
        }
        else if (UiButton.WasClicked(new Rectangle(631, 405, 54, 42)))
        {
            _bet = Math.Min(_currency.Balance, _bet + 1m);
        }

        if (UiButton.WasClicked(new Rectangle(330, 465, 300, 64)) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            Spin();
        }
    }

    public void Draw()
    {
        DrawCenteredText("SLOTS", 38, 40, Color.RayWhite);
        Raylib.DrawText($"COINS  {_currency.Balance:0.00}", 30, 28, 20, Color.Gold);
        DrawCenteredText("Match symbols to win demo coins", 112, 18, Color.LightGray);

        DrawReels();

        DrawCenteredText($"BET: {_bet:0.00}", 410, 23, Color.RayWhite);
        UiButton.Draw(
            new Rectangle(275, 405, 54, 42),
            "-",
            new Color(72, 80, 99, 255));
        UiButton.Draw(
            new Rectangle(631, 405, 54, 42),
            "+",
            new Color(72, 80, 99, 255));

        if (!_isSpinning)
        {
            UiButton.Draw(new Rectangle(330, 465, 300, 64), "SPIN", new Color(145, 49, 69, 255));
        }
        else
        {
            DrawCenteredText("SPINNING...", 485, 24, Color.Gold);
        }

        DrawCenteredText(_resultMessage, 555, 18, _resultColor);
        DrawCenteredText("LEFT / RIGHT: bet     ENTER: spin     ESC: lobby", 595, 16, Color.Gray);
    }

    public void Stop()
    {
        IsFinished = true;
    }

    private void UpdateSpin(float deltaTime)
    {
        _spinTimer += Math.Max(deltaTime, 0);
        _reelTimer += Math.Max(deltaTime, 0);

        if (_reelTimer >= ReelStepDuration)
        {
            _reelTimer = 0;
            for (int i = 0; i < _reels.Length; i++)
            {
                _reels[i] = Symbols[Random.Shared.Next(Symbols.Length)];
            }
        }

        if (_spinTimer >= SpinDuration)
        {
            FinishSpin();
        }
    }

    private void Spin()
    {
        if (_isSpinning)
        {
            return;
        }

        if (_currency.Balance < MinimumBet)
        {
            _resultMessage = "Not enough coins. Visit the coin shop from the lobby.";
            _resultColor = Color.Gold;
            return;
        }

        if (!_currency.TrySpend(_bet))
        {
            _resultMessage = "Your bet is higher than your balance.";
            _resultColor = Color.Gold;
            return;
        }

        _spinTimer = 0;
        _reelTimer = 0;
        _isSpinning = true;
        _resultMessage = "Good luck!";
        _resultColor = Color.LightGray;
    }

    private void FinishSpin()
    {
        _isSpinning = false;
        for (int i = 0; i < _reels.Length; i++)
        {
            _reels[i] = Symbols[Random.Shared.Next(Symbols.Length)];
        }

        int matchCount = CountMatchingSymbols();
        decimal payoutMultiplier = matchCount switch
        {
            3 => _reels[0] == "SEVEN" ? 10m : 5m,
            2 => 1.5m,
            _ => 0m
        };

        if (payoutMultiplier == 0)
        {
            _resultMessage = "No match this time. Try another spin!";
            _resultColor = Color.LightGray;
            return;
        }

        decimal payout = decimal.Round(_bet * payoutMultiplier, 2);
        _currency.Add(payout);
        _resultMessage = $"You matched {matchCount}! Won {payout:0.00} coins.";
        _resultColor = Color.Green;
    }

    private int CountMatchingSymbols()
    {
        if (_reels[0] == _reels[1] && _reels[1] == _reels[2])
        {
            return 3;
        }

        if (_reels[0] == _reels[1] || _reels[0] == _reels[2] || _reels[1] == _reels[2])
        {
            return 2;
        }

        return 0;
    }

    private void DrawReels()
    {
        const int reelWidth = 190;
        const int reelHeight = 150;
        const int reelGap = 25;
        const int firstX = 200;
        const int reelY = 220;

        for (int i = 0; i < _reels.Length; i++)
        {
            int x = firstX + i * (reelWidth + reelGap);
            Raylib.DrawRectangle(x, reelY, reelWidth, reelHeight, new Color(37, 43, 57, 255));
            Raylib.DrawRectangleLines(x, reelY, reelWidth, reelHeight, new Color(180, 149, 81, 255));

            int textSize = 25;
            int textX = x + (reelWidth - Raylib.MeasureText(_reels[i], textSize)) / 2;
            Raylib.DrawText(_reels[i], textX, reelY + 62, textSize, Color.Gold);
        }
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color)
    {
        int x = (GameCanvas.Width - Raylib.MeasureText(text, fontSize)) / 2;
        Raylib.DrawText(text, x, y, fontSize, color);
    }
}
