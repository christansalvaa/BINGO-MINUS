//to be edited, draft only

using Raylib_cs;

namespace BINGO_MINUS;

public sealed class CrashMinigame : IMinigame
{
    private const decimal MinimumBet = 1m;
    private const decimal StartingBet = 10m;
    private const decimal MultiplierGrowthPerSecond = 0.65m;

    private const int ChartLeft = 80;
    private const int ChartTop = 190;
    private const int ChartWidth = 800;
    private const int ChartHeight = 280;

    private readonly CurrencyManager _currency;
    private GameState _state;
    private decimal _bet;
    private decimal _multiplier = 1m;
    private decimal _crashPoint;
    private float _elapsedSeconds;
    private string _resultMessage = string.Empty;

    public string Name => "Crash";

    public bool IsFinished { get; private set; }

    public CrashMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public void Start()
    {
        IsFinished = false;
        _state = GameState.ChoosingBet;
        _bet = Math.Min(StartingBet, _currency.Balance);
        _multiplier = 1m;
        _elapsedSeconds = 0;
        _resultMessage = string.Empty;
    }

    public void Update(float deltaTime)
    {
        if (IsFinished)
        {
            return;
        }

        switch (_state)
        {
            case GameState.ChoosingBet:
                UpdateBetSelection();
                break;
            case GameState.Running:
                UpdateRound(deltaTime);
                break;
            case GameState.RoundOver:
                UpdateRoundResult();
                break;
        }
    }

    public void Draw()
    {
        DrawHeader();

        switch (_state)
        {
            case GameState.ChoosingBet:
                DrawBetSelection();
                break;
            case GameState.Running:
                DrawRunningRound();
                break;
            case GameState.RoundOver:
                DrawRoundResult();
                break;
        }
    }

    public void Stop()
    {
        IsFinished = true;
    }

    private void UpdateBetSelection()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            IsFinished = true;
            return;
        }

        if (_currency.Balance < MinimumBet)
        {
            _resultMessage = "You don't have enough currency to play.";
            _state = GameState.RoundOver;
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

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            StartRound();
        }
    }

    private void StartRound()
    {
        if (!_currency.TrySpend(_bet))
        {
            _resultMessage = "Not enough currency for that bet.";
            return;
        }

        _multiplier = 1m;
        _elapsedSeconds = 0;
        _crashPoint = Random.Shared.Next(125, 601) / 100m;
        _state = GameState.Running;
    }

    private void UpdateRound(float deltaTime)
    {
        _elapsedSeconds += Math.Max(0, deltaTime);
        _multiplier = 1m + (decimal)_elapsedSeconds * MultiplierGrowthPerSecond;

        if (_multiplier >= _crashPoint)
        {
            _multiplier = _crashPoint;
            _resultMessage = $"Crashed at {_crashPoint:0.00}x. You lost {_bet:0.00}.";
            _state = GameState.RoundOver;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            CashOut();
        }
    }

    private void CashOut()
    {
        decimal winnings = decimal.Round(_bet * _multiplier, 2);
        _currency.Add(winnings);
        _resultMessage = $"Cashed out at {_multiplier:0.00}x. Won {winnings:0.00}!";
        _state = GameState.RoundOver;
    }

    private void UpdateRoundResult()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            IsFinished = true;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            _bet = Math.Min(StartingBet, _currency.Balance);
            _state = GameState.ChoosingBet;
        }
    }

    private void DrawHeader()
    {
        DrawCenteredText("CRASH", 38, 40, Color.RayWhite);
        Raylib.DrawText($"Balance: {_currency.Balance:0.00}", 30, 28, 20, Color.LightGray);
    }

    private void DrawBetSelection()
    {
        DrawCenteredText("Choose your bet", 155, 24, Color.RayWhite);
        DrawCenteredText($"{_bet:0.00}", 225, 48, Color.Green);
        DrawCenteredText("LEFT / RIGHT: change bet", 325, 20, Color.LightGray);
        DrawCenteredText("ENTER: start round", 360, 20, Color.LightGray);
        DrawCenteredText("ESC: quit", 395, 20, Color.Gray);
    }

    private void DrawRunningRound()
    {
        DrawChart();
        DrawCenteredText($"{_multiplier:0.00}x", 70, 58, Color.Green);
        DrawCenteredText($"Bet: {_bet:0.00}", 505, 20, Color.LightGray);
        DrawCenteredText("SPACE: cash out", 545, 22, Color.RayWhite);
        DrawCenteredText($"Potential payout: {decimal.Round(_bet * _multiplier, 2):0.00}", 580, 18, Color.Gray);
    }

    private void DrawRoundResult()
    {
        Color resultColor = _resultMessage.StartsWith("Crashed", StringComparison.Ordinal)
            ? Color.Red
            : Color.Green;

        DrawCenteredText(_resultMessage, 245, 24, resultColor);
        DrawCenteredText($"Balance: {_currency.Balance:0.00}", 300, 22, Color.RayWhite);
        DrawCenteredText("ENTER: play again", 375, 20, Color.LightGray);
        DrawCenteredText("ESC: quit", 410, 20, Color.Gray);
    }

    private void DrawChart()
    {
        Raylib.DrawRectangleLines(ChartLeft, ChartTop, ChartWidth, ChartHeight, Color.DarkGray);
        Raylib.DrawLine(
            ChartLeft,
            ChartTop + ChartHeight,
            ChartLeft + ChartWidth,
            ChartTop + ChartHeight,
            Color.Gray);

        float crashDuration = (float)((_crashPoint - 1m) / MultiplierGrowthPerSecond);
        float progress = Math.Clamp(_elapsedSeconds / crashDuration, 0f, 1f);
        float multiplierRange = (float)(_crashPoint - 1m);
        float multiplierProgress = Math.Clamp((float)(_multiplier - 1m) / multiplierRange, 0f, 1f);

        int currentX = ChartLeft + (int)(ChartWidth * progress);
        int currentY = ChartTop + ChartHeight - (int)(ChartHeight * multiplierProgress);

        Raylib.DrawLine(
            ChartLeft,
            ChartTop + ChartHeight,
            currentX,
            currentY,
            Color.Green);
        Raylib.DrawCircle(currentX, currentY, 7, Color.Lime);
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color)
    {
        int x = (GameCanvas.Width - Raylib.MeasureText(text, fontSize)) / 2;
        Raylib.DrawText(text, x, y, fontSize, color);
    }

    private enum GameState
    {
        ChoosingBet,
        Running,
        RoundOver
    }
}
