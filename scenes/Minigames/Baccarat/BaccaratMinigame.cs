using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public sealed class BaccaratMinigame : IMinigame, IDepositRequester
{
    private static readonly int[] ChipValues = [1, 5, 25, 50, 100, 500, 1000];

    // Top Panel Rectangles
    private static readonly Rectangle ShoeBoxRect = new(44, 92, 336, 72);
    private static readonly Rectangle FastToggleRect = new(392, 106, 138, 44);
    private static readonly Rectangle RoadmapRect = new(780, 92, 456, 72);

    // Betting Zone Rectangles on Felt
    private static readonly Rectangle SpotPlayerPairRect = new(52, 384, 182, 148);
    private static readonly Rectangle SpotPlayerRect = new(248, 376, 266, 160);
    private static readonly Rectangle SpotTieRect = new(528, 376, 224, 160);
    private static readonly Rectangle SpotBankerRect = new(766, 376, 266, 160);
    private static readonly Rectangle SpotBankerPairRect = new(1046, 384, 182, 148);

    // Bottom Control Bar Rectangles
    private static readonly Rectangle ControlBarRect = new(24, 560, 1232, 142);
    private static readonly Rectangle ClearBtnRect = new(762, 578, 104, 46);
    private static readonly Rectangle DoubleBtnRect = new(874, 578, 104, 46);
    private static readonly Rectangle RebetBtnRect = new(762, 634, 216, 46);
    private static readonly Rectangle DealBtnRect = new(994, 578, 240, 102);

    // Out of Coins Modal
    private static readonly Rectangle ModalBoxRect = new(390, 220, 500, 280);
    private static readonly Rectangle ModalDepositBtnRect = new(420, 425, 205, 48);
    private static readonly Rectangle ModalLobbyBtnRect = new(655, 425, 205, 48);
    private static readonly Rectangle ModalCloseBtnRect = new(854, 232, 26, 26);

    private enum BaccaratState
    {
        Betting,
        Dealing,
        Result
    }

    private readonly CurrencyManager _currency;
    private readonly BaccaratShoe _shoe = new();
    private readonly Random _rand = new();

    private BaccaratState _state = BaccaratState.Betting;
    private int _selectedChipIndex = 2; // Default 25 (auto-clamped on start)
    private bool _fastMode;
    private bool _showOutOfCoinsModal;
    private bool _consumedEscapeThisFrame;

    // Bets
    private readonly Dictionary<BaccaratBetSpot, decimal> _bets = new();
    private readonly Dictionary<BaccaratBetSpot, decimal> _lastBets = new();
    private decimal _activeRoundStake;
    private decimal _lastRoundReturn;
    private decimal _lastRoundProfit;

    // Hands & Dealing Animation Queue
    private readonly List<DealtCardVisual> _playerCards = [];
    private readonly List<DealtCardVisual> _bankerCards = [];
    private readonly List<DealStep> _dealQueue = [];
    private int _dealStepIndex;
    private float _dealStepTimer;

    // Predetermined round result (settled at end of dealing or immediately on exit)
    private List<Card> _plannedPlayerCards = [];
    private List<Card> _plannedBankerCards = [];
    private BaccaratOutcome _roundOutcome;
    private bool _roundIsNatural;
    private bool _roundPlayerPair;
    private bool _roundBankerPair;
    private bool _roundSettled;
    private string _resultHeadline = "";
    private string _resultSubtext = "";
    private Color _resultColor = Color.Gold;
    private float _resultPulse;
    private string _shuffleNotice = "";
    private float _shuffleNoticeTimer;

    // History & FX
    private readonly List<BaccaratRoundRecord> _history = [];
    private readonly List<BaccaratSpark> _sparks = [];

    // Audio
    private Sound _sndCard;
    private Sound _sndChip;
    private Sound _sndWin;
    private Sound _sndBigWin;
    private Sound _sndTie;
    private Sound _sndLoss;
    private Sound _sndShuffle;
    private Sound _sndError;
    private bool _audioLoaded;

    public string Name => "Baccarat";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }

    public BaccaratMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
        ResetBetsDict(_bets);
        ResetBetsDict(_lastBets);
    }

    private static void ResetBetsDict(Dictionary<BaccaratBetSpot, decimal> dict)
    {
        dict[BaccaratBetSpot.Player] = 0m;
        dict[BaccaratBetSpot.Banker] = 0m;
        dict[BaccaratBetSpot.Tie] = 0m;
        dict[BaccaratBetSpot.PlayerPair] = 0m;
        dict[BaccaratBetSpot.BankerPair] = 0m;
    }

    private decimal GetTotalBet(Dictionary<BaccaratBetSpot, decimal> dict)
    {
        decimal total = 0m;
        foreach (var v in dict.Values) total += v;
        return total;
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _showOutOfCoinsModal = false;
        _consumedEscapeThisFrame = false;
        _state = BaccaratState.Betting;
        _playerCards.Clear();
        _bankerCards.Clear();
        _dealQueue.Clear();
        _sparks.Clear();
        ResetBetsDict(_bets);
        _roundSettled = true;
        _lastRoundReturn = 0m;
        _lastRoundProfit = 0m;
        _resultHeadline = "";
        _resultSubtext = "";

        // Pick a sensible initial chip for player's balance
        _selectedChipIndex = _currency.Balance >= 25m ? 2 : _currency.Balance >= 5m ? 1 : 0;

        LoadAudio();
    }

    public void Stop()
    {
        // If player exits mid-deal, immediately settle the already-paid round so coins are never lost
        if (_state == BaccaratState.Dealing && !_roundSettled)
        {
            SettleRound(silent: true);
        }
        _sparks.Clear();
        UnloadAudio();
    }

    private void LoadAudio()
    {
        if (_audioLoaded || !Raylib.IsAudioDeviceReady()) return;
        try
        {
            _sndCard = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_card.wav");
            _sndChip = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_chip.wav");
            _sndWin = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_win.wav");
            _sndBigWin = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_bigwin.wav");
            _sndTie = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_tie.wav");
            _sndLoss = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_loss.wav");
            _sndShuffle = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_shuffle.wav");
            _sndError = LoadSoundSafe("scenes/Minigames/Baccarat/assets/audio/sfx_error.wav");
            _audioLoaded = true;
        }
        catch
        {
            _audioLoaded = false;
        }
    }

    private void UnloadAudio()
    {
        if (!_audioLoaded) return;
        UnloadSoundSafe(_sndCard);
        UnloadSoundSafe(_sndChip);
        UnloadSoundSafe(_sndWin);
        UnloadSoundSafe(_sndBigWin);
        UnloadSoundSafe(_sndTie);
        UnloadSoundSafe(_sndLoss);
        UnloadSoundSafe(_sndShuffle);
        UnloadSoundSafe(_sndError);
        _audioLoaded = false;
    }

    private static Sound LoadSoundSafe(string relativePath)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        if (!File.Exists(path)) path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (!File.Exists(path)) return default;
        return Raylib.LoadSound(path);
    }

    private static void UnloadSoundSafe(Sound snd)
    {
        if (snd.FrameCount > 0) Raylib.UnloadSound(snd);
    }

    private void PlaySnd(Sound snd, float volume = 0.7f, float pitch = 1f)
    {
        if (!_audioLoaded || snd.FrameCount == 0) return;
        Raylib.SetSoundVolume(snd, volume);
        Raylib.SetSoundPitch(snd, pitch);
        Raylib.PlaySound(snd);
    }

    private static Rectangle GetSpotRect(BaccaratBetSpot spot) => spot switch
    {
        BaccaratBetSpot.PlayerPair => SpotPlayerPairRect,
        BaccaratBetSpot.Player => SpotPlayerRect,
        BaccaratBetSpot.Tie => SpotTieRect,
        BaccaratBetSpot.Banker => SpotBankerRect,
        BaccaratBetSpot.BankerPair => SpotBankerPairRect,
        _ => SpotPlayerRect
    };

    private static Vector2 GetChipCenter(int index)
    {
        float startX = 248f;
        float spacing = 74f;
        return new Vector2(startX + index * spacing, 634f);
    }

    // ---------------- UPDATE ----------------
    public void Update(float deltaTime)
    {
        if (IsFinished) return;
        _consumedEscapeThisFrame = false;

        float dt = Math.Min(deltaTime, 0.05f);

        if (_showOutOfCoinsModal)
        {
            UpdateOutOfCoinsModal();
            return;
        }

        if (_shuffleNoticeTimer > 0f) _shuffleNoticeTimer = Math.Max(0f, _shuffleNoticeTimer - dt);
        _resultPulse += dt * 4.5f;

        // Fast mode toggle available anytime
        if (UiImageButton.WasClicked(FastToggleRect))
        {
            _fastMode = !_fastMode;
            PlaySnd(_sndChip, 0.45f, _fastMode ? 1.18f : 0.88f);
        }

        UpdateCardAnimations(dt);
        UpdateSparks(dt);

        switch (_state)
        {
            case BaccaratState.Betting:
                UpdateBetting();
                break;
            case BaccaratState.Dealing:
                UpdateDealing(dt);
                break;
            case BaccaratState.Result:
                UpdateResult();
                break;
        }
    }

    private void UpdateBetting()
    {
        Vector2 mouse = GameCanvas.GetMousePosition();

        // 1. Chip rack selection
        for (int i = 0; i < ChipValues.Length; i++)
        {
            Vector2 c = GetChipCenter(i);
            if (Vector2.Distance(mouse, c) <= 30f && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _selectedChipIndex = i;
                PlaySnd(_sndChip, 0.55f, 0.9f + i * 0.04f);
            }
        }

        // 2. Betting spot clicks (Left-click adds chip, Right-click clears that spot)
        decimal chipVal = ChipValues[_selectedChipIndex];
        decimal totalBet = GetTotalBet(_bets);

        foreach (BaccaratBetSpot spot in Enum.GetValues(typeof(BaccaratBetSpot)))
        {
            Rectangle r = GetSpotRect(spot);
            if (!Raylib.CheckCollisionPointRec(mouse, r)) continue;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                if (totalBet + chipVal <= _currency.Balance)
                {
                    _bets[spot] += chipVal;
                    PlaySnd(_sndChip, 0.6f, 1.0f + (float)_rand.NextDouble() * 0.08f);
                }
                else
                {
                    _showOutOfCoinsModal = true;
                    PlaySnd(_sndError, 0.7f);
                }
            }
            else if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                if (_bets[spot] > 0m)
                {
                    _bets[spot] = 0m;
                    PlaySnd(_sndChip, 0.4f, 0.78f);
                }
            }
        }

        // 3. Control buttons: CLEAR, 2X, REBET, DEAL
        if (UiImageButton.WasClicked(ClearBtnRect) && totalBet > 0m)
        {
            ResetBetsDict(_bets);
            PlaySnd(_sndChip, 0.45f, 0.75f);
        }

        if (UiImageButton.WasClicked(DoubleBtnRect) && totalBet > 0m)
        {
            if (totalBet * 2m <= _currency.Balance)
            {
                foreach (BaccaratBetSpot spot in Enum.GetValues(typeof(BaccaratBetSpot)))
                {
                    _bets[spot] *= 2m;
                }
                PlaySnd(_sndChip, 0.65f, 1.15f);
            }
            else
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.7f);
            }
        }

        decimal lastTotal = GetTotalBet(_lastBets);
        if (UiImageButton.WasClicked(RebetBtnRect) && lastTotal > 0m)
        {
            if (lastTotal <= _currency.Balance)
            {
                foreach (var kvp in _lastBets) _bets[kvp.Key] = kvp.Value;
                PlaySnd(_sndChip, 0.6f, 1.05f);
            }
            else
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.7f);
            }
        }

        // Deal CTA or Spacebar
        if (UiImageButton.WasClicked(DealBtnRect) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            if (GetTotalBet(_bets) <= 0m && lastTotal > 0m && lastTotal <= _currency.Balance)
            {
                // Convenience: Space/Deal with empty table automatically re-bets previous layout
                foreach (var kvp in _lastBets) _bets[kvp.Key] = kvp.Value;
            }
            TryStartDeal();
        }
    }

    private void UpdateResult()
    {
        Vector2 mouse = GameCanvas.GetMousePosition();

        // Clicking any chip or betting zone transitions smoothly into Betting for a fresh hand
        for (int i = 0; i < ChipValues.Length; i++)
        {
            Vector2 c = GetChipCenter(i);
            if (Vector2.Distance(mouse, c) <= 30f && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _selectedChipIndex = i;
                EnterFreshBetting();
                PlaySnd(_sndChip, 0.55f, 0.9f + i * 0.04f);
                return;
            }
        }

        foreach (BaccaratBetSpot spot in Enum.GetValues(typeof(BaccaratBetSpot)))
        {
            if (Raylib.CheckCollisionPointRec(mouse, GetSpotRect(spot)) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                EnterFreshBetting();
                decimal chipVal = ChipValues[_selectedChipIndex];
                if (chipVal <= _currency.Balance)
                {
                    _bets[spot] = chipVal;
                    PlaySnd(_sndChip, 0.6f, 1.0f);
                }
                else
                {
                    _showOutOfCoinsModal = true;
                    PlaySnd(_sndError, 0.7f);
                }
                return;
            }
        }

        // CLEAR -> New Bet
        if (UiImageButton.WasClicked(ClearBtnRect))
        {
            EnterFreshBetting();
            PlaySnd(_sndChip, 0.45f, 0.8f);
            return;
        }

        // 2X -> Rebet doubled and enter Betting
        decimal lastTotal = GetTotalBet(_lastBets);
        if (UiImageButton.WasClicked(DoubleBtnRect) && lastTotal > 0m)
        {
            if (lastTotal * 2m <= _currency.Balance)
            {
                EnterFreshBetting();
                foreach (var kvp in _lastBets) _bets[kvp.Key] = kvp.Value * 2m;
                PlaySnd(_sndChip, 0.6f, 1.15f);
            }
            else
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.7f);
            }
            return;
        }

        // REBET -> Put last bets on felt in Betting state
        if (UiImageButton.WasClicked(RebetBtnRect) && lastTotal > 0m)
        {
            if (lastTotal <= _currency.Balance)
            {
                EnterFreshBetting();
                foreach (var kvp in _lastBets) _bets[kvp.Key] = kvp.Value;
                PlaySnd(_sndChip, 0.6f, 1.02f);
            }
            else
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.7f);
            }
            return;
        }

        // REBET & DEAL (Big CTA or Spacebar)
        if (UiImageButton.WasClicked(DealBtnRect) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            if (lastTotal > 0m)
            {
                if (lastTotal <= _currency.Balance)
                {
                    foreach (var kvp in _lastBets) _bets[kvp.Key] = kvp.Value;
                    TryStartDeal();
                }
                else
                {
                    _showOutOfCoinsModal = true;
                    PlaySnd(_sndError, 0.7f);
                }
            }
            else
            {
                EnterFreshBetting();
            }
        }
    }

    private void EnterFreshBetting()
    {
        _state = BaccaratState.Betting;
        _playerCards.Clear();
        _bankerCards.Clear();
        ResetBetsDict(_bets);
        _resultHeadline = "";
        _resultSubtext = "";
    }

    private void TryStartDeal()
    {
        decimal totalBet = GetTotalBet(_bets);
        if (totalBet <= 0m)
        {
            PlaySnd(_sndError, 0.55f);
            return;
        }

        if (_currency.Balance < totalBet || !_currency.TrySpend(totalBet))
        {
            _showOutOfCoinsModal = true;
            PlaySnd(_sndError, 0.75f);
            return;
        }

        // Snapshot last bets for Rebet
        foreach (var kvp in _bets) _lastBets[kvp.Key] = kvp.Value;
        _activeRoundStake = totalBet;
        _roundSettled = false;

        // Reshuffle shoe automatically at cut card
        if (_shoe.NeedsReshuffle)
        {
            _shoe.ResetAndShuffle();
            _shuffleNotice = "NEW 8-DECK SHOE SHUFFLED";
            _shuffleNoticeTimer = 2.6f;
            PlaySnd(_sndShuffle, 0.7f);
        }

        // Draw authentic Punto Banco hands
        Card p1 = _shoe.Draw();
        Card b1 = _shoe.Draw();
        Card p2 = _shoe.Draw();
        Card b2 = _shoe.Draw();

        _plannedPlayerCards = [p1, p2];
        _plannedBankerCards = [b1, b2];

        _roundPlayerPair = p1.Rank == p2.Rank;
        _roundBankerPair = b1.Rank == b2.Rank;
        _roundIsNatural = BaccaratRules.IsNatural(_plannedPlayerCards, _plannedBankerCards);

        if (!_roundIsNatural)
        {
            int pTwo = BaccaratRules.GetHandScore(_plannedPlayerCards);
            int bTwo = BaccaratRules.GetHandScore(_plannedBankerCards);

            int? pThirdVal = null;
            if (BaccaratRules.ShouldPlayerDraw(pTwo))
            {
                Card p3 = _shoe.Draw();
                _plannedPlayerCards.Add(p3);
                pThirdVal = BaccaratRules.GetCardValue(p3);
            }

            if (BaccaratRules.ShouldBankerDraw(bTwo, pThirdVal))
            {
                Card b3 = _shoe.Draw();
                _plannedBankerCards.Add(b3);
            }
        }

        int finalPlayerScore = BaccaratRules.GetHandScore(_plannedPlayerCards);
        int finalBankerScore = BaccaratRules.GetHandScore(_plannedBankerCards);
        _roundOutcome = BaccaratRules.DetermineOutcome(finalPlayerScore, finalBankerScore);

        // Build timed dealing sequence: P1 -> B1 -> P2 -> B2 -> (P3) -> (B3)
        _playerCards.Clear();
        _bankerCards.Clear();
        _dealQueue.Clear();

        _dealQueue.Add(new DealStep(true, _plannedPlayerCards[0], GetPlayerCardTarget(0), false));
        _dealQueue.Add(new DealStep(false, _plannedBankerCards[0], GetBankerCardTarget(0), false));
        _dealQueue.Add(new DealStep(true, _plannedPlayerCards[1], GetPlayerCardTarget(1), false));
        _dealQueue.Add(new DealStep(false, _plannedBankerCards[1], GetBankerCardTarget(1), false));

        if (_plannedPlayerCards.Count == 3)
        {
            _dealQueue.Add(new DealStep(true, _plannedPlayerCards[2], GetPlayerCardTarget(2), true));
        }
        if (_plannedBankerCards.Count == 3)
        {
            _dealQueue.Add(new DealStep(false, _plannedBankerCards[2], GetBankerCardTarget(2), true));
        }

        _dealStepIndex = 0;
        _dealStepTimer = 0.05f;
        _state = BaccaratState.Dealing;
    }

    private static Vector2 GetPlayerCardTarget(int index) => index switch
    {
        0 => new Vector2(362, 266),
        1 => new Vector2(466, 266),
        2 => new Vector2(236, 266), // Sideways 3rd card on outer left
        _ => new Vector2(362, 266)
    };

    private static Vector2 GetBankerCardTarget(int index) => index switch
    {
        0 => new Vector2(814, 266),
        1 => new Vector2(918, 266),
        2 => new Vector2(1044, 266), // Sideways 3rd card on outer right
        _ => new Vector2(814, 266)
    };

    private void UpdateDealing(float dt)
    {
        // Spacebar skips remaining deal delay
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            while (_dealStepIndex < _dealQueue.Count)
            {
                SpawnDealStep(_dealQueue[_dealStepIndex++], instant: true);
            }
            SettleRound(silent: false);
            _state = BaccaratState.Result;
            return;
        }

        _dealStepTimer -= dt;
        if (_dealStepTimer <= 0f)
        {
            if (_dealStepIndex < _dealQueue.Count)
            {
                var step = _dealQueue[_dealStepIndex++];
                SpawnDealStep(step, instant: false);

                bool pauseBeforeThird = _dealStepIndex == 4 && _dealQueue.Count > 4;
                float baseDelay = _fastMode ? 0.16f : 0.42f;
                _dealStepTimer = pauseBeforeThird ? baseDelay * 1.55f : baseDelay;
            }
            else
            {
                // All cards dealt — wait until last card flip finishes
                bool allRevealed = true;
                foreach (var c in _playerCards) if (c.FlipProgress < 0.98f) allRevealed = false;
                foreach (var c in _bankerCards) if (c.FlipProgress < 0.98f) allRevealed = false;

                if (allRevealed)
                {
                    SettleRound(silent: false);
                    _state = BaccaratState.Result;
                }
            }
        }
    }

    private void SpawnDealStep(DealStep step, bool instant)
    {
        Vector2 shoeOrigin = new(1180, 128);
        var visual = new DealtCardVisual
        {
            Card = step.Card,
            StartPos = shoeOrigin,
            TargetPos = step.TargetPos,
            Position = instant ? step.TargetPos : shoeOrigin,
            SlideProgress = instant ? 1f : 0f,
            FlipProgress = instant ? 1f : 0f,
            RotatedSideways = step.Sideways
        };

        if (step.IsPlayer) _playerCards.Add(visual);
        else _bankerCards.Add(visual);

        if (!instant)
        {
            PlaySnd(_sndCard, 0.72f, 0.94f + (float)_rand.NextDouble() * 0.14f);
        }
    }

    private void UpdateCardAnimations(float dt)
    {
        float slideSpeed = _fastMode ? 7.5f : 4.2f;
        float flipSpeed = _fastMode ? 6.5f : 3.6f;

        void AdvanceList(List<DealtCardVisual> list)
        {
            foreach (var c in list)
            {
                if (c.SlideProgress < 1f)
                {
                    c.SlideProgress = Math.Min(1f, c.SlideProgress + dt * slideSpeed);
                    float ease = 1f - MathF.Pow(1f - c.SlideProgress, 3f);
                    c.Position = Vector2.Lerp(c.StartPos, c.TargetPos, ease);
                }
                else if (c.FlipProgress < 1f)
                {
                    c.FlipProgress = Math.Min(1f, c.FlipProgress + dt * flipSpeed);
                }
            }
        }

        AdvanceList(_playerCards);
        AdvanceList(_bankerCards);
    }

    private void SettleRound(bool silent)
    {
        if (_roundSettled) return;
        _roundSettled = true;

        int pScore = BaccaratRules.GetHandScore(_plannedPlayerCards);
        int bScore = BaccaratRules.GetHandScore(_plannedBankerCards);

        decimal totalReturn = 0m;
        bool wonBigSideOrTie = false;

        foreach (BaccaratBetSpot spot in Enum.GetValues(typeof(BaccaratBetSpot)))
        {
            decimal wager = _bets[spot];
            if (wager <= 0m) continue;

            decimal ret = BaccaratRules.CalculateSpotReturn(spot, wager, _roundOutcome, _roundPlayerPair, _roundBankerPair);
            totalReturn += ret;

            if (ret > wager && spot is BaccaratBetSpot.Tie or BaccaratBetSpot.PlayerPair or BaccaratBetSpot.BankerPair)
            {
                wonBigSideOrTie = true;
            }
        }

        if (totalReturn > 0m)
        {
            _currency.Add(totalReturn);
        }

        _lastRoundReturn = totalReturn;
        _lastRoundProfit = totalReturn - _activeRoundStake;

        // Record to Bead Road history
        _history.Add(new BaccaratRoundRecord(_roundOutcome, pScore, bScore, _roundPlayerPair, _roundBankerPair, _roundIsNatural));

        // Build headline text
        string natPrefix = _roundIsNatural ? "NATURAL " : "";
        _resultHeadline = _roundOutcome switch
        {
            BaccaratOutcome.PlayerWin => $"{natPrefix}PLAYER WINS!",
            BaccaratOutcome.BankerWin => $"{natPrefix}BANKER WINS!",
            _ => "TIE HAND!"
        };

        _resultColor = _roundOutcome switch
        {
            BaccaratOutcome.PlayerWin => new Color(96, 165, 250, 255),
            BaccaratOutcome.BankerWin => new Color(248, 113, 113, 255),
            _ => new Color(74, 222, 128, 255)
        };

        if (_lastRoundProfit > 0m)
        {
            _resultSubtext = $"WON {totalReturn:N2} COINS (+{_lastRoundProfit:N2})";
        }
        else if (_lastRoundProfit == 0m && totalReturn > 0m)
        {
            _resultSubtext = $"PUSH — RETURNED {totalReturn:N2} COINS";
        }
        else if (totalReturn > 0m)
        {
            _resultSubtext = $"RETURNED {totalReturn:N2} COINS ({_lastRoundProfit:N2})";
        }
        else
        {
            _resultSubtext = $"NO WIN THIS HAND (-{_activeRoundStake:N2})";
        }

        if (silent) return;

        // Audio + celebration sparks
        if (wonBigSideOrTie)
        {
            PlaySnd(_sndBigWin, 0.85f);
            SpawnCelebrationSparks(42, Color.Gold);
        }
        else if (_lastRoundProfit > 0m)
        {
            PlaySnd(_sndWin, 0.78f);
            SpawnCelebrationSparks(24, _resultColor);
        }
        else if (_roundOutcome == BaccaratOutcome.Tie && totalReturn > 0m)
        {
            PlaySnd(_sndTie, 0.7f);
        }
        else
        {
            PlaySnd(_sndLoss, 0.65f);
        }
    }

    private void SpawnCelebrationSparks(int count, Color primary)
    {
        Vector2 origin = new(GameCanvas.Width * 0.5f, 260f);
        for (int i = 0; i < count; i++)
        {
            float ang = (float)_rand.NextDouble() * MathF.PI * 2f;
            float spd = 90f + (float)_rand.NextDouble() * 220f;
            Color c = i % 2 == 0 ? Color.Gold : primary;
            _sparks.Add(new BaccaratSpark(origin, new Vector2(MathF.Cos(ang) * spd, MathF.Sin(ang) * spd - 60f), c, 0.65f + (float)_rand.NextDouble() * 0.5f));
        }
    }

    private void UpdateSparks(float dt)
    {
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var s = _sparks[i];
            s.Velocity.Y += 290f * dt;
            s.Position += s.Velocity * dt;
            s.Life -= dt;
            if (s.Life <= 0f) _sparks.RemoveAt(i);
        }
    }

    // ---------------- DRAW ----------------
    public void Draw()
    {
        BaccaratRenderer.DrawTableBackground();

        DrawTopStrip();
        DrawHandZones();
        DrawBettingSpots();
        DrawControlBar();

        // Draw sparks above table
        foreach (var s in _sparks)
        {
            float a = Math.Clamp(s.Life / s.MaxLife, 0f, 1f);
            Raylib.DrawCircleV(s.Position, 4.2f * a, new Color(s.Color.R, s.Color.G, s.Color.B, (byte)(255 * a)));
        }

        // Shuffle toast
        if (_shuffleNoticeTimer > 0f)
        {
            float a = Math.Min(1f, _shuffleNoticeTimer);
            Rectangle toast = new(470, 174, 340, 34);
            Raylib.DrawRectangleRounded(toast, 0.4f, 6, new Color(18, 26, 44, (int)(235 * a)));
            Raylib.DrawRectangleRoundedLinesEx(toast, 0.4f, 6, 1.5f, new Color(255, 215, 0, (int)(220 * a)));
            UiFont.DrawCenteredInRect(_shuffleNotice, toast, 13, new Color(255, 215, 0, (int)(255 * a)), isBold: true);
        }

        bool backClicked = BingoPlusHeader.Draw("BACCARAT", _currency);
        if (backClicked && !_showOutOfCoinsModal && !_consumedEscapeThisFrame)
        {
            IsFinished = true;
        }

        if (_showOutOfCoinsModal)
        {
            DrawOutOfCoinsModal();
        }
    }

    private void DrawTopStrip()
    {
        // Left: 8-Deck Shoe Status Panel
        Raylib.DrawRectangleRounded(ShoeBoxRect, 0.14f, 6, new Color(11, 16, 28, 225));
        Raylib.DrawRectangleRoundedLinesEx(ShoeBoxRect, 0.14f, 6, 1.5f, new Color(235, 195, 75, 115));

        UiFont.DrawText("8-DECK CASINO SHOE", ShoeBoxRect.X + 14, ShoeBoxRect.Y + 10, 11, Color.Gold, isBold: true);
        UiFont.DrawText($"{_shoe.Remaining} / {BaccaratShoe.TotalCards} CARDS LEFT", ShoeBoxRect.X + 14, ShoeBoxRect.Y + 26, 14, Color.White, isBold: true);

        // Shoe progress bar
        Rectangle barBg = new(ShoeBoxRect.X + 14, ShoeBoxRect.Y + 50, ShoeBoxRect.Width - 28, 10);
        Raylib.DrawRectangleRounded(barBg, 0.5f, 4, new Color(28, 36, 54, 255));
        float pct = _shoe.Remaining / (float)BaccaratShoe.TotalCards;
        Raylib.DrawRectangleRounded(new Rectangle(barBg.X, barBg.Y, barBg.Width * pct, barBg.Height), 0.5f, 4,
            pct < 0.15f ? new Color(239, 68, 68, 255) : new Color(235, 185, 45, 255));

        // Fast Deal Toggle
        bool fastHov = UiImageButton.IsHovered(FastToggleRect);
        Color fastBg = _fastMode ? new Color(26, 56, 40, 240) : new Color(16, 22, 36, 230);
        Color fastBorder = _fastMode ? Color.Lime : (fastHov ? Color.Gold : new Color(255, 255, 255, 45));
        Raylib.DrawRectangleRounded(FastToggleRect, 0.25f, 6, fastBg);
        Raylib.DrawRectangleRoundedLinesEx(FastToggleRect, 0.25f, 6, _fastMode ? 2f : 1.2f, fastBorder);
        UiFont.DrawCenteredInRect(_fastMode ? "FAST: ON" : "FAST: OFF", FastToggleRect, 13, _fastMode ? Color.Lime : Color.LightGray, isBold: true);

        // Right: Authentic Bead Plate Roadmap
        BaccaratRenderer.DrawRoadmapPanel(RoadmapRect, _history);
    }

    private void DrawHandZones()
    {
        // Live visible scores (only count cards that have flipped past 0.5!)
        int visiblePlayerScore = ComputeVisibleScore(_playerCards);
        int visibleBankerScore = ComputeVisibleScore(_bankerCards);
        bool hasPlayerCards = _playerCards.Count > 0;
        bool hasBankerCards = _bankerCards.Count > 0;

        bool playerWon = _state == BaccaratState.Result && _roundOutcome == BaccaratOutcome.PlayerWin;
        bool bankerWon = _state == BaccaratState.Result && _roundOutcome == BaccaratOutcome.BankerWin;

        // PLAYER Header & Score Bubble
        DrawSideHeader("PLAYER", 386, 174, new Color(59, 130, 246, 255), hasPlayerCards ? visiblePlayerScore : null, playerWon);

        // BANKER Header & Score Bubble
        DrawSideHeader("BANKER", 894, 174, new Color(239, 68, 68, 255), hasBankerCards ? visibleBankerScore : null, bankerWon);

        // Card slot outlines when table is waiting for bets
        if (_state == BaccaratState.Betting)
        {
            DrawCardPlaceholder(GetPlayerCardTarget(0));
            DrawCardPlaceholder(GetPlayerCardTarget(1));
            DrawCardPlaceholder(GetBankerCardTarget(0));
            DrawCardPlaceholder(GetBankerCardTarget(1));

            UiFont.DrawCenteredInRect("PLACE YOUR BETS ON THE FELT", new Rectangle(460, 226, 360, 32), 18, Color.Gold, isBold: true);
            UiFont.DrawCenteredInRect("Click a chip below, then click PLAYER, BANKER, TIE, or PAIRS", new Rectangle(430, 260, 420, 24), 13, new Color(210, 230, 220, 220));
        }

        // Draw Player Cards
        for (int i = 0; i < _playerCards.Count; i++)
        {
            var c = _playerCards[i];
            BaccaratRenderer.DrawAnimatedCard(c.Card, c.Position, c.FlipProgress, c.RotatedSideways, highlightGold: playerWon);
        }

        // Draw Banker Cards
        for (int i = 0; i < _bankerCards.Count; i++)
        {
            var c = _bankerCards[i];
            BaccaratRenderer.DrawAnimatedCard(c.Card, c.Position, c.FlipProgress, c.RotatedSideways, highlightGold: bankerWon);
        }

        // Center VS or Result Banner
        if (_state == BaccaratState.Dealing)
        {
            Raylib.DrawCircle(640, 266, 26, new Color(14, 22, 34, 220));
            Raylib.DrawCircleLines(640, 266, 26, new Color(235, 195, 75, 160));
            UiFont.DrawCenteredInRect("VS", new Rectangle(614, 240, 52, 52), 18, Color.Gold, isBold: true);
        }
        else if (_state == BaccaratState.Result)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_resultPulse);
            Rectangle banner = new(470, 218, 340, 96);
            Raylib.DrawRectangleRounded(banner, 0.2f, 8, new Color(10, 15, 26, 242));
            Raylib.DrawRectangleRoundedLinesEx(banner, 0.2f, 8, 2.5f,
                new Color(_resultColor.R, _resultColor.G, _resultColor.B, (byte)(180 + 75 * pulse)));

            UiFont.DrawCenteredInRect(_resultHeadline, new Rectangle(banner.X, banner.Y + 12, banner.Width, 34), 24, _resultColor, isBold: true);

            Color subCol = _lastRoundProfit > 0m ? Color.Lime : _lastRoundProfit == 0m && _lastRoundReturn > 0m ? Color.Gold : Color.LightGray;
            UiFont.DrawCenteredInRect(_resultSubtext, new Rectangle(banner.X, banner.Y + 48, banner.Width, 24), 15, subCol, isBold: true);

            // Show pair hit callouts if any
            if (_roundPlayerPair || _roundBankerPair)
            {
                string pairTag = (_roundPlayerPair, _roundBankerPair) switch
                {
                    (true, true) => "PLAYER PAIR & BANKER PAIR!",
                    (true, false) => "PLAYER PAIR HIT! (11:1)",
                    _ => "BANKER PAIR HIT! (11:1)"
                };
                UiFont.DrawCenteredInRect(pairTag, new Rectangle(banner.X, banner.Y + 73, banner.Width, 18), 11, Color.Gold, isBold: true);
            }
        }
    }

    private static int ComputeVisibleScore(List<DealtCardVisual> cards)
    {
        int sum = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].FlipProgress >= 0.5f)
            {
                sum += BaccaratRules.GetCardValue(cards[i].Card);
            }
        }
        return sum % 10;
    }

    private static void DrawSideHeader(string label, float centerX, float y, Color accent, int? score, bool isWinner)
    {
        Rectangle pill = new(centerX - 102, y - 16, 204, 34);
        Raylib.DrawRectangleRounded(pill, 0.5f, 8, isWinner ? new Color(36, 48, 32, 245) : new Color(12, 18, 30, 225));
        Raylib.DrawRectangleRoundedLinesEx(pill, 0.5f, 8, isWinner ? 2.5f : 1.5f, isWinner ? Color.Gold : accent);

        UiFont.DrawText(label, pill.X + 20, pill.Y + 7, 17, accent, isBold: true);

        // Score circle on right end of pill
        Vector2 sc = new(pill.X + pill.Width - 22, pill.Y + pill.Height * 0.5f);
        Raylib.DrawCircleV(sc, 15, isWinner ? Color.Gold : accent);
        string scoreStr = score.HasValue ? score.Value.ToString() : "-";
        UiFont.DrawCenteredInRect(scoreStr, new Rectangle(sc.X - 15, sc.Y - 15, 30, 30), 17,
            isWinner ? Color.Black : Color.White, isBold: true);
    }

    private static void DrawCardPlaceholder(Vector2 center)
    {
        Rectangle r = new(center.X - BaccaratRenderer.CardWidth * 0.5f, center.Y - BaccaratRenderer.CardHeight * 0.5f, BaccaratRenderer.CardWidth, BaccaratRenderer.CardHeight);
        Raylib.DrawRectangleRounded(r, 0.12f, 8, new Color(8, 42, 32, 130));
        Raylib.DrawRectangleRoundedLinesEx(r, 0.12f, 8, 1.5f, new Color(235, 195, 75, 55));
    }

    private void DrawBettingSpots()
    {
        DrawSingleSpot(BaccaratBetSpot.PlayerPair, "PLAYER PAIR", "PAYS 11 : 1", new Color(37, 99, 235, 255),
            isWinningSpot: _state == BaccaratState.Result && _roundPlayerPair);

        DrawSingleSpot(BaccaratBetSpot.Player, "PLAYER", "PAYS 1 : 1", new Color(37, 99, 235, 255),
            isWinningSpot: _state == BaccaratState.Result && _roundOutcome == BaccaratOutcome.PlayerWin);

        DrawSingleSpot(BaccaratBetSpot.Tie, "TIE", "PAYS 8 : 1", new Color(22, 163, 74, 255),
            isWinningSpot: _state == BaccaratState.Result && _roundOutcome == BaccaratOutcome.Tie);

        DrawSingleSpot(BaccaratBetSpot.Banker, "BANKER", "PAYS 0.95 : 1", new Color(220, 38, 38, 255),
            isWinningSpot: _state == BaccaratState.Result && _roundOutcome == BaccaratOutcome.BankerWin);

        DrawSingleSpot(BaccaratBetSpot.BankerPair, "BANKER PAIR", "PAYS 11 : 1", new Color(220, 38, 38, 255),
            isWinningSpot: _state == BaccaratState.Result && _roundBankerPair);
    }

    private void DrawSingleSpot(BaccaratBetSpot spot, string title, string odds, Color accent, bool isWinningSpot)
    {
        Rectangle rect = GetSpotRect(spot);
        bool interactive = _state is BaccaratState.Betting or BaccaratState.Result;
        bool hovered = interactive && UiImageButton.IsHovered(rect);
        decimal bet = _bets[spot];

        float pulse = isWinningSpot ? (0.5f + 0.5f * MathF.Sin(_resultPulse * 1.4f)) : 0f;

        Color fill = isWinningSpot
            ? new Color((byte)(accent.R * 0.45f + 45 * pulse), (byte)(accent.G * 0.45f + 45 * pulse), (byte)(accent.B * 0.35f + 20), (byte)230)
            : hovered
                ? new Color((byte)(accent.R * 0.25f + 14), (byte)(accent.G * 0.25f + 38), (byte)(accent.B * 0.25f + 32), (byte)220)
                : new Color((byte)(accent.R * 0.14f + 8), (byte)(accent.G * 0.14f + 34), (byte)(accent.B * 0.14f + 28), (byte)205);

        Color border = isWinningSpot
            ? Color.Gold
            : hovered
                ? Color.Gold
                : bet > 0m
                    ? accent
                    : new Color(235, 195, 75, 105);

        Raylib.DrawRectangleRounded(rect, 0.12f, 8, fill);
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.12f, 8, isWinningSpot || hovered ? 2.6f : 1.5f, border);

        // Title & Odds
        float titleSize = spot is BaccaratBetSpot.PlayerPair or BaccaratBetSpot.BankerPair ? 16f : 21f;
        UiFont.DrawCenteredInRect(title, new Rectangle(rect.X, rect.Y + 12, rect.Width, 26), titleSize,
            isWinningSpot ? Color.Gold : Color.White, isBold: true);
        UiFont.DrawCenteredInRect(odds, new Rectangle(rect.X, rect.Y + 38, rect.Width, 18), 12,
            new Color(240, 210, 120, 220), isBold: true);

        // Chip stack if wager placed
        if (bet > 0m)
        {
            BaccaratRenderer.DrawBetStack(new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height - 54f), bet);
        }
        else if (hovered && _state == BaccaratState.Betting)
        {
            UiFont.DrawCenteredInRect($"+ {ChipValues[_selectedChipIndex]} COINS", new Rectangle(rect.X, rect.Y + rect.Height - 44, rect.Width, 24),
                12, new Color(255, 230, 140, 210), isBold: true);
        }
    }

    private void DrawControlBar()
    {
        Raylib.DrawRectangleRec(ControlBarRect, new Color(17, 23, 38, 255));
        Raylib.DrawRectangleLinesEx(ControlBarRect, 2, new Color(235, 195, 75, 85));

        // Left Readout Box (Total Bet + Last Payout)
        decimal totalBet = _state == BaccaratState.Betting ? GetTotalBet(_bets) : _activeRoundStake;
        Rectangle statPanel = new(40, 576, 166, 110);
        Raylib.DrawRectangleRounded(statPanel, 0.12f, 6, new Color(11, 15, 25, 255));
        Raylib.DrawRectangleRoundedLinesEx(statPanel, 0.12f, 6, 1f, new Color(255, 255, 255, 25));

        UiFont.DrawText("TOTAL BET", statPanel.X + 12, statPanel.Y + 10, 10, Color.Gray, isBold: true);
        UiFont.DrawText($"{totalBet:N2}", statPanel.X + 12, statPanel.Y + 23, 18, Color.Gold, isBold: true);

        UiFont.DrawText("LAST RETURN", statPanel.X + 12, statPanel.Y + 54, 10, Color.Gray, isBold: true);
        Color retCol = _lastRoundReturn > 0m ? Color.Lime : Color.LightGray;
        UiFont.DrawText($"{_lastRoundReturn:N2}", statPanel.X + 12, statPanel.Y + 67, 16, retCol, isBold: true);

        UiFont.DrawText("L-Click: Bet | R-Click: Undo", statPanel.X + 8, statPanel.Y + 92, 9.5f, new Color(145, 158, 180, 255));

        // Center: Interactive Casino Chip Rack
        UiFont.DrawText("SELECT CHIP DENOMINATION", 374, 572, 11, new Color(165, 178, 200, 255), isBold: true);

        decimal currentPending = _state == BaccaratState.Betting ? GetTotalBet(_bets) : 0m;
        for (int i = 0; i < ChipValues.Length; i++)
        {
            Vector2 c = GetChipCenter(i);
            bool selected = i == _selectedChipIndex;
            bool affordable = currentPending + ChipValues[i] <= _currency.Balance;
            if (selected) c.Y -= 8f;

            BaccaratRenderer.DrawCasinoChip(c, 28f, ChipValues[i], selected, affordable);
        }

        // Right Buttons: CLEAR, 2X, REBET, DEAL
        bool isDealing = _state == BaccaratState.Dealing;
        bool hasBets = GetTotalBet(_bets) > 0m;
        bool hasLastBets = GetTotalBet(_lastBets) > 0m;

        DrawActionButton(ClearBtnRect, _state == BaccaratState.Result ? "NEW BET" : "CLEAR", 13,
            new Color(110, 32, 38, 255), enabled: !isDealing && (hasBets || _state == BaccaratState.Result));

        DrawActionButton(DoubleBtnRect, "2X BET", 13,
            new Color(34, 52, 92, 255), enabled: !isDealing && (hasBets || (_state == BaccaratState.Result && hasLastBets)));

        DrawActionButton(RebetBtnRect, "REBET PREVIOUS", 13,
            new Color(44, 54, 78, 255), enabled: !isDealing && hasLastBets);

        // Main DEAL / REBET & DEAL CTA
        string dealTitle = _state switch
        {
            BaccaratState.Dealing => "DEALING...",
            BaccaratState.Result => hasLastBets ? "REBET & DEAL" : "NEW HAND",
            _ => "DEAL"
        };
        string dealSub = _state switch
        {
            BaccaratState.Dealing => "[SPACE] SKIP",
            BaccaratState.Result => hasLastBets ? $"{GetTotalBet(_lastBets):N2} COINS  |  [SPACE]" : "SELECT BET",
            _ => hasBets ? $"{totalBet:N2} COINS  |  [SPACE]" : "CLICK FELT TO BET"
        };

        bool dealEnabled = _state == BaccaratState.Dealing || hasBets || hasLastBets;
        bool dealHov = dealEnabled && UiImageButton.IsHovered(DealBtnRect);
        Color dealBg = !dealEnabled
            ? new Color(36, 44, 60, 255)
            : _state == BaccaratState.Dealing
                ? new Color(180, 125, 22, 255)
                : dealHov
                    ? new Color(34, 197, 94, 255)
                    : new Color(22, 163, 74, 255);

        Raylib.DrawRectangleRounded(DealBtnRect, 0.14f, 8, dealBg);
        Raylib.DrawRectangleRoundedLinesEx(DealBtnRect, 0.14f, 8, 2f,
            dealEnabled ? (dealHov ? Color.White : Color.Gold) : new Color(90, 100, 120, 150));

        UiFont.DrawCenteredInRect(dealTitle, new Rectangle(DealBtnRect.X, DealBtnRect.Y + 20, DealBtnRect.Width, 32), 23,
            dealEnabled ? Color.White : Color.Gray, isBold: true);
        UiFont.DrawCenteredInRect(dealSub, new Rectangle(DealBtnRect.X, DealBtnRect.Y + 58, DealBtnRect.Width, 22), 12,
            dealEnabled ? new Color(225, 255, 232, 255) : Color.Gray, isBold: true);
    }

    private static void DrawActionButton(Rectangle rect, string label, float fontSize, Color baseBg, bool enabled)
    {
        bool hov = enabled && UiImageButton.IsHovered(rect);
        Color fill = !enabled
            ? new Color(26, 32, 46, 200)
            : hov
                ? new Color((byte)Math.Min(255, baseBg.R + 22), (byte)Math.Min(255, baseBg.G + 22), (byte)Math.Min(255, baseBg.B + 22), (byte)255)
                : baseBg;

        Raylib.DrawRectangleRounded(rect, 0.18f, 6, fill);
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.18f, 6, 1.2f,
            !enabled ? new Color(70, 80, 100, 120) : hov ? Color.Gold : new Color(255, 255, 255, 55));
        UiFont.DrawCenteredInRect(label, rect, fontSize, enabled ? Color.White : new Color(110, 120, 140, 200), isBold: true);
    }

    // ---------------- OUT OF COINS MODAL ----------------
    private void UpdateOutOfCoinsModal()
    {
        if (UiImageButton.WasClicked(ModalCloseBtnRect) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _showOutOfCoinsModal = false;
            _consumedEscapeThisFrame = true;
            return;
        }

        if (UiImageButton.WasClicked(ModalDepositBtnRect))
        {
            _showOutOfCoinsModal = false;
            WantsDeposit = true;
            IsFinished = true;
            return;
        }

        if (UiImageButton.WasClicked(ModalLobbyBtnRect))
        {
            _showOutOfCoinsModal = false;
            WantsDeposit = false;
            IsFinished = true;
        }
    }

    private void DrawOutOfCoinsModal()
    {
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(0, 0, 0, 210));
        Raylib.DrawRectangleRec(ModalBoxRect, new Color(18, 24, 38, 255));
        Raylib.DrawRectangleLinesEx(ModalBoxRect, 2, Color.Gold);

        bool closeHov = UiImageButton.IsHovered(ModalCloseBtnRect);
        Raylib.DrawRectangleRec(ModalCloseBtnRect, closeHov ? new Color(220, 38, 38, 200) : new Color(36, 44, 62, 255));
        Raylib.DrawRectangleLinesEx(ModalCloseBtnRect, 1, closeHov ? Color.Red : Color.Gray);
        UiFont.DrawCenteredInRect("X", ModalCloseBtnRect, 14, Color.White, isBold: true);

        UiFont.DrawCenteredInRect("! OUT OF COINS !", new Rectangle(ModalBoxRect.X, ModalBoxRect.Y + 28, ModalBoxRect.Width, 34), 24, Color.Gold, isBold: true);
        UiFont.DrawCenteredInRect("Not enough coins for this wager.", new Rectangle(ModalBoxRect.X + 20, ModalBoxRect.Y + 80, ModalBoxRect.Width - 40, 28), 18, Color.White);
        UiFont.DrawCenteredInRect($"Your current balance: {_currency.Balance:N2} coins", new Rectangle(ModalBoxRect.X + 20, ModalBoxRect.Y + 115, ModalBoxRect.Width - 40, 26), 16, Color.LightGray);
        UiFont.DrawCenteredInRect("Deposit more coins or select a smaller chip.", new Rectangle(ModalBoxRect.X + 20, ModalBoxRect.Y + 152, ModalBoxRect.Width - 40, 22), 14, new Color(160, 174, 192, 255));

        bool depHov = UiImageButton.IsHovered(ModalDepositBtnRect);
        Raylib.DrawRectangleRec(ModalDepositBtnRect, depHov ? new Color(40, 200, 100, 255) : new Color(34, 160, 80, 255));
        Raylib.DrawRectangleLinesEx(ModalDepositBtnRect, 2, depHov ? Color.Lime : Color.Gold);
        UiFont.DrawCenteredInRect("DEPOSIT MORE", ModalDepositBtnRect, 16, Color.White, isBold: true);

        bool lobHov = UiImageButton.IsHovered(ModalLobbyBtnRect);
        Raylib.DrawRectangleRec(ModalLobbyBtnRect, lobHov ? new Color(71, 85, 105, 255) : new Color(51, 65, 85, 255));
        Raylib.DrawRectangleLinesEx(ModalLobbyBtnRect, 1, lobHov ? Color.White : Color.Gray);
        UiFont.DrawCenteredInRect("RETURN TO LOBBY", ModalLobbyBtnRect, 15, Color.LightGray, isBold: true);
    }

    // ---------------- HELPER TYPES ----------------
    private sealed class DealtCardVisual
    {
        public Card Card = null!;
        public Vector2 StartPos;
        public Vector2 TargetPos;
        public Vector2 Position;
        public float SlideProgress;
        public float FlipProgress;
        public bool RotatedSideways;
    }

    private sealed record DealStep(bool IsPlayer, Card Card, Vector2 TargetPos, bool Sideways);

    private sealed class BaccaratSpark
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Life;
        public float MaxLife;

        public BaccaratSpark(Vector2 pos, Vector2 vel, Color color, float life)
        {
            Position = pos;
            Velocity = vel;
            Color = color;
            Life = life;
            MaxLife = life;
        }
    }
}
