// European (single-zero) Roulette minigame.
// Flow: Betting -> Spinning -> Result -> (NEW ROUND) -> Betting ...
//  * Chips are taken from the wallet as soon as they are placed; UNDO/CLEAR/leaving refunds them.
//  * The winning number is picked when the spin starts, then the wheel/ball animation is built to land on it.
//  * Winnings are paid when the ball settles (or immediately if the player leaves mid-spin).

using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public sealed class RouletteMinigame : IMinigame, IDepositRequester
{
    private enum RouletteState { Betting, Spinning, Result }

    private const float SpinDuration = 6.5f;
    private const int MaxHistory = 20;

    private readonly CurrencyManager _currency;
    private readonly RouletteBetBook _book = new();
    private readonly CasinoSfx _sfx = new();
    private readonly List<int> _history = [];              // newest first
    private List<(RouletteBetSpot Spot, decimal Amount)> _lastBets = [];

    private RouletteState _state;
    private int _selectedChip = 1;
    private bool _showOutOfCoins;

    // Round data
    private int _result;
    private int? _winningNumber;
    private decimal _lastWin;
    private string _message = "PLACE YOUR BETS";
    private Color _messageColor = Color.LightGray;
    private float _flashTimer;
    private string _flashText = "";

    // Animation
    private float _spinTime;
    private float _wheelAngle;
    private float _wheelStartAngle;
    private float _wheelTotalTurn;
    private float _ballAngle;
    private float _ballRadius = RouletteUI.BallTrackR;
    private float _ballExtraTurn;
    private bool _showBall;

    public string Name => "Roulette";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }

    public RouletteMinigame(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _book.Clear();
        _history.Clear();
        _lastBets = [];
        _winningNumber = null;
        _lastWin = 0m;
        _showBall = false;
        _state = RouletteState.Betting;
        SetMessage("PLACE YOUR BETS", Color.LightGray);

        RouletteUI.LoadArt();
        _sfx.Load();
        _showOutOfCoins = _currency.Balance < CasinoChips.MinChip;
    }

    public void Stop()
    {
        // Leaving mid-spin must not dodge a loss (or a win): settle the round first.
        if (_state == RouletteState.Spinning)
        {
            SettleRound();
        }
        else if (_state == RouletteState.Betting && _book.HasBets)
        {
            // Chips that were never spun go back to the wallet.
            decimal refund = _book.Clear();
            if (refund > 0m) _currency.Add(refund);
        }

        _sfx.Stop(Sfx.Spin);
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
            case RouletteState.Betting:
                _wheelAngle = (_wheelAngle + 14f * deltaTime) % 360f;
                UpdateBetting();
                break;
            case RouletteState.Spinning:
                UpdateSpin(deltaTime);
                break;
            case RouletteState.Result:
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
            if (UiImageButton.WasClicked(RouletteUI.ChipBounds(i)))
            {
                _selectedChip = i;
                _sfx.Play(Sfx.Click, 0.6f);
            }
        }

        RouletteBetSpot? spot = RouletteTable.HitTest(GameCanvas.GetMousePosition());
        if (spot != null)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) PlaceChip(spot);
            else if (Raylib.IsMouseButtonPressed(MouseButton.Right)) RemoveBet(spot);
        }

        if (UiButton.WasClicked(RouletteUI.BtnUndo))
        {
            if (_book.TryUndo(out decimal refund))
            {
                _currency.Add(refund);
                _sfx.Play(Sfx.Click, 0.5f, 0.8f);
            }
        }
        else if (UiButton.WasClicked(RouletteUI.BtnClear))
        {
            decimal refund = _book.Clear();
            if (refund > 0m)
            {
                _currency.Add(refund);
                _sfx.Play(Sfx.Click, 0.5f, 0.8f);
            }
        }
        else if (UiButton.WasClicked(RouletteUI.BtnRebet))
        {
            Rebet();
        }
        else if (UiButton.WasClicked(RouletteUI.BtnSpin))
        {
            if (!_book.HasBets)
            {
                Flash("PLACE A BET FIRST");
                _sfx.Play(Sfx.Error, 0.6f);
            }
            else
            {
                StartSpin();
            }
        }
    }

    private void PlaceChip(RouletteBetSpot spot)
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

        _book.Place(spot, chip);
        _sfx.Play(Sfx.Click, 0.6f);
    }

    private void RemoveBet(RouletteBetSpot spot)
    {
        decimal refund = _book.RemoveSpot(spot);
        if (refund > 0m)
        {
            _currency.Add(refund);
            _sfx.Play(Sfx.Click, 0.5f, 0.8f);
        }
    }

    private void Rebet()
    {
        if (_lastBets.Count == 0)
        {
            Flash("NO PREVIOUS BET");
            _sfx.Play(Sfx.Error, 0.6f);
            return;
        }

        decimal total = _lastBets.Sum(b => b.Amount);
        if (total > _currency.Balance + _book.TotalStake)
        {
            Flash("NOT ENOUGH COINS FOR REBET");
            _sfx.Play(Sfx.Error, 0.6f);
            return;
        }

        decimal refund = _book.Clear();
        if (refund > 0m) _currency.Add(refund);

        _currency.TrySpend(total);
        foreach (var (spot, amount) in _lastBets)
        {
            _book.Place(spot, amount);
        }

        _sfx.Play(Sfx.Click, 0.6f);
    }

    // --- Spinning --------------------------------------------------------------------------
    private void StartSpin()
    {
        _lastBets = _book.Snapshot();
        _result = RouletteRules.Spin(Random.Shared);

        _state = RouletteState.Spinning;
        _spinTime = 0f;
        _winningNumber = null;
        _showBall = true;
        SetMessage("NO MORE BETS...", Color.Gold);

        // The wheel turns clockwise a few times; the ball laps the other way and drops into the winning pocket.
        _wheelStartAngle = _wheelAngle;
        _wheelTotalTurn = (3f + (float)Random.Shared.NextDouble() * 1.5f) * 360f;
        _ballExtraTurn = (7f + (float)Random.Shared.NextDouble() * 2f) * 360f;

        _sfx.Play(Sfx.Spin, 0.8f);
    }

    private void UpdateSpin(float deltaTime)
    {
        _spinTime += deltaTime;
        float p = Math.Clamp(_spinTime / SpinDuration, 0f, 1f);

        float wheelEase = 1f - MathF.Pow(1f - p, 3f);
        float ballEase = 1f - MathF.Pow(1f - p, 2f);

        float finalWheel = _wheelStartAngle + _wheelTotalTurn;
        _wheelAngle = _wheelStartAngle + _wheelTotalTurn * wheelEase;

        // The ball ends exactly on the winning pocket of the (by then stopped) wheel.
        float step = 360f / RouletteRules.PocketCount;
        float pocketCenter = (RouletteRules.PocketIndexOf(_result) + 0.5f) * step;
        float finalBall = finalWheel + pocketCenter;
        _ballAngle = finalBall + _ballExtraTurn * (1f - ballEase);

        // The ball stays on the outer track, then drops inward during the last third.
        float drop = Math.Clamp((p - 0.62f) / 0.38f, 0f, 1f);
        _ballRadius = RouletteUI.BallTrackR + (RouletteUI.BallPocketR - RouletteUI.BallTrackR) * drop * drop;

        if (p >= 1f)
        {
            _wheelAngle = finalWheel % 360f;
            _ballAngle = finalBall % 360f;
            _sfx.Stop(Sfx.Spin);
            _sfx.Play(Sfx.Pop, 0.9f);
            SettleRound();
        }
    }

    private void SettleRound()
    {
        decimal stake = _book.TotalStake;
        decimal gross = _book.Settle(_result);
        if (gross > 0m) _currency.Add(gross);

        _lastWin = gross;
        _winningNumber = _result;
        _showBall = true;
        _state = RouletteState.Result;

        _history.Insert(0, _result);
        if (_history.Count > MaxHistory) _history.RemoveAt(_history.Count - 1);

        string colorName = RouletteRules.ColorOf(_result).ToString().ToUpperInvariant();
        if (gross > 0m)
        {
            decimal profit = gross - stake;
            SetMessage($"{_result} {colorName}  -  YOU WON {gross:N2}" + (profit > 0m ? $" (+{profit:N2})" : ""), Color.Lime);
            _sfx.Play(gross >= stake * 10m ? Sfx.BigWin : Sfx.Win, 0.9f);
        }
        else
        {
            SetMessage($"{_result} {colorName}  -  NO WIN. BETTER LUCK NEXT SPIN", new Color(255, 120, 120, 255));
        }
    }

    // --- Result ----------------------------------------------------------------------------
    private void UpdateResult()
    {
        if (UiButton.WasClicked(RouletteUI.BtnSpin))
        {
            NewRound();
        }
        else if (UiButton.WasClicked(RouletteUI.BtnRebet))
        {
            NewRound();
            if (!_showOutOfCoins) Rebet();
        }
    }

    // Clears the settled bets from the table (they are already paid out) and reopens betting.
    private void NewRound()
    {
        _book.Clear();
        _winningNumber = null;
        _showBall = false;
        _state = RouletteState.Betting;
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
        RouletteUI.DrawBackground();

        if (BingoPlusHeader.Draw("EUROPEAN ROULETTE", _currency))
        {
            IsFinished = true;
        }

        bool betting = _state == RouletteState.Betting && !_showOutOfCoins;
        RouletteBetSpot? hover = betting ? RouletteTable.HitTest(GameCanvas.GetMousePosition()) : null;

        RouletteUI.DrawWheel(_wheelAngle, _ballAngle, _ballRadius, _showBall);
        RouletteUI.DrawTable(_book, hover, _winningNumber);
        RouletteUI.DrawHistory(_history);
        RouletteUI.DrawChipRack(_selectedChip, _state == RouletteState.Betting);

        string spinLabel = _state switch
        {
            RouletteState.Betting => "SPIN",
            RouletteState.Spinning => "SPINNING...",
            _ => "NEW ROUND"
        };
        RouletteUI.DrawButtons(
            canEdit: _state == RouletteState.Betting,
            canSpin: _state != RouletteState.Spinning,
            spinLabel);

        bool flashing = _flashTimer > 0f;
        RouletteUI.DrawInfoPanel(
            _book.TotalStake,
            _lastWin,
            _winningNumber ?? (_history.Count > 0 ? _history[0] : null),
            flashing ? _flashText : _message,
            flashing ? new Color(255, 120, 120, 255) : _messageColor);

        RouletteUI.DrawPayoutTable();
        RouletteUI.DrawFooterHint(hover, betting);

        if (_showOutOfCoins)
        {
            OutOfCoinsModal.Draw(CasinoChips.MinChip, _currency.Balance, "place a bet");
        }
    }
}
