using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public sealed class CrashMinigame : IMinigame, IDepositRequester
{
    private const decimal MinimumBet = 1m;
    private const decimal DefaultStartingBet = 10m;

    // Viewport coordinates (1920 x 1080)
    private const float ArenaX = 75f;
    private const float ArenaY = 165f;
    private const float ArenaWidth = 1770f;
    private const float ArenaHeight = 630f;

    private readonly CurrencyManager _currency;
    private readonly Random _rand = new();

    // Textures
    private Texture2D _texPlane;
    private Texture2D _texPropeller;
    private Texture2D _texSmoke;
    private Texture2D _texBtnLaunch;
    private Texture2D _texBtnCashout;

    // Audio SFX
    private Sound _sndPropeller;
    private Sound _sndTakeoff;
    private Sound _sndCashout;
    private Sound _sndCrash;
    private Sound _sndError;
    private bool _isAudioLoaded;

    // Game States
    private enum FlightState
    {
        Idle,       // Waiting on runway for player to click BET / LAUNCH
        TakingOff,  // Quick launch rev-up (0.35s)
        InFlight,   // Climbing smoothly along exponential flight path
        Crashed     // Flew away / exploded
    }

    private FlightState _state = FlightState.Idle;
    private float _flightTime;
    private decimal _multiplier = 1.00m;
    private decimal _crashPoint = 1.00m;

    // Betting & Winnings
    private decimal _bet = DefaultStartingBet;
    private bool _hasActiveBet;
    private bool _hasCashedOut;
    private decimal _cashedOutMultiplier;
    private decimal _roundWinnings;
    private bool _isMaxBet = false;
    private bool _showOutOfCoinsModal = false;
    private string _lastRoundOutcomeText = "";
    private Color _lastRoundOutcomeColor = Color.Lime;

    // Auto-Cashout Settings
    private bool _autoCashoutEnabled = false;
    private decimal _autoCashoutTarget = 2.00m;

    // History Strip (Last 8 rounds)
    private readonly List<decimal> _history = [1.84m, 1.00m, 3.42m, 1.25m, 12.50m, 1.10m, 2.05m];

    // Visual FX & Animation
    private float _propellerAngle;
    private float _screenShake;
    private readonly List<CrashParticle> _particles = [];
    private readonly List<Vector2> _trailPoints = [];

    // Flight plane screen position
    private Vector2 _planePos = new(ArenaX + 120f, ArenaY + ArenaHeight - 90f);
    private float _planeRotation = 0f;

    public string Name => "Aviator Crash";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }

    public CrashMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
        LoadAssets();
    }

    private void LoadAssets()
    {
        _texPlane = UiImageButton.LoadTexture("scenes/Minigames/Crash/assets/plane.png");
        _texPropeller = UiImageButton.LoadTexture("scenes/Minigames/Crash/assets/propeller.png");
        _texSmoke = UiImageButton.LoadTexture("scenes/Minigames/Crash/assets/smoke_particle.png");
        _texBtnLaunch = UiImageButton.LoadTexture("scenes/Minigames/Crash/assets/btn_launch.png");
        _texBtnCashout = UiImageButton.LoadTexture("scenes/Minigames/Crash/assets/btn_cashout.png");

        if (Raylib.IsAudioDeviceReady())
        {
            try
            {
                _sndPropeller = Raylib.LoadSound("scenes/Minigames/Crash/assets/audio/sfx_propeller.wav");
                _sndTakeoff = Raylib.LoadSound("scenes/Minigames/Crash/assets/audio/sfx_takeoff.wav");
                _sndCashout = Raylib.LoadSound("scenes/Minigames/Crash/assets/audio/sfx_cashout.wav");
                _sndCrash = Raylib.LoadSound("scenes/Minigames/Crash/assets/audio/sfx_crash.wav");
                _sndError = Raylib.LoadSound("scenes/Minigames/Crash/assets/audio/sfx_error.wav");
                _isAudioLoaded = true;
            }
            catch
            {
                _isAudioLoaded = false;
            }
        }
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _showOutOfCoinsModal = false;
        _isMaxBet = false;
        _lastRoundOutcomeText = "";
        _state = FlightState.Idle;
        _multiplier = 1.00m;
        _flightTime = 0f;
        _hasActiveBet = false;
        _hasCashedOut = false;
        _bet = Math.Min(DefaultStartingBet, _currency.Balance);
        _particles.Clear();
        _trailPoints.Clear();
        ResetPlaneToRunway();
    }

    public void Stop()
    {
        IsFinished = true;
        StopEngineAudio();
        _particles.Clear();
    }

    private void ResetPlaneToRunway()
    {
        _planePos = new Vector2(ArenaX + 80f, ArenaY + ArenaHeight - 65f);
        _planeRotation = 0f;
        _trailPoints.Clear();
        _trailPoints.Add(_planePos);
    }

    private void PlaySnd(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (!_isAudioLoaded) return;
        Raylib.SetSoundVolume(sound, volume);
        Raylib.SetSoundPitch(sound, pitch);
        Raylib.PlaySound(sound);
    }

    private void StopEngineAudio()
    {
        if (_isAudioLoaded && Raylib.IsSoundPlaying(_sndPropeller))
        {
            Raylib.StopSound(_sndPropeller);
        }
    }

    public void Update(float deltaTime)
    {
        if (IsFinished) return;

        // If Out of Coins modal is active, handle modal input and freeze background controls
        if (_showOutOfCoinsModal)
        {
            UpdateOutOfCoinsModal();
            return;
        }

        // Screen shake decay
        if (_screenShake > 0f)
        {
            _screenShake = Math.Max(0f, _screenShake - deltaTime * 2.5f);
        }

        // Update active particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(deltaTime);
            if (_particles[i].IsDead)
            {
                _particles.RemoveAt(i);
            }
        }

        // Animated spinning propeller
        float propSpeed = _state == FlightState.InFlight ? 2800f : _state == FlightState.TakingOff ? 1600f : 500f;
        _propellerAngle = (_propellerAngle + propSpeed * deltaTime) % 360f;

        // State Machine
        switch (_state)
        {
            case FlightState.Idle:
                UpdateIdle();
                break;
            case FlightState.TakingOff:
                UpdateTakingOff(deltaTime);
                break;
            case FlightState.InFlight:
                UpdateInFlight(deltaTime);
                break;
            case FlightState.Crashed:
                UpdateCrashed();
                break;
        }

        // Process Dashboard UI clicks (Bet presets, Auto-cashout, etc.)
        UpdateDashboardControls();
    }

    private void UpdateIdle()
    {
        // Plane gently idles on runway
        float idleBob = MathF.Sin((float)Raylib.GetTime() * 3f) * 2f;
        _planePos = new Vector2(ArenaX + 80f, ArenaY + ArenaHeight - 65f + idleBob);
        _planeRotation = 0f;

        // Spacebar shortcut to launch
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            TriggerLaunch();
        }
    }

    private void TriggerLaunch()
    {
        if (_currency.Balance < _bet || _currency.Balance <= 0m)
        {
            _showOutOfCoinsModal = true;
            PlaySnd(_sndError, 0.9f);
            return;
        }

        if (!_currency.TrySpend(_bet))
        {
            _showOutOfCoinsModal = true;
            PlaySnd(_sndError, 0.9f);
            return;
        }

        _hasActiveBet = true;
        _hasCashedOut = false;
        _roundWinnings = 0m;
        _cashedOutMultiplier = 1.00m;
        _multiplier = 1.00m;
        _flightTime = 0f;
        _trailPoints.Clear();
        _particles.Clear();

        // Calculate Crash Point with Aviator math (~94% RTP, 100.00x Maximum Cap):
        // 3.5% chance of instant blowup right off the runway (1.00x)
        int instantRoll = _rand.Next(1000);
        if (instantRoll < 35)
        {
            _crashPoint = 1.00m;
        }
        else
        {
            // Standard inverse crash distribution capped at 100.00x:
            double u = _rand.NextDouble();
            u = Math.Max(0.0001, u);
            double rawCrash = 0.94 / (1.0 - u * (1.0 - 0.94 / 100.0));
            _crashPoint = Math.Round((decimal)Math.Clamp(rawCrash, 1.01, 100.0), 2);
        }

        _state = FlightState.TakingOff;
        PlaySnd(_sndTakeoff, 0.85f);
    }

    private void UpdateTakingOff(float deltaTime)
    {
        _flightTime += deltaTime;
        if (_flightTime >= 0.35f)
        {
            _flightTime = 0f;
            _state = FlightState.InFlight;

            if (_isAudioLoaded)
            {
                Raylib.SetSoundVolume(_sndPropeller, 0.5f);
                Raylib.PlaySound(_sndPropeller);
            }

            // If crashed immediately at 1.00x ("Auto blowup at start")
            if (_crashPoint <= 1.00m)
            {
                TriggerCrash();
            }
        }
    }

    private void UpdateInFlight(float deltaTime)
    {
        _flightTime += deltaTime;

        // Fast Casino Aviator Multiplier Curve (~22.7s to 100.00x):
        // 1.5x at ~2.5s, 2.0x at ~3.5s, 10x at ~9s, 50x at ~17.5s, 100x at ~22.7s
        double t = _flightTime;
        double currentMult = 1.00 + 0.08 * t + 0.04 * t * t + 0.0065 * t * t * t;
        _multiplier = Math.Min(100.00m, Math.Round((decimal)currentMult, 2));

        // Propeller sound pitch shift with rising multiplier
        if (_isAudioLoaded)
        {
            float pitch = Math.Clamp(0.85f + (float)(_multiplier - 1.00m) * 0.035f, 0.85f, 2.5f);
            Raylib.SetSoundPitch(_sndPropeller, pitch);
            if (!Raylib.IsSoundPlaying(_sndPropeller))
            {
                Raylib.PlaySound(_sndPropeller);
            }
        }

        // Auto-Cashout Check
        if (_hasActiveBet && !_hasCashedOut && _autoCashoutEnabled && _multiplier >= _autoCashoutTarget)
        {
            PerformCashOut();
        }

        // Check Crash Hit
        if (_multiplier >= _crashPoint)
        {
            _multiplier = _crashPoint;
            TriggerCrash();
            return;
        }

        // Spacebar cash out shortcut
        if (_hasActiveBet && !_hasCashedOut && Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            PerformCashOut();
        }

        // Calculate Plane Flight Coordinates across radar screen
        // Dynamic Logarithmic Climb: Maps trajectory smoothly from 1.00x at runway to 100.00x at ceiling
        // ln(1.00) = 0, ln(100.0) ~= 4.605
        float logProgress = Math.Clamp(MathF.Log(Math.Max(1.0001f, (float)_multiplier)) / 4.60517f, 0f, 1f);

        // Gentle easing for smooth takeoff and apex ceiling approach
        float easeX = MathF.Pow(logProgress, 0.85f);
        float easeY = MathF.Pow(logProgress, 0.95f);

        float targetX = ArenaX + 120f + easeX * (ArenaWidth - 330f);
        float targetY = (ArenaY + ArenaHeight - 97.5f) - easeY * (ArenaHeight - 165f);

        // Small turbulent vibration
        float turbulence = MathF.Sin(_flightTime * 14f) * 3.75f;
        _planePos = new Vector2(targetX, targetY + turbulence);

        // Tangent angle of climb
        _planeRotation = -16f - (1f - easeY) * 12f;

        // Record flight trail points
        if (_trailPoints.Count == 0 || Vector2.Distance(_trailPoints[^1], _planePos) > 18f)
        {
            _trailPoints.Add(_planePos);
        }

        // Emit exhaust smoke particles
        if (_rand.Next(10) < 6)
        {
            Vector2 exhaustPos = _planePos + new Vector2(-42f, 8f);
            _particles.Add(new CrashParticle(
                exhaustPos,
                new Vector2(-_rand.Next(40, 100), _rand.Next(-20, 20)),
                new Color(255, 60, 80, 210),
                lifetime: 0.65f,
                size: _rand.Next(16, 26)
            ));
        }
    }

    private void PerformCashOut()
    {
        if (!_hasActiveBet || _hasCashedOut) return;

        _hasCashedOut = true;
        _cashedOutMultiplier = _multiplier;
        _roundWinnings = decimal.Round(_bet * _cashedOutMultiplier, 2);
        _currency.Add(_roundWinnings);
        _lastRoundOutcomeText = $"LAST WIN: +{_roundWinnings:N2} ({_cashedOutMultiplier:0.00}x)";
        _lastRoundOutcomeColor = Color.Lime;

        if (_isMaxBet && _currency.Balance > 0m)
        {
            _bet = Math.Max(MinimumBet, _currency.Balance);
        }

        PlaySnd(_sndCashout, 0.95f);

        // Spawn gold victory sparkle fountain
        for (int i = 0; i < 24; i++)
        {
            float angle = (float)_rand.NextDouble() * MathF.PI * 2f;
            float speed = _rand.Next(80, 260);
            _particles.Add(new CrashParticle(
                _planePos,
                new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Color.Gold,
                lifetime: 0.9f,
                size: _rand.Next(12, 20)
            ));
        }
    }

    private void TriggerCrash()
    {
        _state = FlightState.Crashed;
        StopEngineAudio();
        PlaySnd(_sndCrash, 1.0f);
        _screenShake = 0.65f;

        if (!_hasCashedOut)
        {
            _lastRoundOutcomeText = $"LOST: -{_bet:N2} (Crashed at {_crashPoint:0.00}x)";
            _lastRoundOutcomeColor = new Color(255, 90, 100, 255);
        }

        if (_isMaxBet && _currency.Balance > 0m)
        {
            _bet = Math.Max(MinimumBet, _currency.Balance);
        }

        // Push to history ribbon (keep last 8)
        _history.Insert(0, _crashPoint);
        if (_history.Count > 8) _history.RemoveAt(_history.Count - 1);

        // Spawn crash explosion burst
        for (int i = 0; i < 40; i++)
        {
            float angle = (float)_rand.NextDouble() * MathF.PI * 2f;
            float speed = _rand.Next(60, 340);
            Color col = i % 2 == 0 ? new Color(255, 50, 60, 255) : new Color(255, 200, 30, 255);
            _particles.Add(new CrashParticle(
                _planePos,
                new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                col,
                lifetime: 0.85f,
                size: _rand.Next(16, 36)
            ));
        }
    }

    private void UpdateCrashed()
    {
        // Plane rests in crashed state until player clicks BET or changes bet
    }

    // ==================== DASHBOARD UI LAYOUT CONSTANTS (Single Source of Truth) ====================
    private static readonly Rectangle DashRect = new(ArenaX, 818, ArenaWidth, 232);

    // Section 1: Bet Controls
    private static readonly Rectangle BetMinusRect = new(113, 846, 66, 66);
    private static readonly Rectangle BetPillRect = new(191, 846, 225, 66);
    private static readonly Rectangle BetPlusRect = new(428, 846, 66, 66);

    private static readonly decimal[] ChipPresets = [10m, 50m, 100m, 500m];
    private static Rectangle GetChipRect(int index) => new(113 + index * 78, 939, 69, 48);
    private static readonly Rectangle HalfBetRect = new(431, 939, 66, 48);
    private static readonly Rectangle DoubleBetRect = new(506, 939, 66, 48);
    private static readonly Rectangle MaxBetRect = new(581, 939, 78, 48);

    // Section 2: Big Action Button
    private static readonly Rectangle ActionBtnRect = new(683, 843, 465, 141);

    // Section 3: Auto Cashout Module
    private static readonly Rectangle AutoToggleBtnRect = new(1178, 846, 293, 66);
    private static readonly Rectangle AutoMinusBtnRect = new(1493, 846, 66, 66);
    private static readonly Rectangle AutoTargetPillRect = new(1571, 846, 189, 66);
    private static readonly Rectangle AutoPlusBtnRect = new(1772, 846, 66, 66);

    private static readonly decimal[] AutoPresets = [1.20m, 1.50m, 2.00m, 3.00m, 5.00m];
    private static Rectangle GetAutoPresetRect(int index) => new(1178 + index * 133.5f, 939, 123, 48);

    private void UpdateDashboardControls()
    {
        // 1. Big Action Button: Launch / Cash Out
        if (UiImageButton.WasClicked(ActionBtnRect))
        {
            if (_state == FlightState.Idle || _state == FlightState.Crashed)
            {
                TriggerLaunch();
            }
            else if (_state == FlightState.InFlight && _hasActiveBet && !_hasCashedOut)
            {
                PerformCashOut();
            }
        }

        // 2. Bet Adjustments (Only allowed when not flying)
        if (_state == FlightState.Idle || _state == FlightState.Crashed)
        {
            // Minus Bet
            if (UiImageButton.WasClicked(BetMinusRect))
            {
                _isMaxBet = false;
                _bet = Math.Max(MinimumBet, _bet <= 10m ? _bet - 1m : _bet - 5m);
            }

            // Plus Bet
            if (UiImageButton.WasClicked(BetPlusRect))
            {
                _isMaxBet = false;
                _bet = Math.Min(_currency.Balance, _bet < 10m ? _bet + 1m : _bet + 5m);
            }

            // Chip presets: 10, 50, 100, 500
            for (int i = 0; i < ChipPresets.Length; i++)
            {
                if (UiImageButton.WasClicked(GetChipRect(i)))
                {
                    _isMaxBet = false;
                    _bet = Math.Min(_currency.Balance, ChipPresets[i]);
                }
            }

            // 1/2, 2X, MAX chips
            if (UiImageButton.WasClicked(HalfBetRect))
            {
                _isMaxBet = false;
                _bet = Math.Max(MinimumBet, Math.Round(_bet / 2m, 0));
            }

            if (UiImageButton.WasClicked(DoubleBetRect))
            {
                _isMaxBet = false;
                _bet = Math.Min(_currency.Balance, _bet * 2m);
            }

            if (UiImageButton.WasClicked(MaxBetRect))
            {
                _isMaxBet = true;
                if (_currency.Balance <= 0m)
                {
                    _showOutOfCoinsModal = true;
                    PlaySnd(_sndError, 0.9f);
                }
                else
                {
                    _bet = Math.Max(MinimumBet, _currency.Balance);
                }
            }
        }

        // 3. Auto-Cashout Controls (Always clickable, even in flight!)
        if (UiImageButton.WasClicked(AutoToggleBtnRect))
        {
            _autoCashoutEnabled = !_autoCashoutEnabled;
        }

        if (UiImageButton.WasClicked(AutoMinusBtnRect))
        {
            decimal step = _autoCashoutTarget > 20.00m ? 5.00m : _autoCashoutTarget > 5.00m ? 1.00m : 0.25m;
            _autoCashoutTarget = Math.Max(1.10m, _autoCashoutTarget - step);
        }

        if (UiImageButton.WasClicked(AutoPlusBtnRect))
        {
            decimal step = _autoCashoutTarget >= 20.00m ? 5.00m : _autoCashoutTarget >= 5.00m ? 1.00m : 0.50m;
            _autoCashoutTarget = Math.Min(100.00m, _autoCashoutTarget + step);
        }

        for (int i = 0; i < AutoPresets.Length; i++)
        {
            if (UiImageButton.WasClicked(GetAutoPresetRect(i)))
            {
                _autoCashoutTarget = AutoPresets[i];
            }
        }
    }

    public void Draw()
    {
        // Screen Shake calculation
        Vector2 shakeOffset = Vector2.Zero;
        if (_screenShake > 0f)
        {
            shakeOffset = new Vector2(
                ((float)_rand.NextDouble() * 2f - 1f) * _screenShake * 24f,
                ((float)_rand.NextDouble() * 2f - 1f) * _screenShake * 24f
            );
        }

        // 1. Casino Header
        if (BingoPlusHeader.Draw("AVIATOR CRASH", _currency))
        {
            IsFinished = true;
        }

        // 2. Top Multiplier History Ribbon (y: 65 to 105)
        DrawHistoryRibbon();

        // 3. Main Flight Radar Viewport
        DrawFlightRadar(shakeOffset);

        // 4. Bottom Betting Dashboard (y: 545 to 705)
        DrawDashboard();

        // 5. Out of Coins Modal Popup
        if (_showOutOfCoinsModal)
        {
            DrawOutOfCoinsModal();
        }
    }

    private void DrawHistoryRibbon()
    {
        Rectangle ribbonRect = new(ArenaX, 98, ArenaWidth, 57);
        Raylib.DrawRectangleRec(ribbonRect, new Color(18, 24, 40, 220));
        Raylib.DrawRectangleLinesEx(ribbonRect, 1.5f, new Color(255, 255, 255, 20));

        UiFont.DrawText("HISTORY", ArenaX + 24, 113, 18, Color.Gray, isBold: true);

        float pillX = ArenaX + 135;
        for (int i = 0; i < _history.Count; i++)
        {
            decimal mult = _history[i];
            Color pillBg;
            Color textCol;

            if (mult < 2.00m)
            {
                pillBg = new Color(30, 58, 138, 220); // Blue
                textCol = new Color(147, 197, 253, 255);
            }
            else if (mult < 10.00m)
            {
                pillBg = new Color(107, 33, 168, 220); // Purple
                textCol = new Color(233, 213, 255, 255);
            }
            else
            {
                pillBg = new Color(161, 98, 7, 220);  // Gold
                textCol = new Color(254, 240, 138, 255);
            }

            Rectangle pillRect = new(pillX, 105, 117, 39);
            Raylib.DrawRectangleRec(pillRect, pillBg);
            Raylib.DrawRectangleLinesEx(pillRect, 1.5f, textCol);
            UiFont.DrawCenteredInRect($"{mult:0.00}x", pillRect, 18, textCol, isBold: true);

            pillX += 129;
        }
    }

    private void DrawFlightRadar(Vector2 shake)
    {
        Rectangle radarRect = new(ArenaX + shake.X, ArenaY + shake.Y, ArenaWidth, ArenaHeight);

        // Radar background deep dark space/radar blue
        Raylib.DrawRectangleRec(radarRect, new Color(11, 14, 24, 255));
        Raylib.DrawRectangleLinesEx(radarRect, 3, new Color(255, 45, 70, 140));

        // Radar coordinate grid lines
        for (float x = ArenaX + 150; x < ArenaX + ArenaWidth; x += 180)
        {
            Raylib.DrawLine((int)(x + shake.X), (int)(ArenaY + shake.Y), (int)(x + shake.X), (int)(ArenaY + ArenaHeight + shake.Y), new Color(255, 255, 255, 12));
        }
        for (float y = ArenaY + 90; y < ArenaY + ArenaHeight; y += 90)
        {
            Raylib.DrawLine((int)(ArenaX + shake.X), (int)(y + shake.Y), (int)(ArenaX + ArenaWidth + shake.X), (int)(y + shake.Y), new Color(255, 255, 255, 12));
        }

        // Round Outcome Summary Badge (Top-left of radar)
        if (!string.IsNullOrEmpty(_lastRoundOutcomeText))
        {
            Rectangle outcomeRect = new(ArenaX + 24 + shake.X, ArenaY + 21 + shake.Y, 420, 39);
            Raylib.DrawRectangleRec(outcomeRect, new Color(14, 18, 30, 230));
            Raylib.DrawRectangleLinesEx(outcomeRect, 1.5f, _lastRoundOutcomeColor);
            UiFont.DrawCenteredInRect(_lastRoundOutcomeText, outcomeRect, 16.5f, _lastRoundOutcomeColor, isBold: true);
        }

        // Draw curved red flight trajectory
        if (_trailPoints.Count > 1)
        {
            // Filled red glowing under-curve
            for (int i = 0; i < _trailPoints.Count - 1; i++)
            {
                Vector2 p1 = _trailPoints[i] + shake;
                Vector2 p2 = _trailPoints[i + 1] + shake;
                float bottomY = ArenaY + ArenaHeight + shake.Y;

                Vector2[] quad = [
                    new Vector2(p1.X, bottomY),
                    p1,
                    p2,
                    new Vector2(p2.X, bottomY)
                ];
                Raylib.DrawTriangle(quad[0], quad[1], quad[2], new Color(220, 20, 50, 35));
                Raylib.DrawTriangle(quad[0], quad[2], quad[3], new Color(220, 20, 50, 35));

                // Glowing flight curve line
                Raylib.DrawLineEx(p1, p2, 6f, new Color(255, 45, 70, 255));
            }
        }

        // Draw active smoke & explosion particles
        foreach (var p in _particles)
        {
            p.Draw(_texSmoke);
        }

        // Draw Aviator Monoplane (if not exploded)
        if (_state != FlightState.Crashed)
        {
            DrawPlane(shake);
        }

        // Draw Big Multiplier Display in Center
        DrawMultiplierOverlay();
    }

    private void DrawPlane(Vector2 shake)
    {
        if (_texPlane.Id == 0) return;

        Vector2 drawPos = _planePos + shake;
        float planeW = 165f;
        float planeH = 105f;

        Rectangle src = new(0, 0, _texPlane.Width, _texPlane.Height);
        Rectangle dest = new(drawPos.X, drawPos.Y, planeW, planeH);
        Vector2 origin = new(planeW * 0.5f, planeH * 0.5f);

        Raylib.DrawTexturePro(_texPlane, src, dest, origin, _planeRotation, Color.White);

        // Draw spinning propeller on the nose
        if (_texPropeller.Id != 0)
        {
            float propW = 21f;
            float propH = 69f;
            float rad = _planeRotation * MathF.PI / 180f;
            Vector2 noseOffset = new(MathF.Cos(rad) * 66f, MathF.Sin(rad) * 66f);
            Vector2 propPos = drawPos + noseOffset;

            Rectangle propSrc = new(0, 0, _texPropeller.Width, _texPropeller.Height);
            Rectangle propDest = new(propPos.X, propPos.Y, propW, propH);
            Vector2 propOrigin = new(propW * 0.5f, propH * 0.5f);

            Raylib.DrawTexturePro(_texPropeller, propSrc, propDest, propOrigin, _planeRotation + _propellerAngle, Color.White);
        }
    }

    private void DrawMultiplierOverlay()
    {
        Rectangle centerRect = new(ArenaX, ArenaY + 105, ArenaWidth, 180);

        if (_state == FlightState.Idle)
        {
            UiFont.DrawCenteredInRect("1.00x", centerRect, 102, Color.Gray, isBold: true);
            UiFont.DrawCenteredInRect("READY FOR TAKE OFF", new Rectangle(ArenaX, ArenaY + 240, ArenaWidth, 36), 24, Color.LightGray, isBold: false);
        }
        else if (_state == FlightState.TakingOff)
        {
            UiFont.DrawCenteredInRect("1.00x", centerRect, 102, Color.Yellow, isBold: true);
            UiFont.DrawCenteredInRect("STARTING ENGINES...", new Rectangle(ArenaX, ArenaY + 240, ArenaWidth, 36), 24, Color.Gold, isBold: true);
        }
        else if (_state == FlightState.InFlight)
        {
            string multText = $"{_multiplier:0.00}x";
            UiFont.DrawCenteredInRect(multText, centerRect, 114, Color.White, isBold: true);

            if (_hasCashedOut)
            {
                string wonText = $"CASHED OUT AT {_cashedOutMultiplier:0.00}x (+{_roundWinnings:N2} COINS)";
                UiFont.DrawCenteredInRect(wonText, new Rectangle(ArenaX, ArenaY + 248, ArenaWidth, 42), 27, Color.Lime, isBold: true);
            }
        }
        else if (_state == FlightState.Crashed)
        {
            UiFont.DrawCenteredInRect("FLEW AWAY!", centerRect, 108, new Color(255, 45, 70, 255), isBold: true);
            string crashResult = $"CRASHED AT {_crashPoint:0.00}x";
            Color resColor = _hasCashedOut ? Color.Lime : new Color(255, 120, 140, 255);
            UiFont.DrawCenteredInRect(crashResult, new Rectangle(ArenaX, ArenaY + 248, ArenaWidth, 42), 27, resColor, isBold: true);
        }
    }

    private void DrawDashboard()
    {
        // Dashboard Base Plate
        Raylib.DrawRectangleRec(DashRect, new Color(19, 25, 40, 255));
        Raylib.DrawRectangleLinesEx(DashRect, 3, new Color(255, 255, 255, 25));

        // ==================== SECTION 1: BETTING CONTROLS ====================
        UiFont.DrawText("BET AMOUNT", 113, 822, 16.5f, Color.Gray, isBold: true);

        // Minus Button
        bool minusHov = UiImageButton.IsHovered(BetMinusRect);
        Raylib.DrawRectangleRec(BetMinusRect, new Color(30, 40, 60, 255));
        Raylib.DrawRectangleLinesEx(BetMinusRect, 1.5f, minusHov ? Color.Gold : Color.LightGray);
        UiFont.DrawCenteredInRect("-", BetMinusRect, 33, Color.White, isBold: true);

        // Bet Amount Pill
        Raylib.DrawRectangleRec(BetPillRect, new Color(12, 16, 26, 255));
        Raylib.DrawRectangleLinesEx(BetPillRect, 1.5f, Color.Gold);
        UiFont.DrawCenteredInRect($"{_bet:N2}", BetPillRect, 27, Color.Gold, isBold: true);

        // Plus Button
        bool plusHov = UiImageButton.IsHovered(BetPlusRect);
        Raylib.DrawRectangleRec(BetPlusRect, new Color(30, 40, 60, 255));
        Raylib.DrawRectangleLinesEx(BetPlusRect, 1.5f, plusHov ? Color.Gold : Color.LightGray);
        UiFont.DrawCenteredInRect("+", BetPlusRect, 33, Color.White, isBold: true);

        // Chip Presets: 10, 50, 100, 500
        for (int i = 0; i < ChipPresets.Length; i++)
        {
            Rectangle chipRect = GetChipRect(i);
            bool chipHov = UiImageButton.IsHovered(chipRect);
            Raylib.DrawRectangleRec(chipRect, new Color(26, 34, 52, 255));
            Raylib.DrawRectangleLinesEx(chipRect, 1.5f, chipHov ? Color.Gold : new Color(255, 255, 255, 30));
            UiFont.DrawCenteredInRect($"{ChipPresets[i]:0}", chipRect, 16.5f, chipHov ? Color.Gold : Color.LightGray, isBold: true);
        }

        // Modifiers: 1/2, 2X, MAX
        bool halfHov = UiImageButton.IsHovered(HalfBetRect);
        Raylib.DrawRectangleRec(HalfBetRect, new Color(26, 34, 52, 255));
        Raylib.DrawRectangleLinesEx(HalfBetRect, 1.5f, halfHov ? Color.Gold : new Color(255, 255, 255, 30));
        UiFont.DrawCenteredInRect("1/2", HalfBetRect, 16.5f, halfHov ? Color.Gold : Color.LightGray, isBold: true);

        bool doubleHov = UiImageButton.IsHovered(DoubleBetRect);
        Raylib.DrawRectangleRec(DoubleBetRect, new Color(26, 34, 52, 255));
        Raylib.DrawRectangleLinesEx(DoubleBetRect, 1.5f, doubleHov ? Color.Gold : new Color(255, 255, 255, 30));
        UiFont.DrawCenteredInRect("2X", DoubleBetRect, 16.5f, doubleHov ? Color.Gold : Color.LightGray, isBold: true);

        bool maxHov = UiImageButton.IsHovered(MaxBetRect);
        Color maxBg = _isMaxBet ? new Color(36, 48, 72, 255) : new Color(26, 34, 52, 255);
        Raylib.DrawRectangleRec(MaxBetRect, maxBg);
        Raylib.DrawRectangleLinesEx(MaxBetRect, _isMaxBet ? 3 : 1.5f, _isMaxBet ? Color.Lime : (maxHov ? Color.Yellow : Color.Gold));
        UiFont.DrawCenteredInRect("MAX", MaxBetRect, 16.5f, _isMaxBet ? Color.Lime : Color.Gold, isBold: true);

        // ==================== SECTION 2: BIG ACTION BUTTON ====================
        if (_state == FlightState.Idle || _state == FlightState.Crashed)
        {
            // Green "BET / LAUNCH" button
            UiImageButton.Draw(_texBtnLaunch, ActionBtnRect);
            UiFont.DrawCenteredInRect("BET", new Rectangle(ActionBtnRect.X, ActionBtnRect.Y + 24, ActionBtnRect.Width, 48), 39, Color.White, isBold: true);
            UiFont.DrawCenteredInRect($"{_bet:N2} COINS", new Rectangle(ActionBtnRect.X, ActionBtnRect.Y + 78, ActionBtnRect.Width, 36), 24, new Color(200, 255, 210, 255), isBold: true);
        }
        else if (_state == FlightState.InFlight && _hasActiveBet && !_hasCashedOut)
        {
            // Amber Gold "CASH OUT" button with live winnings
            UiImageButton.Draw(_texBtnCashout, ActionBtnRect);
            decimal liveWin = decimal.Round(_bet * _multiplier, 2);
            UiFont.DrawCenteredInRect("CASH OUT", new Rectangle(ActionBtnRect.X, ActionBtnRect.Y + 24, ActionBtnRect.Width, 48), 39, Color.White, isBold: true);
            UiFont.DrawCenteredInRect($"{liveWin:N2} COINS", new Rectangle(ActionBtnRect.X, ActionBtnRect.Y + 78, ActionBtnRect.Width, 36), 27, Color.Yellow, isBold: true);
        }
        else
        {
            // Waiting / Cashed Out / Watching
            Raylib.DrawRectangleRec(ActionBtnRect, new Color(35, 45, 65, 255));
            Raylib.DrawRectangleLinesEx(ActionBtnRect, 1.5f, Color.Gray);
            string statusStr = _hasCashedOut ? "CASHED OUT" : "IN FLIGHT";
            Color col = _hasCashedOut ? Color.Lime : Color.Gray;
            UiFont.DrawCenteredInRect(statusStr, ActionBtnRect, 33, col, isBold: true);
        }

        // Keyboard Spacebar Shortcut Helper Tooltip
        UiFont.DrawCenteredInRect("[SPACE] LAUNCH / CASH OUT", new Rectangle(ActionBtnRect.X, ActionBtnRect.Y + ActionBtnRect.Height + 8, ActionBtnRect.Width, 24), 15, new Color(140, 160, 195, 200), isBold: false);

        // ==================== SECTION 3: AUTO-CASHOUT MODULE ====================
        UiFont.DrawText("AUTO CASHOUT", 1178, 822, 16.5f, Color.Gray, isBold: true);

        // Prominent Dedicated Toggle Switch Button (Like Slots Auto/Turbo toggle)
        bool toggleHovered = UiImageButton.IsHovered(AutoToggleBtnRect);
        Color toggleBg = _autoCashoutEnabled ? new Color(18, 38, 28, 255) : new Color(24, 30, 46, 255);
        Color toggleBorder = _autoCashoutEnabled ? Color.Lime : (toggleHovered ? Color.Gold : new Color(255, 255, 255, 40));
        Raylib.DrawRectangleRec(AutoToggleBtnRect, toggleBg);
        Raylib.DrawRectangleLinesEx(AutoToggleBtnRect, _autoCashoutEnabled ? 3 : 1.5f, toggleBorder);

        Color toggleTextColor = _autoCashoutEnabled ? Color.White : Color.LightGray;
        UiFont.DrawText("AUTO CASH", AutoToggleBtnRect.X + 21, AutoToggleBtnRect.Y + 21, 19.5f, toggleTextColor, isBold: true);

        // State Badge [ ON ] / [ OFF ]
        Rectangle badgeRect = new(AutoToggleBtnRect.X + AutoToggleBtnRect.Width - 99, AutoToggleBtnRect.Y + 12, 81, 42);
        Color badgeBg = _autoCashoutEnabled ? new Color(22, 163, 74, 255) : new Color(42, 50, 68, 255);
        Color badgeText = _autoCashoutEnabled ? Color.White : Color.Gray;
        Raylib.DrawRectangleRec(badgeRect, badgeBg);
        Raylib.DrawRectangleLinesEx(badgeRect, 1.5f, _autoCashoutEnabled ? Color.Lime : Color.Gray);
        UiFont.DrawCenteredInRect(_autoCashoutEnabled ? "ON" : "OFF", badgeRect, 19.5f, badgeText, isBold: true);

        // Multiplier Adjust Stepper: [-]  [ 2.00x ]  [+]
        bool autoMinusHov = UiImageButton.IsHovered(AutoMinusBtnRect);
        Raylib.DrawRectangleRec(AutoMinusBtnRect, new Color(30, 40, 60, 255));
        Raylib.DrawRectangleLinesEx(AutoMinusBtnRect, 1.5f, autoMinusHov ? Color.Gold : Color.LightGray);
        UiFont.DrawCenteredInRect("-", AutoMinusBtnRect, 33, Color.White, isBold: true);

        bool pillHov = UiImageButton.IsHovered(AutoTargetPillRect);
        Raylib.DrawRectangleRec(AutoTargetPillRect, new Color(12, 16, 26, 255));
        Color pillBorder = _autoCashoutEnabled ? Color.Lime : (pillHov ? Color.Gold : Color.LightGray);
        Raylib.DrawRectangleLinesEx(AutoTargetPillRect, _autoCashoutEnabled ? 3 : 1.5f, pillBorder);
        UiFont.DrawCenteredInRect($"{_autoCashoutTarget:0.00}x", AutoTargetPillRect, 27, Color.Gold, isBold: true);

        bool autoPlusHov = UiImageButton.IsHovered(AutoPlusBtnRect);
        Raylib.DrawRectangleRec(AutoPlusBtnRect, new Color(30, 40, 60, 255));
        Raylib.DrawRectangleLinesEx(AutoPlusBtnRect, 1.5f, autoPlusHov ? Color.Gold : Color.LightGray);
        UiFont.DrawCenteredInRect("+", AutoPlusBtnRect, 33, Color.White, isBold: true);

        // Preset Target Pills: 1.20x, 1.50x, 2.00x, 3.00x, 5.00x
        for (int i = 0; i < AutoPresets.Length; i++)
        {
            Rectangle prRect = GetAutoPresetRect(i);
            bool isCurrent = _autoCashoutTarget == AutoPresets[i];
            bool prHov = UiImageButton.IsHovered(prRect);

            Color prBg = isCurrent ? new Color(36, 48, 72, 255) : new Color(26, 34, 52, 255);
            Color prBorder = isCurrent ? Color.Gold : (prHov ? Color.Yellow : new Color(255, 255, 255, 30));
            Color prText = isCurrent ? Color.Gold : (prHov ? Color.White : Color.LightGray);

            Raylib.DrawRectangleRec(prRect, prBg);
            Raylib.DrawRectangleLinesEx(prRect, isCurrent ? 3 : 1.5f, prBorder);
            UiFont.DrawCenteredInRect($"{AutoPresets[i]:0.00}x", prRect, 18, prText, isBold: true);
        }
    }

    // Modal Dialog Layout Rectangles
    private static readonly Rectangle ModalBoxRect = new(585, 330, 750, 420);
    private static readonly Rectangle ModalDepositBtnRect = new(630, 638, 308, 72);
    private static readonly Rectangle ModalLobbyBtnRect = new(983, 638, 308, 72);
    private static readonly Rectangle ModalCloseBtnRect = new(1281, 348, 39, 39);

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
        Raylib.DrawRectangleLinesEx(ModalBoxRect, 3, Color.Gold);

        // 3. Close Button [X]
        bool closeHov = UiImageButton.IsHovered(ModalCloseBtnRect);
        Raylib.DrawRectangleRec(ModalCloseBtnRect, closeHov ? new Color(220, 38, 38, 200) : new Color(36, 44, 62, 255));
        Raylib.DrawRectangleLinesEx(ModalCloseBtnRect, 1.5f, closeHov ? Color.Red : Color.Gray);
        UiFont.DrawCenteredInRect("X", ModalCloseBtnRect, 21, Color.White, isBold: true);

        // 4. Header Title & Icon
        Rectangle titleRect = new(ModalBoxRect.X, ModalBoxRect.Y + 42, ModalBoxRect.Width, 51);
        UiFont.DrawCenteredInRect("! OUT OF COINS !", titleRect, 36, Color.Gold, isBold: true);

        // 5. Message Body
        Rectangle msg1Rect = new(ModalBoxRect.X + 30, ModalBoxRect.Y + 120, ModalBoxRect.Width - 60, 42);
        UiFont.DrawCenteredInRect($"You need {_bet:N2} coins to place this bet.", msg1Rect, 27, Color.White);

        Rectangle msg2Rect = new(ModalBoxRect.X + 30, ModalBoxRect.Y + 172, ModalBoxRect.Width - 60, 39);
        UiFont.DrawCenteredInRect($"Your current balance: {_currency.Balance:N2} coins", msg2Rect, 24, Color.LightGray);

        Rectangle hintRect = new(ModalBoxRect.X + 30, ModalBoxRect.Y + 228, ModalBoxRect.Width - 60, 33);
        UiFont.DrawCenteredInRect("Deposit coins now or return to the main lobby.", hintRect, 21, new Color(160, 174, 192, 255));

        // 6. Action Buttons
        // Deposit More
        bool depHov = UiImageButton.IsHovered(ModalDepositBtnRect);
        Color depBg = depHov ? new Color(40, 200, 100, 255) : new Color(34, 160, 80, 255);
        Raylib.DrawRectangleRec(ModalDepositBtnRect, depBg);
        Raylib.DrawRectangleLinesEx(ModalDepositBtnRect, 3, depHov ? Color.Lime : Color.Gold);
        UiFont.DrawCenteredInRect("DEPOSIT MORE", ModalDepositBtnRect, 24, Color.White, isBold: true);

        // Return to Lobby
        bool lobHov = UiImageButton.IsHovered(ModalLobbyBtnRect);
        Color lobBg = lobHov ? new Color(71, 85, 105, 255) : new Color(51, 65, 85, 255);
        Raylib.DrawRectangleRec(ModalLobbyBtnRect, lobBg);
        Raylib.DrawRectangleLinesEx(ModalLobbyBtnRect, 1.5f, lobHov ? Color.White : Color.Gray);
        UiFont.DrawCenteredInRect("RETURN TO LOBBY", ModalLobbyBtnRect, 22.5f, Color.LightGray, isBold: true);
    }
}

// Particle system for exhaust smoke, spark fountains, and crash explosions
public sealed class CrashParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color ParticleColor;
    public float Lifetime;
    public float MaxLifetime;
    public float Size;

    public bool IsDead => Lifetime <= 0f;

    public CrashParticle(Vector2 pos, Vector2 vel, Color color, float lifetime, float size)
    {
        Position = pos;
        Velocity = vel;
        ParticleColor = color;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        Velocity *= 0.96f; // drag
        Lifetime -= deltaTime;
    }

    public void Draw(Texture2D smokeTex)
    {
        float alpha = Math.Clamp(Lifetime / MaxLifetime, 0f, 1f);
        Color c = new(ParticleColor.R, ParticleColor.G, ParticleColor.B, (byte)(ParticleColor.A * alpha));

        if (smokeTex.Id != 0)
        {
            float s = Size * (1.2f - alpha * 0.2f);
            Rectangle src = new(0, 0, smokeTex.Width, smokeTex.Height);
            Rectangle dest = new(Position.X - s * 0.5f, Position.Y - s * 0.5f, s, s);
            Raylib.DrawTexturePro(smokeTex, src, dest, Vector2.Zero, 0f, c);
        }
        else
        {
            Raylib.DrawCircleV(Position, Size * 0.5f, c);
        }
    }
}
