// Color Game ("Perya") minigame: 3 dice, 6 colours, 1:1 per matching die.
// Flow: Betting -> Rolling -> Result -> (NEW ROUND) -> Betting ...
//  * Chips are taken from the wallet as soon as they are placed; UNDO/CLEAR/leaving refunds them.
//  * The final dice are picked when the roll starts; the animation just reveals them one die at a time.
//  * Winnings are paid when the last die stops (or immediately if the player leaves mid-roll).

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public sealed class ColorGameMinigame : IMinigame, IDepositRequester
{
    private enum ColorGameState { Betting, Rolling, Result }

    // Each die stops tumbling at its own time, then the result is held briefly.
    private static readonly float[] DieStopTimes = [1.1f, 1.7f, 2.3f];
    private const float RollDuration = 2.9f;
    private const float FaceChangeInterval = 0.07f;
    private const int MaxHistory = 5;

    private readonly CurrencyManager _currency;
    private readonly ColorGameBetBook _book = new();
    private readonly CasinoSfx _sfx = new();
    private readonly List<DiceColor[]> _history = [];       // newest first
    private decimal[] _lastBets = new decimal[ColorGameRules.All.Length];

    private ColorGameState _state;
    private int _selectedChip = 1;
    private bool _showOutOfCoins;

    // Round data
    private DiceColor[] _result = new DiceColor[ColorGameRules.DiceCount];
    private readonly DiceColor[] _shown = new DiceColor[ColorGameRules.DiceCount];
    private readonly bool[] _dieStopped = new bool[ColorGameRules.DiceCount];
    private int[]? _matches;
    private decimal _lastWin;
    private string _message = "PLACE YOUR BETS";
    private Color _messageColor = Color.LightGray;
    private float _flashTimer;
    private string _flashText = "";

    // Animation
    private float _rollTime;
    private float _faceTimer;

    public string Name => "Color Game";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }

    public ColorGameMinigame(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _book.Clear();
        _history.Clear();
        _lastBets = new decimal[ColorGameRules.All.Length];
        _matches = null;
        _lastWin = 0m;
        _state = ColorGameState.Betting;
        SetMessage("PLACE YOUR BETS", Color.LightGray);

        // Dice rest on a nice spread of colours before the first roll.
        for (int i = 0; i < _shown.Length; i++)
        {
            _shown[i] = ColorGameRules.All[(i * 2) % ColorGameRules.All.Length];
            _dieStopped[i] = true;
        }

        ColorGameUI.LoadArt();
        _sfx.Load();
        _showOutOfCoins = _currency.Balance < CasinoChips.MinChip;
    }

    public void Stop()
    {
        // Leaving mid-roll must not dodge a loss (or a win): settle the round first.
        if (_state == ColorGameState.Rolling)
        {
            SettleRound();
        }
        else if (_state == ColorGameState.Betting && _book.HasBets)
        {
            // Chips that were never rolled go back to the wallet.
            decimal refund = _book.Clear();
            if (refund > 0m) _currency.Add(refund);
        }

        _sfx.Unload();
    }

    // ---------------------------------------------------------------------------------------
    public void Update(float deltaTime)
    {
        if (IsFinished) return;

        if (_flashTimer > 0f) _flashTimer -= deltaTime;

        if (_showOutOfCoins)
        {
            UpdateOutOfCoins();
            return;
        }

        switch (_state)
        {
            case ColorGameState.Betting:
                UpdateBetting();
                break;
            case ColorGameState.Rolling:
                UpdateRoll(deltaTime);
                break;
            case ColorGameState.Result:
                UpdateResult();
                break;
        }
    }

    private void UpdateOutOfCoins()
    {
        switch (OutOfCoinsModal.Update())
        {
            case OutOfCoinsResult.Deposit:
                WantsDeposit = true;
                IsFinished = true;
                break;
            case OutOfCoinsResult.Lobby:
                IsFinished = true;
                break;
            case OutOfCoinsResult.Dismissed:
                _showOutOfCoins = false;
                break;
        }
    }

    // --- Betting ---------------------------------------------------------------------------
    private void UpdateBetting()
    {
        for (int i = 0; i < CasinoChips.Values.Length; i++)
        {
            if (UiImageButton.WasClicked(ColorGameUI.ChipBounds(i)))
            {
                _selectedChip = i;
                _sfx.Play(Sfx.Click, 0.6f);
            }
        }

        for (int i = 0; i < ColorGameRules.All.Length; i++)
        {
            Rectangle tile = ColorGameUI.TileBounds(i);
            if (!UiImageButton.IsHovered(tile)) continue;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) PlaceChip(ColorGameRules.All[i]);
            else if (Raylib.IsMouseButtonPressed(MouseButton.Right)) RemoveBet(ColorGameRules.All[i]);
        }

        if (UiButton.WasClicked(ColorGameUI.BtnUndo))
        {
            if (_book.TryUndo(out decimal refund))
            {
                _currency.Add(refund);
                _sfx.Play(Sfx.Click, 0.5f, 0.8f);
            }
        }
        else if (UiButton.WasClicked(ColorGameUI.BtnClear))
        {
            decimal refund = _book.Clear();
            if (refund > 0m)
            {
                _currency.Add(refund);
                _sfx.Play(Sfx.Click, 0.5f, 0.8f);
            }
        }
        else if (UiButton.WasClicked(ColorGameUI.BtnRebet))
        {
            Rebet();
        }
        else if (UiButton.WasClicked(ColorGameUI.BtnRoll))
        {
            if (!_book.HasBets)
            {
                Flash("PLACE A BET FIRST");
                _sfx.Play(Sfx.Error, 0.6f);
            }
            else
            {
                StartRoll();
            }
        }
    }

    private void PlaceChip(DiceColor color)
    {
        decimal chip = CasinoChips.Values[_selectedChip];
        if (!_currency.TrySpend(chip))
        {
            Flash("NOT ENOUGH COINS");
            _sfx.Play(Sfx.Error, 0.6f);

            // Nothing left to bet with at all -> offer a deposit.
            if (_currency.Balance < CasinoChips.MinChip && !_book.HasBets)
            {
                _showOutOfCoins = true;
            }

            return;
        }

        _book.Place(color, chip);
        _sfx.Play(Sfx.Click, 0.6f);
    }

    private void RemoveBet(DiceColor color)
    {
        decimal refund = _book.RemoveColor(color);
        if (refund > 0m)
        {
            _currency.Add(refund);
            _sfx.Play(Sfx.Click, 0.5f, 0.8f);
        }
    }

    private void Rebet()
    {
        decimal total = _lastBets.Sum();
        if (total <= 0m)
        {
            Flash("NO PREVIOUS BET");
            _sfx.Play(Sfx.Error, 0.6f);
            return;
        }

        if (total > _currency.Balance + _book.TotalStake)
        {
            Flash("NOT ENOUGH COINS FOR REBET");
            _sfx.Play(Sfx.Error, 0.6f);
            return;
        }

        decimal refund = _book.Clear();
        if (refund > 0m) _currency.Add(refund);

        _currency.TrySpend(total);
        for (int i = 0; i < _lastBets.Length; i++)
        {
            if (_lastBets[i] > 0m) _book.Place(ColorGameRules.All[i], _lastBets[i]);
        }

        _sfx.Play(Sfx.Click, 0.6f);
    }

    // --- Rolling ---------------------------------------------------------------------------
    private void StartRoll()
    {
        _lastBets = _book.Snapshot();
        _result = ColorGameRules.Roll(Random.Shared);

        _state = ColorGameState.Rolling;
        _rollTime = 0f;
        _faceTimer = 0f;
        _matches = null;
        Array.Fill(_dieStopped, false);
        SetMessage("ROLLING...", Color.Gold);

        _sfx.Play(Sfx.Spin, 0.7f, 1.4f);
    }

    private void UpdateRoll(float deltaTime)
    {
        _rollTime += deltaTime;
        _faceTimer -= deltaTime;

        bool changeFaces = _faceTimer <= 0f;
        if (changeFaces) _faceTimer = FaceChangeInterval;

        for (int i = 0; i < _shown.Length; i++)
        {
            if (_rollTime >= DieStopTimes[i])
            {
                if (!_dieStopped[i])
                {
                    _dieStopped[i] = true;
                    _shown[i] = _result[i];
                    _sfx.Play(Sfx.Pop, 0.8f, 0.9f + i * 0.1f);
                }
            }
            else if (changeFaces)
            {
                _shown[i] = ColorGameRules.All[Random.Shared.Next(ColorGameRules.All.Length)];
            }
        }

        if (_rollTime >= RollDuration)
        {
            SettleRound();
        }
    }

    private void SettleRound()
    {
        decimal stake = _book.TotalStake;
        decimal gross = _book.Settle(_result);
        if (gross > 0m) _currency.Add(gross);

        _lastWin = gross;
        _state = ColorGameState.Result;

        for (int i = 0; i < _shown.Length; i++)
        {
            _shown[i] = _result[i];
            _dieStopped[i] = true;
        }

        _matches = ColorGameRules.All.Select(c => ColorGameRules.Matches(c, _result)).ToArray();

        _history.Insert(0, (DiceColor[])_result.Clone());
        if (_history.Count > MaxHistory) _history.RemoveAt(_history.Count - 1);

        bool triple = _result.All(d => d == _result[0]);
        string rollText = triple ? $"TRIPLE {ColorGameRules.NameOf(_result[0])}!" : string.Join(" - ", _result.Select(ColorGameRules.NameOf));

        if (gross > 0m)
        {
            decimal profit = gross - stake;
            SetMessage($"{rollText}   YOU WON {gross:N2}" + (profit > 0m ? $" (+{profit:N2})" : ""), Color.Lime);
            _sfx.Play(triple || gross >= stake * 3m ? Sfx.BigWin : Sfx.Win, 0.9f);
        }
        else
        {
            SetMessage($"{rollText}   NO MATCH", new Color(255, 120, 120, 255));
        }
    }

    // --- Result ----------------------------------------------------------------------------
    private void UpdateResult()
    {
        if (UiButton.WasClicked(ColorGameUI.BtnRoll))
        {
            NewRound();
        }
        else if (UiButton.WasClicked(ColorGameUI.BtnRebet))
        {
            NewRound();
            if (!_showOutOfCoins) Rebet();
        }
    }

    // Clears the settled bets from the board (they are already paid out) and reopens betting.
    private void NewRound()
    {
        _book.Clear();
        _matches = null;
        _state = ColorGameState.Betting;
        SetMessage("PLACE YOUR BETS", Color.LightGray);

        if (_currency.Balance < CasinoChips.MinChip)
        {
            _showOutOfCoins = true;
        }
    }

    // ---------------------------------------------------------------------------------------
    private void SetMessage(string text, Color color)
    {
        _message = text;
        _messageColor = color;
    }

    private void Flash(string text)
    {
        _flashText = text;
        _flashTimer = 2.2f;
    }

    public void Draw()
    {
        ColorGameUI.DrawBackground();

        if (BingoPlusHeader.Draw("COLOR GAME", _currency))
        {
            IsFinished = true;
        }

        float time = (float)Raylib.GetTime();
        for (int i = 0; i < ColorGameRules.DiceCount; i++)
        {
            float shake = _state == ColorGameState.Rolling && !_dieStopped[i] ? 1f : 0f;
            ColorGameUI.DrawDie(i, _shown[i], shake, time);
        }

        bool flashing = _flashTimer > 0f;
        ColorGameUI.DrawStatusLine(
            flashing ? _flashText : _message,
            flashing ? new Color(255, 120, 120, 255) : _messageColor);

        bool betting = _state == ColorGameState.Betting && !_showOutOfCoins;
        ColorGameUI.DrawBoard(_book, _matches, betting);
        ColorGameUI.DrawChipRack(_selectedChip, _state == ColorGameState.Betting);

        string rollLabel = _state switch
        {
            ColorGameState.Betting => "ROLL DICE",
            ColorGameState.Rolling => "ROLLING...",
            _ => "NEW ROUND"
        };
        ColorGameUI.DrawButtons(
            canEdit: _state == ColorGameState.Betting,
            canRoll: _state != ColorGameState.Rolling,
            rollLabel);

        ColorGameUI.DrawInfoPanels(_book.TotalStake, _lastWin, _history);
        ColorGameUI.DrawFooterHint();

        if (_showOutOfCoins)
        {
            OutOfCoinsModal.Draw(CasinoChips.MinChip, _currency.Balance, "place a bet");
        }
    }
}
