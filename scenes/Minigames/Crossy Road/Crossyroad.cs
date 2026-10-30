using System;
using Raylib_cs;

namespace BINGO_MINUS;

public enum CrossyDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum CrossyState
{
    Betting,
    Playing,
    GameOver
}

public class Crossyroad : IMinigame
{
    public const int TotalLanes = 8;

    private readonly CurrencyManager _currency;
    private readonly CrossyroadUI _ui = new();
    private readonly Random _rng = new();

    public string Name => "Crossy Road";
    public bool IsFinished { get; private set; }

    private CrossyState _currentState = CrossyState.Betting;
    private CrossyDifficulty _difficulty = CrossyDifficulty.Easy;

    private decimal _currentBet;
    private int _currentLane;
    private int _crashedLane = -1;
    private decimal _currentMultiplier = 1.00m;
    private decimal _nextMultiplier = 1.02m;
    private string _resultMessage = "";
    private bool _isWin;

    public Crossyroad(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        IsFinished = false;
        _ui.LoadAssets();
        ResetRound();
    }

    private void ResetRound()
    {
        _currentState = CrossyState.Betting;
        _currentBet = 0m;
        _currentLane = 0;
        _crashedLane = -1;
        _currentMultiplier = 1.00m;
        _nextMultiplier = GetMultiplier(_difficulty, 1);
        _resultMessage = "";
        _isWin = false;
    }

    public void Update(float deltaTime)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || UiButton.WasClicked(CrossyroadUI.BtnBack))
        {
            IsFinished = true;
            return;
        }

        switch (_currentState)
        {
            case CrossyState.Betting:
                UpdateBettingPhase();
                break;
            case CrossyState.Playing:
                UpdatePlayingPhase();
                break;
            case CrossyState.GameOver:
                UpdateGameOverPhase();
                break;
        }
    }

    private void UpdateBettingPhase()
    {
        // Difficulty selection
        if (UiButton.WasClicked(CrossyroadUI.BtnEasy))
        {
            _difficulty = CrossyDifficulty.Easy;
            _nextMultiplier = GetMultiplier(_difficulty, 1);
        }
        else if (UiButton.WasClicked(CrossyroadUI.BtnMedium))
        {
            _difficulty = CrossyDifficulty.Medium;
            _nextMultiplier = GetMultiplier(_difficulty, 1);
        }
        else if (UiButton.WasClicked(CrossyroadUI.BtnHard))
        {
            _difficulty = CrossyDifficulty.Hard;
            _nextMultiplier = GetMultiplier(_difficulty, 1);
        }

        // Chip betting
        for (int i = 0; i < _ui.ChipValues.Length; i++)
        {
            int chipValue = _ui.ChipValues[i];
            if (UiButton.WasClicked(CrossyroadUI.GetChipBounds(i, _ui.ChipValues.Length)) &&
                _currentBet + chipValue <= _currency.Balance)
            {
                _currentBet += chipValue;
            }
        }

        if (_currentBet > 0)
        {
            if (UiButton.WasClicked(CrossyroadUI.BtnClear))
            {
                _currentBet = 0m;
            }

            if (UiButton.WasClicked(CrossyroadUI.BtnStart) && _currency.TrySpend(_currentBet))
            {
                _currentLane = 0;
                _crashedLane = -1;
                _currentMultiplier = 1.00m;
                _nextMultiplier = GetMultiplier(_difficulty, 1);
                _currentState = CrossyState.Playing;
            }
        }
    }

    private void UpdatePlayingPhase()
    {
        // Advance lane / Cross
        if (UiButton.WasClicked(CrossyroadUI.BtnStep))
        {
            StepForward();
        }
        // Cash out early
        else if (_currentLane > 0 && UiButton.WasClicked(CrossyroadUI.BtnCashOut))
        {
            CashOut();
        }
    }

    private void StepForward()
    {
        float roll = (float)_rng.NextDouble();
        float crashChance = GetCrashChance(_difficulty);

        if (roll < crashChance)
        {
            // Crashed by vehicle!
            _crashedLane = _currentLane + 1;
            _currentLane = _crashedLane;
            _currentState = CrossyState.GameOver;
            _isWin = false;
            _resultMessage = $"CRASH! You got hit on Lane {_crashedLane}!";
        }
        else
        {
            // Survived step
            _currentLane++;
            _currentMultiplier = GetMultiplier(_difficulty, _currentLane);

            if (_currentLane >= TotalLanes)
            {
                // Reached the opposite side safely!
                decimal payout = Math.Round(_currentBet * _currentMultiplier, 2);
                _currency.Add(payout);
                _currentState = CrossyState.GameOver;
                _isWin = true;
                _resultMessage = $"ROAD CLEARED! You reached safety and won {payout:0.00} coins ({_currentMultiplier:0.00}x)!";
            }
            else
            {
                _nextMultiplier = GetMultiplier(_difficulty, _currentLane + 1);
            }
        }
    }

    private void CashOut()
    {
        decimal payout = Math.Round(_currentBet * _currentMultiplier, 2);
        _currency.Add(payout);
        _currentState = CrossyState.GameOver;
        _isWin = true;
        _resultMessage = $"CASHED OUT! Won {payout:0.00} coins at {_currentMultiplier:0.00}x multiplier!";
    }

    private void UpdateGameOverPhase()
    {
        if (UiButton.WasClicked(CrossyroadUI.BtnPlayAgain))
        {
            ResetRound();
        }
    }

    public void Draw()
    {
        _ui.DrawBackground();
        CrossyroadUI.DrawTopBar(_currency.Balance);
        _ui.DrawRoadGrid(TotalLanes, _currentLane, _crashedLane, _difficulty);

        switch (_currentState)
        {
            case CrossyState.Betting:
                _ui.DrawBettingPhase(_currentBet, _currency.Balance, _difficulty);
                break;

            case CrossyState.Playing:
                CrossyroadUI.DrawPlayingPhase(_currentBet, _currentMultiplier, _nextMultiplier, _currentLane, TotalLanes);
                break;

            case CrossyState.GameOver:
                CrossyroadUI.DrawGameOverPhase(_resultMessage, _isWin, _currentBet, _currentMultiplier);
                break;
        }
    }

    public void Stop()
    {
        _ui.UnloadAssets();
    }

    public static float GetCrashChance(CrossyDifficulty difficulty) => difficulty switch
    {
        CrossyDifficulty.Easy => 0.04f,
        CrossyDifficulty.Medium => 0.12f,
        CrossyDifficulty.Hard => 0.20f,
        _ => 0.04f
    };

    public static decimal GetMultiplier(CrossyDifficulty difficulty, int lane)
    {
        if (lane <= 0) return 1.00m;
        float survival = 1.0f - GetCrashChance(difficulty);
        // Fair multiplier calculation with ~2% casino house edge (0.98 RTP)
        double mult = 0.98 / Math.Pow(survival, lane);
        return Math.Max(1.01m, (decimal)Math.Round(mult, 2));
    }
}
