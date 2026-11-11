using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public enum SlotSymbolType
{
    Yellow,
    Blue,
    Orange,
    Cyan,
    Green,
    Purple,
    Heart,
    Swirl,
    Star,
    Bomb,
    Scatter
}

public sealed class SlotCell
{
    public SlotSymbolType Symbol;
    public float Y;
    public float TargetY;
    public float VelocityY;
    public bool IsWinning;
    public float Scale = 1f;
    public int BombMultiplier = 1;
    public bool IsEmpty;
}

public sealed class SlotsMinigame : IMinigame
{
    public const int Cols = 6;
    public const int Rows = 5;
    private const float CellSize = 84f;
    private const float CellGap = 8f;

    // Grid Coordinates
    public const float GridStartX = 368f;
    public const float GridStartY = 80f;

    private readonly CurrencyManager _currency;
    private readonly Random _rand = new();
    private readonly SlotsFX _fx = new();

    // Textures
    private readonly Dictionary<SlotSymbolType, Texture2D> _symbolTextures = [];
    private Texture2D _texBtnSpin;
    private Texture2D _texBtnMinus;
    private Texture2D _texBtnPlus;
    private Texture2D _texBtnAuto;
    private Texture2D _texBtnTurbo;
    private Texture2D _texBannerBigWin;
    private Texture2D _texBannerMegaWin;
    private Texture2D _texBannerSensational;

    // Audio SFX
    private Sound _sndSpin;
    private Sound _sndReelDrop;
    private Sound _sndMatchPop;
    private Sound _sndTumble;
    private Sound _sndBombExplode;
    private Sound _sndWinChime;
    private Sound _sndBigWin;
    private Sound _sndUiClick;
    private Sound _sndError;
    private bool _isAudioLoaded;

    // Grid Data
    private readonly SlotCell[,] _grid = new SlotCell[Cols, Rows];

    // Betting
    private static readonly decimal[] BetPresets = [1m, 2m, 5m, 10m, 20m, 50m, 100m, 250m, 500m];
    private int _betIndex = 3; // Default 10m
    private decimal CurrentBet => BetPresets[_betIndex];

    // Game State
    private enum SlotsState
    {
        Idle,
        DroppingNewSpin,
        Evaluating,
        ExplodingWins,
        Tumbling,
        DetonatingBombs,
        WinCelebration
    }

    private SlotsState _state = SlotsState.Idle;
    private float _stateTimer;

    // Tumble & Wins
    private decimal _roundWin;
    private decimal _displayWin;
    private int _tumbleCombo = 1;
    private int _accumulatedBombMultiplier = 0;
    private readonly List<SlotCell> _activeBombs = [];

    // Celebration
    private string _winTierName = "";
    private Texture2D _winBanner;
    private float _celebrationTimer;

    // Options
    private bool _turboMode = false;
    private bool _autoSpin = false;
    private float _autoSpinDelay;

    public string Name => "Slots";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }
    private bool _showOutOfCoinsModal = false;

    private static readonly SlotSymbolType[] RegularCandies =
    [
        SlotSymbolType.Yellow,
        SlotSymbolType.Blue,
        SlotSymbolType.Orange,
        SlotSymbolType.Cyan,
        SlotSymbolType.Green,
        SlotSymbolType.Purple,
        SlotSymbolType.Heart,
        SlotSymbolType.Swirl,
        SlotSymbolType.Star
    ];

    private SlotSymbolType _favoredSymbolA = SlotSymbolType.Yellow;
    private SlotSymbolType _favoredSymbolB = SlotSymbolType.Blue;

    private void PickFavoredSymbols()
    {
        int idxA = _rand.Next(RegularCandies.Length);
        int idxB = (_rand.Next(RegularCandies.Length - 1) + idxA + 1) % RegularCandies.Length;
        _favoredSymbolA = RegularCandies[idxA];
        _favoredSymbolB = RegularCandies[idxB];
    }

    public SlotsMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
        InitGrid();
        LoadAssets();
    }

    private void InitGrid()
    {
        PickFavoredSymbols();
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float targetY = GridStartY + r * (CellSize + CellGap);
                _grid[c, r] = new SlotCell
                {
                    Symbol = GetRandomSymbol(),
                    Y = targetY,
                    TargetY = targetY,
                    IsEmpty = false
                };
            }
        }
    }

    private void LoadAssets()
    {
        _fx.LoadAssets();

        _symbolTextures[SlotSymbolType.Yellow] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_yellow.png");
        _symbolTextures[SlotSymbolType.Blue] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_blue.png");
        _symbolTextures[SlotSymbolType.Orange] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_orange.png");
        _symbolTextures[SlotSymbolType.Cyan] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_cyan.png");
        _symbolTextures[SlotSymbolType.Green] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_green.png");
        _symbolTextures[SlotSymbolType.Purple] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_purple.png");
        _symbolTextures[SlotSymbolType.Heart] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_heart.png");
        _symbolTextures[SlotSymbolType.Swirl] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_swirl.png");
        _symbolTextures[SlotSymbolType.Star] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_star.png");
        _symbolTextures[SlotSymbolType.Bomb] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_bomb.png");
        _symbolTextures[SlotSymbolType.Scatter] = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sym_scatter.png");

        _texBtnSpin = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/btn_spin.png");
        _texBtnMinus = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/btn_minus.png");
        _texBtnPlus = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/btn_plus.png");
        _texBtnAuto = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/btn_auto.png");
        _texBtnTurbo = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/btn_turbo.png");

        _texBannerBigWin = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/banner_bigwin.png");
        _texBannerMegaWin = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/banner_megawin.png");
        _texBannerSensational = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/banner_sensational.png");

        if (Raylib.IsAudioDeviceReady())
        {
            try
            {
                _sndSpin = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_spin.wav");
                _sndReelDrop = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_reel_drop.wav");
                _sndMatchPop = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_match_pop.wav");
                _sndTumble = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_tumble.wav");
                _sndBombExplode = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_bomb_explode.wav");
                _sndWinChime = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_win_chime.wav");
                _sndBigWin = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_bigwin.wav");
                _sndUiClick = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_ui_click.wav");
                _sndError = Raylib.LoadSound("scenes/Minigames/Slots/assets/audio/sfx_error.wav");
                _isAudioLoaded = true;
            }
            catch
            {
                _isAudioLoaded = false;
            }
        }
    }

    private void PlaySnd(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (!_isAudioLoaded) return;
        Raylib.SetSoundVolume(sound, volume);
        Raylib.SetSoundPitch(sound, pitch);
        Raylib.PlaySound(sound);
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _showOutOfCoinsModal = false;
        _state = SlotsState.Idle;
        _roundWin = 0m;
        _displayWin = 0m;
        _tumbleCombo = 1;
        _accumulatedBombMultiplier = 0;
        _activeBombs.Clear();
        _fx.Clear();
    }

    public void Stop()
    {
        IsFinished = true;
        _autoSpin = false;
        _fx.Clear();
        if (_isAudioLoaded)
        {
            if (Raylib.IsSoundPlaying(_sndSpin)) Raylib.StopSound(_sndSpin);
            if (Raylib.IsSoundPlaying(_sndBigWin)) Raylib.StopSound(_sndBigWin);
        }
    }

    private SlotSymbolType GetRandomSymbol(bool isTumble = false, int combo = 1)
    {
        int roll = _rand.Next(1000);

        if (!isTumble)
        {
            // Initial Spin: Moderate Clustered Reel Weighting (Realistic Casino Hit Rate ~20-24%)
            // Favored Symbol A has 16.0% chance (160)
            // Favored Symbol B has 13.5% chance (135)
            // Scatter has 1.5% chance (15)
            // The remaining 7 regular candies share the remaining 690 (~98-99 each)
            if (roll < 160) return _favoredSymbolA;
            if (roll < 295) return _favoredSymbolB;
            if (roll < 310) return SlotSymbolType.Scatter;

            int normRoll = roll - 310;
            int otherIndex = 0;
            for (int i = 0; i < RegularCandies.Length; i++)
            {
                if (RegularCandies[i] == _favoredSymbolA || RegularCandies[i] == _favoredSymbolB)
                    continue;

                if (normRoll < (otherIndex + 1) * 98)
                    return RegularCandies[i];

                otherIndex++;
            }
            return SlotSymbolType.Star;
        }
        else
        {
            // Cascades / Tumbles:
            // Multiplier Bombs unlock on Cascade 2+ (combo >= 2):
            // Combo 2: 2.5% (25 / 1000) per tumbling cell
            // Combo 3: 4.5% (45 / 1000) per tumbling cell
            // Combo 4+: 7.5% (75 / 1000) per tumbling cell
            int bombChance = combo < 2 ? 0 : Math.Min(75, 25 + (combo - 2) * 20);
            if (roll < bombChance)
            {
                return SlotSymbolType.Bomb;
            }

            int remainingRoll = roll - bombChance;
            int totalPool = 1000 - bombChance;

            // In tumbles, distribute cleanly across all candies with 1% scatter
            int scatterPool = (int)(totalPool * 0.01f);
            if (remainingRoll < scatterPool) return SlotSymbolType.Scatter;

            int candyRoll = remainingRoll - scatterPool;
            int candyPool = totalPool - scatterPool;
            int slice = Math.Max(1, candyPool / RegularCandies.Length);

            int candyIndex = Math.Min(RegularCandies.Length - 1, candyRoll / slice);
            return RegularCandies[candyIndex];
        }
    }

    private Color GetSymbolColor(SlotSymbolType sym) => sym switch
    {
        SlotSymbolType.Yellow => new Color(255, 230, 40, 255),
        SlotSymbolType.Blue => new Color(50, 180, 255, 255),
        SlotSymbolType.Orange => new Color(255, 140, 0, 255),
        SlotSymbolType.Cyan => new Color(0, 230, 240, 255),
        SlotSymbolType.Green => new Color(40, 240, 100, 255),
        SlotSymbolType.Purple => new Color(190, 60, 240, 255),
        SlotSymbolType.Heart => new Color(255, 60, 80, 255),
        SlotSymbolType.Swirl => new Color(255, 100, 180, 255),
        SlotSymbolType.Star => new Color(255, 215, 0, 255),
        SlotSymbolType.Bomb => new Color(255, 80, 0, 255),
        SlotSymbolType.Scatter => new Color(255, 120, 200, 255),
        _ => Color.White
    };

    public void Update(float deltaTime)
    {
        if (IsFinished) return;

        if (_showOutOfCoinsModal)
        {
            UpdateOutOfCoinsModal();
            return;
        }

        _fx.Update(deltaTime);

        // Win rolling number lerp
        if (_displayWin < _roundWin)
        {
            decimal step = Math.Max(0.5m, (_roundWin - _displayWin) * (decimal)(deltaTime * 8f));
            _displayWin = Math.Min(_roundWin, _displayWin + step);
        }

        // Global UI Toggles (Can be clicked at ANY time, even while spinning/tumbling!)
        Rectangle autoRect = new(905, 625, 84, 36);
        Rectangle spinRect = new(1015, 595, 96, 96);
        Rectangle turboRect = new(810, 625, 84, 36);

        if (UiImageButton.WasClicked(turboRect))
        {
            _turboMode = !_turboMode;
            PlaySnd(_sndUiClick, 0.65f);
        }

        if (UiImageButton.WasClicked(autoRect))
        {
            _autoSpin = !_autoSpin;
            PlaySnd(_sndUiClick, 0.65f);
            if (_autoSpin && _state == SlotsState.Idle)
            {
                TryTriggerSpin();
            }
        }
        else if (_autoSpin && UiImageButton.WasClicked(spinRect))
        {
            // Clicking STOP while Auto is running immediately stops Auto!
            _autoSpin = false;
            PlaySnd(_sndUiClick, 0.65f);
        }

        // State Machine
        switch (_state)
        {
            case SlotsState.Idle:
                UpdateIdle(deltaTime);
                break;
            case SlotsState.DroppingNewSpin:
                UpdateDropping(deltaTime);
                break;
            case SlotsState.Evaluating:
                UpdateEvaluating();
                break;
            case SlotsState.ExplodingWins:
                UpdateExploding(deltaTime);
                break;
            case SlotsState.Tumbling:
                UpdateTumbling(deltaTime);
                break;
            case SlotsState.DetonatingBombs:
                UpdateDetonatingBombs(deltaTime);
                break;
            case SlotsState.WinCelebration:
                UpdateCelebration(deltaTime);
                break;
        }
    }

    private void UpdateIdle(float deltaTime)
    {
        // Auto-spin delay check
        if (_autoSpin)
        {
            _autoSpinDelay += deltaTime;
            if (_autoSpinDelay >= (_turboMode ? 0.2f : 0.45f))
            {
                _autoSpinDelay = 0f;
                TryTriggerSpin();
            }
        }

        // Manual Spin Button (when auto is not active)
        Rectangle spinRect = new(1015, 595, 96, 96);
        if (!_autoSpin && (UiImageButton.WasClicked(spinRect) || Raylib.IsKeyPressed(KeyboardKey.Space)))
        {
            TryTriggerSpin();
        }

        // Bet Minus
        Rectangle minusRect = new(175, 620, 46, 46);
        if (UiImageButton.WasClicked(minusRect))
        {
            if (_betIndex > 0)
            {
                _betIndex--;
                PlaySnd(_sndUiClick, 0.65f);
            }
        }

        // Bet Plus
        Rectangle plusRect = new(345, 620, 46, 46);
        if (UiImageButton.WasClicked(plusRect))
        {
            if (_betIndex < BetPresets.Length - 1)
            {
                _betIndex++;
                PlaySnd(_sndUiClick, 0.65f);
            }
        }
    }

    private void TryTriggerSpin()
    {
        if (_currency.Balance < CurrentBet || !_currency.TrySpend(CurrentBet))
        {
            _autoSpin = false;
            _showOutOfCoinsModal = true;
            PlaySnd(_sndError, 0.9f);
            return;
        }

        // Begin Spin
        PickFavoredSymbols();
        _roundWin = 0m;
        _displayWin = 0m;
        _tumbleCombo = 1;
        _accumulatedBombMultiplier = 0;
        _activeBombs.Clear();

        // Launch drop animation
        float dropSpeed = _turboMode ? 2800f : 1600f;
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float targetY = GridStartY + r * (CellSize + CellGap);
                float startY = -CellSize * (Rows - r + 1) - c * 60f;

                var sym = GetRandomSymbol(isTumble: false, combo: 1);
                int bombMult = sym == SlotSymbolType.Bomb ? GetRandomBombMultiplier() : 1;

                _grid[c, r] = new SlotCell
                {
                    Symbol = sym,
                    Y = startY,
                    TargetY = targetY,
                    VelocityY = dropSpeed,
                    IsEmpty = false,
                    IsWinning = false,
                    Scale = 1f,
                    BombMultiplier = bombMult
                };
            }
        }

        _state = SlotsState.DroppingNewSpin;
        PlaySnd(_sndSpin, 0.85f);
    }

    private int GetRandomBombMultiplier()
    {
        int roll = _rand.Next(100);
        if (roll < 60) return 2;   // 60% -> 2x
        if (roll < 85) return 3;   // 25% -> 3x
        if (roll < 95) return 5;   // 10% -> 5x
        if (roll < 99) return 10;  //  4% -> 10x
        return 25;                 //  1% -> 25x Jackpot Bomb!
    }

    private void UpdateDropping(float deltaTime)
    {
        bool allLanded = true;
        float speed = _turboMode ? 2800f : 1600f;

        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                var cell = _grid[c, r];
                if (cell.Y < cell.TargetY)
                {
                    cell.Y += speed * deltaTime;
                    if (cell.Y >= cell.TargetY)
                    {
                        cell.Y = cell.TargetY;
                        // Landing bounce shake
                        if (r == Rows - 1) _fx.AddTrauma(0.06f);
                    }
                    else
                    {
                        allLanded = false;
                    }
                }
            }
        }

        if (allLanded)
        {
            PlaySnd(_sndReelDrop, 0.85f);
            _state = SlotsState.Evaluating;
        }
    }

    private void UpdateEvaluating()
    {
        // 1. Count symbols across all 30 cells (excluding Scatter and Bomb)
        Dictionary<SlotSymbolType, int> counts = [];
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                var cell = _grid[c, r];
                if (cell.IsEmpty) continue;

                if (!counts.ContainsKey(cell.Symbol)) counts[cell.Symbol] = 0;
                counts[cell.Symbol]++;

                // Track bombs
                if (cell.Symbol == SlotSymbolType.Bomb && !_activeBombs.Contains(cell))
                {
                    _activeBombs.Add(cell);
                }
            }
        }

        // 2. Check cluster wins (8+ of any regular symbol)
        bool hasWin = false;
        decimal cascadeWin = 0m;

        foreach (var (sym, count) in counts)
        {
            if (sym == SlotSymbolType.Bomb) continue;

            if (sym == SlotSymbolType.Scatter)
            {
                // 4+ Scatters pay balanced casino bonus
                if (count >= 4)
                {
                    hasWin = true;
                    decimal mult = count >= 6 ? 40m : count == 5 ? 15m : 5m;
                    cascadeWin += CurrentBet * mult;
                    MarkWinningSymbol(sym);
                }
                continue;
            }

            if (count >= 8)
            {
                hasWin = true;
                decimal basePayout = GetSymbolPayout(sym, count) * CurrentBet;
                cascadeWin += basePayout;
                MarkWinningSymbol(sym);
            }
        }

        if (hasWin)
        {
            _roundWin += cascadeWin;
            _stateTimer = _turboMode ? 0.35f : 0.65f;
            _state = SlotsState.ExplodingWins;
            float pitch = Math.Min(1.8f, 1.0f + (_tumbleCombo - 1) * 0.08f);
            PlaySnd(_sndMatchPop, 0.85f, pitch);
        }
        else
        {
            // No more cluster wins
            if (_activeBombs.Count > 0 && _roundWin > 0)
            {
                _stateTimer = 0.5f;
                _state = SlotsState.DetonatingBombs;
            }
            else
            {
                FinishRound();
            }
        }
    }

    private void MarkWinningSymbol(SlotSymbolType sym)
    {
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                if (_grid[c, r].Symbol == sym && !_grid[c, r].IsEmpty)
                {
                    _grid[c, r].IsWinning = true;
                }
            }
        }
    }

    private decimal GetSymbolPayout(SlotSymbolType sym, int count)
    {
        // Balanced Casino Paytable (Authentic Sweet Bonanza RTP ~96%):
        // 8-9 matching symbols provide teaser / partial return (0.5x to 8x)
        // 10-11 steps up to solid payout (1.2x to 16x)
        // 12+ awards strong jackpot tier base payouts (2.5x to 60x)!
        (decimal p8, decimal p10, decimal p12) = sym switch
        {
            SlotSymbolType.Yellow => (0.50m, 1.20m, 2.50m),
            SlotSymbolType.Blue => (0.65m, 1.50m, 3.50m),
            SlotSymbolType.Orange => (0.90m, 2.00m, 5.00m),
            SlotSymbolType.Cyan => (1.25m, 2.75m, 7.00m),
            SlotSymbolType.Green => (1.75m, 3.75m, 10.0m),
            SlotSymbolType.Purple => (2.50m, 5.00m, 15.0m),
            SlotSymbolType.Heart => (3.50m, 7.00m, 22.0m),
            SlotSymbolType.Swirl => (5.00m, 10.0m, 35.0m),
            SlotSymbolType.Star => (8.00m, 16.0m, 60.0m),
            _ => (0.50m, 1.00m, 2.50m)
        };

        if (count >= 12) return p12;
        if (count >= 10) return p10;
        return p8;
    }

    private void UpdateExploding(float deltaTime)
    {
        _stateTimer -= deltaTime;

        // Animate exploding scale/pulse
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                var cell = _grid[c, r];
                if (cell.IsWinning)
                {
                    cell.Scale = Math.Max(0f, cell.Scale - deltaTime * 2.5f);
                }
            }
        }

        if (_stateTimer <= 0f)
        {
            // Shatter winning symbols into explosion particles!
            for (int c = 0; c < Cols; c++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    var cell = _grid[c, r];
                    if (cell.IsWinning)
                    {
                        float cellX = GridStartX + c * (CellSize + CellGap) + CellSize / 2f;
                        float cellY = GridStartY + r * (CellSize + CellGap) + CellSize / 2f;
                        _fx.TriggerExplosion(new Vector2(cellX, cellY), GetSymbolColor(cell.Symbol), 28);

                        cell.IsEmpty = true;
                        cell.IsWinning = false;
                        cell.Scale = 1f;
                    }
                }
            }

            _tumbleCombo++;
            StartTumble();
        }
    }

    private void StartTumble()
    {
        float fallSpeed = _turboMode ? 2600f : 1400f;

        // For each column, collapse existing symbols down
        for (int c = 0; c < Cols; c++)
        {
            int writeRow = Rows - 1;
            for (int r = Rows - 1; r >= 0; r--)
            {
                if (!_grid[c, r].IsEmpty)
                {
                    if (writeRow != r)
                    {
                        _grid[c, writeRow] = _grid[c, r];
                        _grid[c, writeRow].TargetY = GridStartY + writeRow * (CellSize + CellGap);
                        _grid[c, writeRow].VelocityY = fallSpeed;
                        _grid[c, r] = new SlotCell { IsEmpty = true };
                    }
                    writeRow--;
                }
            }

            // Fill empty slots from top
            int spawnIndex = 1;
            for (int r = writeRow; r >= 0; r--)
            {
                float targetY = GridStartY + r * (CellSize + CellGap);
                float startY = GridStartY - (spawnIndex * (CellSize + CellGap));

                var sym = GetRandomSymbol(isTumble: true, combo: _tumbleCombo);
                int bombMult = sym == SlotSymbolType.Bomb ? GetRandomBombMultiplier() : 1;

                _grid[c, r] = new SlotCell
                {
                    Symbol = sym,
                    Y = startY,
                    TargetY = targetY,
                    VelocityY = fallSpeed,
                    IsEmpty = false,
                    IsWinning = false,
                    Scale = 1f,
                    BombMultiplier = bombMult
                };
                spawnIndex++;
            }
        }

        _state = SlotsState.Tumbling;
        float tumblePitch = Math.Min(1.6f, 1.0f + (_tumbleCombo - 1) * 0.06f);
        PlaySnd(_sndTumble, 0.75f, tumblePitch);
    }

    private void UpdateTumbling(float deltaTime)
    {
        bool allSettled = true;
        float speed = _turboMode ? 2600f : 1400f;

        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                var cell = _grid[c, r];
                if (!cell.IsEmpty && cell.Y < cell.TargetY)
                {
                    cell.Y += speed * deltaTime;
                    if (cell.Y >= cell.TargetY)
                    {
                        cell.Y = cell.TargetY;
                    }
                    else
                    {
                        allSettled = false;
                    }
                }
            }
        }

        if (allSettled)
        {
            _fx.AddTrauma(0.08f);
            _state = SlotsState.Evaluating;
        }
    }

    private void UpdateDetonatingBombs(float deltaTime)
    {
        _stateTimer -= deltaTime;
        if (_stateTimer <= 0f)
        {
            PlaySnd(_sndBombExplode, 0.95f);

            // Detonate active bombs!
            int totalBombMult = 0;
            foreach (var bomb in _activeBombs)
            {
                float cellX = bomb.TargetY; // placeholder
                for (int c = 0; c < Cols; c++)
                {
                    for (int r = 0; r < Rows; r++)
                    {
                        if (_grid[c, r] == bomb)
                        {
                            Vector2 pos = new(
                                GridStartX + c * (CellSize + CellGap) + CellSize / 2f,
                                GridStartY + r * (CellSize + CellGap) + CellSize / 2f);
                            _fx.TriggerBombBlast(pos);
                        }
                    }
                }

                totalBombMult += bomb.BombMultiplier;
            }

            _accumulatedBombMultiplier = Math.Max(1, totalBombMult);
            _roundWin *= _accumulatedBombMultiplier;

            FinishRound();
        }
    }

    private void FinishRound()
    {
        if (_roundWin > 0m)
        {
            _currency.Add(_roundWin);

            // Win celebration check
            if (_roundWin >= CurrentBet * 10m)
            {
                PlaySnd(_sndBigWin, 0.95f);
                _state = SlotsState.WinCelebration;
                _celebrationTimer = 3.0f;

                if (_roundWin >= CurrentBet * 50m)
                {
                    _winTierName = "SENSATIONAL WIN!";
                    _winBanner = _texBannerSensational;
                }
                else if (_roundWin >= CurrentBet * 25m)
                {
                    _winTierName = "MEGA WIN!";
                    _winBanner = _texBannerMegaWin;
                }
                else
                {
                    _winTierName = "BIG WIN!";
                    _winBanner = _texBannerBigWin;
                }

                _fx.AddTrauma(0.9f);
                return;
            }
            else
            {
                PlaySnd(_sndWinChime, 0.85f);
            }
        }

        _state = SlotsState.Idle;
    }

    private void UpdateCelebration(float deltaTime)
    {
        _celebrationTimer -= deltaTime;

        // Fountain coins
        _fx.SpawnCoinFountain(5);

        if (_celebrationTimer <= 0f || Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _state = SlotsState.Idle;
        }
    }

    public void Draw()
    {
        // Screen Shake offset
        Vector2 shake = _fx.ShakeOffset;

        // 1. Casino Header
        if (BingoPlusHeader.Draw("NEON BONANZA SLOTS", _currency))
        {
            IsFinished = true;
        }

        // 2. Machine Outer Cabinet (y: 68 to 570)
        Rectangle cabinetRect = new(GridStartX - 28 + shake.X, GridStartY - 12 + shake.Y, 600, 480);
        Raylib.DrawRectangleRec(cabinetRect, new Color(18, 24, 40, 240));
        Raylib.DrawRectangleLinesEx(cabinetRect, 3, new Color(255, 180, 0, 180));

        // Machine subtle vertical reel borders
        for (int c = 0; c <= Cols; c++)
        {
            float lineX = GridStartX + c * (CellSize + CellGap) - CellGap / 2f + shake.X;
            Raylib.DrawLine((int)lineX, (int)GridStartY - 8, (int)lineX, (int)(GridStartY + Rows * (CellSize + CellGap)), new Color(255, 255, 255, 15));
        }

        // 3. Draw 6x5 Grid Symbols
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                var cell = _grid[c, r];
                if (cell.IsEmpty) continue;

                // Cull if above visible window
                if (cell.Y + CellSize < GridStartY - 20) continue;

                float drawX = GridStartX + c * (CellSize + CellGap) + shake.X;
                float drawY = cell.Y + shake.Y;

                // Cell background glass box
                Rectangle cellBox = new(drawX, drawY, CellSize, CellSize);
                Raylib.DrawRectangleRec(cellBox, new Color(26, 34, 56, 180));
                Raylib.DrawRectangleLinesEx(cellBox, 1, new Color(255, 255, 255, 25));

                if (_symbolTextures.TryGetValue(cell.Symbol, out var tex) && tex.Id != 0)
                {
                    float sz = CellSize * cell.Scale;
                    float ox = (CellSize - sz) / 2f;
                    float oy = (CellSize - sz) / 2f;
                    Rectangle src = new(0, 0, tex.Width, tex.Height);
                    Rectangle dest = new(drawX + ox, drawY + oy, sz, sz);

                    Color tint = cell.IsWinning ? Color.Yellow : Color.White;
                    Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0f, tint);

                    // If Bomb, draw multiplier badge
                    if (cell.Symbol == SlotSymbolType.Bomb)
                    {
                        string multStr = $"{cell.BombMultiplier}x";
                        UiFont.DrawCenteredInRect(multStr, cellBox, 15, Color.Yellow, isBold: true);
                    }
                }

                // Winning cell pulse frame
                if (cell.IsWinning)
                {
                    Raylib.DrawRectangleLinesEx(cellBox, 3, Color.Gold);
                }
            }
        }

        // 4. Left Tower: Tumble Combo Gauge (x: 40 to 320)
        DrawTumbleTower();

        // 5. Right Tower: Win Guide / Paytable hints (x: 960 to 1240)
        DrawRightPaytable();

        // 6. Bottom Console & Controls (y: 575 to 715)
        DrawBottomConsole();

        // 7. Particles & Explosions
        _fx.Draw();

        // 8. Win Celebration Modal Banner
        if (_state == SlotsState.WinCelebration)
        {
            DrawCelebrationPopup();
        }

        // 9. Out of Coins Modal Popup
        if (_showOutOfCoinsModal)
        {
            DrawOutOfCoinsModal();
        }
    }

    private void DrawTumbleTower()
    {
        Rectangle towerRect = new(50, 85, 270, 460);
        Raylib.DrawRectangleRec(towerRect, new Color(20, 26, 44, 235));
        Raylib.DrawRectangleLinesEx(towerRect, 2, new Color(255, 180, 0, 150));

        UiFont.DrawCenteredInRect("MULTIPLIER VAULT", new Rectangle(50, 95, 270, 28), 16, Color.Gold, isBold: true);

        // Calculate total active bomb multiplier in vault
        int collectedMult = 0;
        foreach (var bomb in _activeBombs)
        {
            collectedMult += bomb.BombMultiplier;
        }

        // Vault Chamber Box
        Rectangle vaultBox = new(65, 130, 240, 160);
        Color vaultBg = collectedMult > 0 ? new Color(38, 22, 54, 255) : new Color(16, 20, 32, 255);
        Color vaultBorder = collectedMult > 0 ? Color.Gold : new Color(255, 255, 255, 25);
        Raylib.DrawRectangleRec(vaultBox, vaultBg);
        Raylib.DrawRectangleLinesEx(vaultBox, 2, vaultBorder);

        if (_symbolTextures.TryGetValue(SlotSymbolType.Bomb, out var bombTex) && bombTex.Id != 0)
        {
            Rectangle src = new(0, 0, bombTex.Width, bombTex.Height);
            Rectangle dest = new(65 + (240 - 58) / 2f, 142, 58, 58);
            Color bombTint = collectedMult > 0 ? Color.White : new Color(140, 140, 140, 160);
            Raylib.DrawTexturePro(bombTex, src, dest, Vector2.Zero, 0f, bombTint);
        }

        if (collectedMult > 0)
        {
            string multText = $"{collectedMult}x";
            UiFont.DrawCenteredInRect(multText, new Rectangle(65, 206, 240, 44), 32, Color.Yellow, isBold: true);
            UiFont.DrawCenteredInRect("ACTIVE BOOST", new Rectangle(65, 256, 240, 20), 12, Color.Lime, isBold: true);
        }
        else
        {
            UiFont.DrawCenteredInRect("VAULT EMPTY", new Rectangle(65, 212, 240, 24), 15, Color.Gray, isBold: true);
            UiFont.DrawCenteredInRect("Unlocks on Cascade 2+", new Rectangle(65, 246, 240, 20), 11, new Color(160, 170, 190, 255), isBold: false);
        }

        // Cascade streak indicator
        Rectangle streakBox = new(65, 305, 240, 60);
        Raylib.DrawRectangleRec(streakBox, new Color(18, 24, 38, 255));
        Raylib.DrawRectangleLinesEx(streakBox, 1, new Color(255, 255, 255, 25));
        UiFont.DrawCenteredInRect("CASCADE STREAK", new Rectangle(65, 312, 240, 14), 10, Color.LightGray, isBold: true);
        string streakText = _tumbleCombo > 1 ? $"CASCADE {_tumbleCombo}" : "BASE SPIN";
        Color streakColor = _tumbleCombo > 1 ? Color.Lime : Color.LightGray;
        UiFont.DrawCenteredInRect(streakText, new Rectangle(65, 332, 240, 24), 16, streakColor, isBold: true);

        // Round Win Accumulator Box
        Rectangle roundBox = new(65, 380, 240, 145);
        Raylib.DrawRectangleRec(roundBox, new Color(14, 18, 30, 255));
        Raylib.DrawRectangleLinesEx(roundBox, 2, Color.Gold);
        UiFont.DrawCenteredInRect("ROUND WIN", new Rectangle(65, 400, 240, 20), 13, Color.LightGray, isBold: true);
        string winStr = $"{_displayWin:N2}";
        UiFont.DrawCenteredInRect(winStr, new Rectangle(65, 436, 240, 48), 26, Color.Gold, isBold: true);
    }

    private void DrawRightPaytable()
    {
        Rectangle guideRect = new(955, 85, 275, 460);
        Raylib.DrawRectangleRec(guideRect, new Color(20, 26, 44, 235));
        Raylib.DrawRectangleLinesEx(guideRect, 2, new Color(255, 180, 0, 150));

        UiFont.DrawCenteredInRect("PAY ANYWHERE RULES", new Rectangle(955, 95, 275, 30), 16, Color.Gold, isBold: true);

        string rules = "• Match 8+ identical symbols anywhere on the grid to win.\n\n• Winning symbols explode and trigger cascading tumbles.\n\n• Multiplier BOMBS boost tumble wins on Cascade 2+!\n\n• 4+ Lollipops award Scatter Bonus!";

        float textX = 975;
        float textY = 135;
        float maxWidth = 275 - 40; // 235px cleanly wrapped inside the 275px box
        float textHeight = UiFont.DrawWrappedText(rules, textX, textY, maxWidth, 13, new Color(210, 220, 240, 255), isBold: false, lineSpacing: 4f);

        float symY = textY + textHeight + 16f;
        Raylib.DrawLine((int)textX, (int)symY - 8, (int)(textX + maxWidth), (int)symY - 8, new Color(255, 255, 255, 25));

        DrawSymbolRow(SlotSymbolType.Star, "8+ Pays 8x Bet", textX, symY);
        DrawSymbolRow(SlotSymbolType.Swirl, "8+ Pays 5x Bet", textX, symY + 44);
        DrawSymbolRow(SlotSymbolType.Heart, "8+ Pays 3.5x Bet", textX, symY + 88);
    }

    private void DrawSymbolRow(SlotSymbolType sym, string desc, float x, float y)
    {
        if (_symbolTextures.TryGetValue(sym, out var tex) && tex.Id != 0)
        {
            Rectangle src = new(0, 0, tex.Width, tex.Height);
            Rectangle dest = new(x, y, 42, 42);
            Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0f, Color.White);
        }
        UiFont.DrawText(desc, x + 50, y + 12, 13, Color.White, isBold: true);
    }

    private void DrawBottomConsole()
    {
        // Console Base
        Raylib.DrawRectangle(0, 570, GameCanvas.Width, 150, new Color(14, 18, 30, 255));
        Raylib.DrawLine(0, 570, GameCanvas.Width, 570, new Color(255, 180, 0, 120));

        // Bet Minus
        Rectangle minusRect = new(175, 620, 46, 46);
        UiImageButton.Draw(_texBtnMinus, minusRect);

        // Bet Pill
        Rectangle betPill = new(230, 620, 105, 46);
        Raylib.DrawRectangleRec(betPill, new Color(26, 34, 54, 255));
        Raylib.DrawRectangleLinesEx(betPill, 1, Color.Gold);
        UiFont.DrawCenteredInRect("BET", new Rectangle(230, 624, 105, 14), 10, Color.LightGray, isBold: true);
        UiFont.DrawCenteredInRect($"{CurrentBet:0.00}", new Rectangle(230, 638, 105, 24), 16, Color.Gold, isBold: true);

        // Bet Plus
        Rectangle plusRect = new(345, 620, 46, 46);
        UiImageButton.Draw(_texBtnPlus, plusRect);

        // Center Win Display
        Rectangle winDisplay = new(430, 605, 340, 65);
        Raylib.DrawRectangleRec(winDisplay, new Color(20, 28, 48, 255));
        Raylib.DrawRectangleLinesEx(winDisplay, 2, new Color(255, 215, 0, 220));
        UiFont.DrawCenteredInRect("WIN COINS", new Rectangle(430, 610, 340, 16), 11, Color.LightGray, isBold: true);
        string winFormatted = $"{_displayWin:N2}";
        Color winColor = _displayWin > 0 ? Color.Gold : Color.Gray;
        UiFont.DrawCenteredInRect(winFormatted, new Rectangle(430, 626, 340, 38), 26, winColor, isBold: true);

        // Turbo Toggle
        Rectangle turboRect = new(810, 625, 84, 36);
        UiImageButton.Draw(_texBtnTurbo, turboRect);
        if (_turboMode)
        {
            Raylib.DrawRectangleLinesEx(turboRect, 2, Color.Lime);
        }

        // Auto Spin Toggle (Same toggle style as Turbo button)
        Rectangle autoRect = new(905, 625, 84, 36);
        UiImageButton.Draw(_texBtnAuto, autoRect);
        if (_autoSpin)
        {
            Raylib.DrawRectangleLinesEx(autoRect, 2, Color.Lime);
        }

        // Big Spin Button
        Rectangle spinRect = new(1015, 595, 96, 96);
        UiImageButton.Draw(_texBtnSpin, spinRect);
        if (_autoSpin)
        {
            Raylib.DrawCircleLines(1015 + 48, 595 + 48, 48, Color.Lime);
        }
    }

    private void DrawCelebrationPopup()
    {
        // Dark backdrop overlay
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(0, 0, 0, 180));

        // Draw Win Banner
        if (_winBanner.Id != 0)
        {
            float bannerW = 540;
            float bannerH = 150;
            Rectangle src = new(0, 0, _winBanner.Width, _winBanner.Height);
            Rectangle dest = new((GameCanvas.Width - bannerW) / 2f, 180, bannerW, bannerH);
            Raylib.DrawTexturePro(_winBanner, src, dest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            UiFont.DrawCenteredText(_winTierName, 180, 52, Color.Gold, isBold: true);
        }

        // Win Amount
        string winAmountStr = $"{_roundWin:N2} COINS!";
        UiFont.DrawCenteredText(winAmountStr, 350, 48, Color.Gold, isBold: true);
        UiFont.DrawCenteredText("CLICK ANYWHERE TO CONTINUE", 430, 16, Color.LightGray, isBold: false);
    }

    // Modal Dialog Layout Rectangles
    private static readonly Rectangle ModalBoxRect = new(390, 220, 500, 280);
    private static readonly Rectangle ModalDepositBtnRect = new(420, 425, 205, 48);
    private static readonly Rectangle ModalLobbyBtnRect = new(655, 425, 205, 48);
    private static readonly Rectangle ModalCloseBtnRect = new(854, 232, 26, 26);

    private void UpdateOutOfCoinsModal()
    {
        if (UiImageButton.WasClicked(ModalCloseBtnRect) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _showOutOfCoinsModal = false;
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
            return;
        }
    }

    private void DrawOutOfCoinsModal()
    {
        // 1. Semi-transparent dark background backdrop
        Raylib.DrawRectangle(0, 0, GameCanvas.Width, GameCanvas.Height, new Color(0, 0, 0, 210));

        // 2. Modal Card Frame
        Raylib.DrawRectangleRec(ModalBoxRect, new Color(18, 24, 38, 255));
        Raylib.DrawRectangleLinesEx(ModalBoxRect, 2, Color.Gold);

        // 3. Close Button [X]
        bool closeHov = UiImageButton.IsHovered(ModalCloseBtnRect);
        Raylib.DrawRectangleRec(ModalCloseBtnRect, closeHov ? new Color(220, 38, 38, 200) : new Color(36, 44, 62, 255));
        Raylib.DrawRectangleLinesEx(ModalCloseBtnRect, 1, closeHov ? Color.Red : Color.Gray);
        UiFont.DrawCenteredInRect("X", ModalCloseBtnRect, 14, Color.White, isBold: true);

        // 4. Header Title & Icon
        Rectangle titleRect = new(ModalBoxRect.X, ModalBoxRect.Y + 28, ModalBoxRect.Width, 34);
        UiFont.DrawCenteredInRect("! OUT OF COINS !", titleRect, 24, Color.Gold, isBold: true);

        // 5. Message Body
        Rectangle msg1Rect = new(ModalBoxRect.X + 20, ModalBoxRect.Y + 80, ModalBoxRect.Width - 40, 28);
        UiFont.DrawCenteredInRect($"You need {CurrentBet:N2} coins to spin.", msg1Rect, 18, Color.White);

        Rectangle msg2Rect = new(ModalBoxRect.X + 20, ModalBoxRect.Y + 115, ModalBoxRect.Width - 40, 26);
        UiFont.DrawCenteredInRect($"Your current balance: {_currency.Balance:N2} coins", msg2Rect, 16, Color.LightGray);

        Rectangle hintRect = new(ModalBoxRect.X + 20, ModalBoxRect.Y + 152, ModalBoxRect.Width - 40, 22);
        UiFont.DrawCenteredInRect("Deposit coins now or return to the main lobby.", hintRect, 14, new Color(160, 174, 192, 255));

        // 6. Action Buttons
        // Deposit More
        bool depHov = UiImageButton.IsHovered(ModalDepositBtnRect);
        Color depBg = depHov ? new Color(40, 200, 100, 255) : new Color(34, 160, 80, 255);
        Raylib.DrawRectangleRec(ModalDepositBtnRect, depBg);
        Raylib.DrawRectangleLinesEx(ModalDepositBtnRect, 2, depHov ? Color.Lime : Color.Gold);
        UiFont.DrawCenteredInRect("DEPOSIT MORE", ModalDepositBtnRect, 16, Color.White, isBold: true);

        // Return to Lobby
        bool lobHov = UiImageButton.IsHovered(ModalLobbyBtnRect);
        Color lobBg = lobHov ? new Color(71, 85, 105, 255) : new Color(51, 65, 85, 255);
        Raylib.DrawRectangleRec(ModalLobbyBtnRect, lobBg);
        Raylib.DrawRectangleLinesEx(ModalLobbyBtnRect, 1, lobHov ? Color.White : Color.Gray);
        UiFont.DrawCenteredInRect("RETURN TO LOBBY", ModalLobbyBtnRect, 15, Color.LightGray, isBold: true);
    }
}
