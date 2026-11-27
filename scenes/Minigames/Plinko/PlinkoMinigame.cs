using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

public sealed class PlinkoMinigame : IMinigame, IDepositRequester
{
    private const decimal MinimumBet = 1m;
    private const decimal DefaultBet = 10m;
    private const int MaxActiveBalls = 28;
    private const int MaxHistory = 13;

    // Layout Regions (1920 x 1080)
    private static readonly Rectangle LeftPanelRect = new(36, 114, 462, 936);
    private static readonly Rectangle BoardRect = new(522, 114, 996, 936);
    private static readonly Rectangle RightPanelRect = new(1542, 114, 342, 936);

    // Control Rectangles inside Left Panel
    private static readonly Rectangle BetMinusRect = new(60, 183, 63, 60);
    private static readonly Rectangle BetPillRect = new(132, 183, 270, 60);
    private static readonly Rectangle BetPlusRect = new(411, 183, 63, 60);

    private static readonly decimal[] BetPresets = [1m, 5m, 10m, 25m, 50m, 100m];

    private static readonly Rectangle HalfBetRect = new(60, 318, 129, 45);
    private static readonly Rectangle DoubleBetRect = new(202.5f, 318, 129, 45);
    private static readonly Rectangle MaxBetRect = new(345, 318, 129, 45);

    private static readonly Rectangle RiskLowRect = new(60, 414, 132, 54);
    private static readonly Rectangle RiskMedRect = new(201, 414, 132, 54);
    private static readonly Rectangle RiskHighRect = new(342, 414, 132, 54);

    private static readonly Rectangle Rows8Rect = new(60, 519, 132, 54);
    private static readonly Rectangle Rows12Rect = new(201, 519, 132, 54);
    private static readonly Rectangle Rows16Rect = new(342, 519, 132, 54);

    private static readonly Rectangle DropBtnRect = new(60, 606, 414, 102);
    private static readonly Rectangle AutoToggleRect = new(60, 738, 414, 66);
    private static readonly Rectangle StatsBoxRect = new(60, 831, 414, 192);

    // Out of Coins Modal Rectangles
    private static readonly Rectangle ModalBoxRect = new(585, 330, 750, 420);
    private static readonly Rectangle ModalDepositBtnRect = new(630, 638, 308, 72);
    private static readonly Rectangle ModalLobbyBtnRect = new(983, 638, 308, 72);
    private static readonly Rectangle ModalCloseBtnRect = new(1281, 348, 39, 39);

    private readonly CurrencyManager _currency;
    private readonly Random _rand = new();

    // State
    private decimal _bet = DefaultBet;
    private PlinkoRisk _risk = PlinkoRisk.Medium;
    private int _rows = 12;
    private bool _autoDrop;
    private float _autoDropTimer;
    private float _chutePulse;
    private float _screenShake;
    private float _pegSoundCooldown;
    private bool _showOutOfCoinsModal;
    private bool _consumedEscapeThisFrame;

    // Session Stats
    private int _totalDrops;
    private decimal _totalWagered;
    private decimal _totalReturned;
    private decimal _bestWin;

    // Active Simulation Entities
    private readonly List<PlinkoBall> _balls = [];
    private readonly List<PlinkoParticle> _particles = [];
    private readonly List<FloatingPayout> _popups = [];
    private readonly List<PlinkoDropRecord> _history = [];

    // Board Reactive FX
    private readonly Dictionary<(int Row, int Col), float> _pegFlashes = [];
    private float[] _bucketBounce = new float[17];
    private float[] _bucketFlash = new float[17];

    // Audio
    private Sound _sndDrop;
    private Sound _sndPeg;
    private Sound _sndWin;
    private Sound _sndBigWin;
    private Sound _sndLoss;
    private Sound _sndClick;
    private Sound _sndError;
    private bool _audioLoaded;

    public string Name => "Plinko";
    public bool IsFinished { get; private set; }
    public bool WantsDeposit { get; private set; }

    public PlinkoMinigame(CurrencyManager currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public void Start()
    {
        IsFinished = false;
        WantsDeposit = false;
        _showOutOfCoinsModal = false;
        _consumedEscapeThisFrame = false;
        _autoDrop = false;
        _autoDropTimer = 0f;
        _chutePulse = 0f;
        _screenShake = 0f;
        _balls.Clear();
        _particles.Clear();
        _popups.Clear();
        _pegFlashes.Clear();
        Array.Clear(_bucketBounce);
        Array.Clear(_bucketFlash);
        _bet = Math.Max(MinimumBet, Math.Min(DefaultBet, _currency.Balance > 0m ? _currency.Balance : DefaultBet));

        LoadAudio();
    }

    public void Stop()
    {
        // Settle any in-flight balls so the player never loses already-paid wagers on exit
        foreach (var ball in _balls)
        {
            decimal mult = PlinkoTables.GetMultipliers(ball.Rows, ball.Risk)[ball.TargetBucket];
            decimal payout = decimal.Round(ball.Bet * mult, 2);
            if (payout > 0m)
            {
                _currency.Add(payout);
            }
        }
        _balls.Clear();
        _particles.Clear();
        _popups.Clear();
        _autoDrop = false;
        UnloadAudio();
    }

    private void LoadAudio()
    {
        if (_audioLoaded || !Raylib.IsAudioDeviceReady()) return;
        try
        {
            _sndDrop = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_drop.wav");
            _sndPeg = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_peg.wav");
            _sndWin = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_win.wav");
            _sndBigWin = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_bigwin.wav");
            _sndLoss = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_loss.wav");
            _sndClick = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_click.wav");
            _sndError = LoadSoundSafe("scenes/Minigames/Plinko/assets/audio/sfx_error.wav");
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
        UnloadSoundSafe(_sndDrop);
        UnloadSoundSafe(_sndPeg);
        UnloadSoundSafe(_sndWin);
        UnloadSoundSafe(_sndBigWin);
        UnloadSoundSafe(_sndLoss);
        UnloadSoundSafe(_sndClick);
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

    // ---------------- GEOMETRY HELPERS ----------------
    private float BoardCenterX => BoardRect.X + BoardRect.Width * 0.5f;
    private float PyramidTopY => BoardRect.Y + 87f;
    private float PyramidBottomY => BoardRect.Y + 765f;
    private float BucketTopY => BoardRect.Y + 813f;
    private const float BucketHeight = 63f;

    private float GetSpacingX(int rows) => rows switch
    {
        8 => 84f,
        12 => 61.5f,
        16 => 48.75f,
        _ => 61.5f
    };

    private float GetSpacingY(int rows) => (PyramidBottomY - PyramidTopY) / Math.Max(1, rows - 1);

    private float GetPegRadius(int rows) => rows switch
    {
        8 => 8.25f,
        12 => 6.75f,
        16 => 5.55f,
        _ => 6.75f
    };

    private float GetBallRadius(int rows) => rows switch
    {
        8 => 15.75f,
        12 => 12.75f,
        16 => 10.2f,
        _ => 12.75f
    };

    private Vector2 GetPegPosition(int rows, int row, int col)
    {
        float dx = GetSpacingX(rows);
        float dy = GetSpacingY(rows);
        float x = BoardCenterX + (col - row * 0.5f) * dx;
        float y = PyramidTopY + row * dy;
        return new Vector2(x, y);
    }

    private Rectangle GetBucketRect(int rows, int bucketIndex)
    {
        float dx = GetSpacingX(rows);
        float cx = BoardCenterX + (bucketIndex - rows * 0.5f) * dx;
        float w = dx - 6f;
        float bounceOffset = bucketIndex < _bucketBounce.Length ? MathF.Sin(_bucketBounce[bucketIndex] * MathF.PI) * 10.5f : 0f;
        return new Rectangle(cx - w * 0.5f, BucketTopY + bounceOffset, w, BucketHeight);
    }

    private static Rectangle GetPresetRect(int index)
    {
        int col = index % 3;
        int row = index / 3;
        return new Rectangle(60 + col * 142.5f, 255 + row * 51, 129, 42);
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

        // Decay timers
        if (_chutePulse > 0f) _chutePulse = Math.Max(0f, _chutePulse - dt * 3.8f);
        if (_screenShake > 0f) _screenShake = Math.Max(0f, _screenShake - dt * 3.0f);
        if (_pegSoundCooldown > 0f) _pegSoundCooldown = Math.Max(0f, _pegSoundCooldown - dt);

        // Decay peg flashes
        if (_pegFlashes.Count > 0)
        {
            var keys = new List<(int, int)>(_pegFlashes.Keys);
            foreach (var key in keys)
            {
                float v = _pegFlashes[key] - dt * 2.4f;
                if (v <= 0f) _pegFlashes.Remove(key);
                else _pegFlashes[key] = v;
            }
        }

        // Decay bucket animations
        for (int i = 0; i < _bucketBounce.Length; i++)
        {
            if (_bucketBounce[i] > 0f) _bucketBounce[i] = Math.Max(0f, _bucketBounce[i] - dt * 4.5f);
            if (_bucketFlash[i] > 0f) _bucketFlash[i] = Math.Max(0f, _bucketFlash[i] - dt * 2.2f);
        }

        UpdateControls(dt);
        UpdateBalls(dt);
        UpdateParticlesAndPopups(dt);
    }

    private void UpdateControls(float dt)
    {
        bool hasActiveBalls = _balls.Count > 0;

        // Bet Minus / Plus
        if (UiImageButton.WasClicked(BetMinusRect))
        {
            _bet = Math.Max(MinimumBet, _bet - GetBetStep(_bet, false));
            PlaySnd(_sndClick, 0.5f, 0.92f);
        }
        if (UiImageButton.WasClicked(BetPlusRect))
        {
            decimal next = _bet + GetBetStep(_bet, true);
            _bet = _currency.Balance >= MinimumBet ? Math.Min(next, _currency.Balance) : next;
            PlaySnd(_sndClick, 0.5f, 1.08f);
        }

        // Quick Presets
        for (int i = 0; i < BetPresets.Length; i++)
        {
            if (UiImageButton.WasClicked(GetPresetRect(i)))
            {
                _bet = BetPresets[i];
                PlaySnd(_sndClick, 0.5f, 1.0f);
            }
        }

        // Modifiers: 1/2, 2X, MAX
        if (UiImageButton.WasClicked(HalfBetRect))
        {
            _bet = Math.Max(MinimumBet, decimal.Round(_bet * 0.5m, 2));
            PlaySnd(_sndClick, 0.5f, 0.9f);
        }
        if (UiImageButton.WasClicked(DoubleBetRect))
        {
            decimal doubled = decimal.Round(_bet * 2m, 2);
            _bet = _currency.Balance >= MinimumBet ? Math.Min(doubled, _currency.Balance) : doubled;
            PlaySnd(_sndClick, 0.5f, 1.12f);
        }
        if (UiImageButton.WasClicked(MaxBetRect))
        {
            if (_currency.Balance >= MinimumBet)
            {
                _bet = Math.Floor(_currency.Balance * 100m) / 100m;
                PlaySnd(_sndClick, 0.6f, 1.2f);
            }
            else
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.7f);
            }
        }

        // Risk & Rows (locked only while balls are actively falling to keep board consistent)
        if (!hasActiveBalls)
        {
            if (UiImageButton.WasClicked(RiskLowRect) && _risk != PlinkoRisk.Low)
            {
                _risk = PlinkoRisk.Low;
                PlaySnd(_sndClick, 0.55f, 0.95f);
            }
            else if (UiImageButton.WasClicked(RiskMedRect) && _risk != PlinkoRisk.Medium)
            {
                _risk = PlinkoRisk.Medium;
                PlaySnd(_sndClick, 0.55f, 1.05f);
            }
            else if (UiImageButton.WasClicked(RiskHighRect) && _risk != PlinkoRisk.High)
            {
                _risk = PlinkoRisk.High;
                PlaySnd(_sndClick, 0.55f, 1.15f);
            }

            if (UiImageButton.WasClicked(Rows8Rect) && _rows != 8)
            {
                SetRows(8);
            }
            else if (UiImageButton.WasClicked(Rows12Rect) && _rows != 12)
            {
                SetRows(12);
            }
            else if (UiImageButton.WasClicked(Rows16Rect) && _rows != 16)
            {
                SetRows(16);
            }
        }

        // Auto Drop Toggle
        if (UiImageButton.WasClicked(AutoToggleRect))
        {
            _autoDrop = !_autoDrop;
            _autoDropTimer = 0f;
            PlaySnd(_sndClick, 0.6f, _autoDrop ? 1.15f : 0.85f);
        }

        // Manual Drop Button or Spacebar
        if (UiImageButton.WasClicked(DropBtnRect) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            TrySpawnBall(isManual: true);
        }

        // Auto Drop Ticker
        if (_autoDrop)
        {
            _autoDropTimer -= dt;
            if (_autoDropTimer <= 0f)
            {
                if (TrySpawnBall(isManual: false))
                {
                    _autoDropTimer = 0.32f;
                }
                else
                {
                    _autoDrop = false;
                }
            }
        }
    }

    private void SetRows(int newRows)
    {
        _rows = newRows;
        _pegFlashes.Clear();
        Array.Clear(_bucketBounce);
        Array.Clear(_bucketFlash);
        PlaySnd(_sndClick, 0.55f, 1.0f);
    }

    private static decimal GetBetStep(decimal current, bool up)
    {
        if (current < 10m || (!up && current <= 10m)) return 1m;
        if (current < 50m || (!up && current <= 50m)) return 5m;
        if (current < 200m || (!up && current <= 200m)) return 25m;
        return 50m;
    }

    private bool TrySpawnBall(bool isManual)
    {
        if (_balls.Count >= MaxActiveBalls) return false;

        if (_bet < MinimumBet) _bet = MinimumBet;

        if (_currency.Balance < _bet || !_currency.TrySpend(_bet))
        {
            if (isManual)
            {
                _showOutOfCoinsModal = true;
                PlaySnd(_sndError, 0.75f);
            }
            return false;
        }

        int rows = _rows;
        PlinkoRisk risk = _risk;
        float ballRadius = GetBallRadius(rows);
        float pegRadius = GetPegRadius(rows);

        // Generate exact unbiased binomial path
        var waypoints = new List<Vector2>(rows + 2);
        var pegCoords = new List<(int Row, int Col)>(rows);

        float startJitter = ((float)_rand.NextDouble() - 0.5f) * 14f;
        waypoints.Add(new Vector2(BoardCenterX + startJitter, BoardRect.Y + 26f));

        int k = 0;
        for (int r = 0; r < rows; r++)
        {
            Vector2 pegPos = GetPegPosition(rows, r, k);
            pegCoords.Add((r, k));

            // Decide bounce direction for next row
            int dir = _rand.Next(2); // 0 = left, 1 = right
            float sideOffset = (dir == 0 ? -1f : 1f) * (pegRadius * 0.45f);
            float microJitter = ((float)_rand.NextDouble() - 0.5f) * 2.5f;
            waypoints.Add(new Vector2(pegPos.X + sideOffset + microJitter, pegPos.Y - pegRadius - ballRadius * 0.85f));

            k += dir;
        }

        int targetBucket = k;
        Rectangle bucketRect = GetBucketRect(rows, targetBucket);
        waypoints.Add(new Vector2(bucketRect.X + bucketRect.Width * 0.5f, BucketTopY + 12f));

        // Segment speed scales smoothly with row count so 8, 12, and 16 rows all feel snappy
        float segDuration = rows switch
        {
            8 => 0.175f,
            12 => 0.140f,
            16 => 0.115f,
            _ => 0.140f
        };

        var ball = new PlinkoBall
        {
            Bet = _bet,
            Rows = rows,
            Risk = risk,
            TargetBucket = targetBucket,
            Waypoints = waypoints,
            PegCoords = pegCoords,
            SegmentIndex = 0,
            SegmentT = 0f,
            SegmentDuration = segDuration * (0.94f + (float)_rand.NextDouble() * 0.12f),
            Position = waypoints[0],
            Color = PlinkoTables.GetRiskAccentColor(risk)
        };

        _balls.Add(ball);
        _totalDrops++;
        _totalWagered += _bet;
        _chutePulse = 1f;
        PlaySnd(_sndDrop, 0.55f, 0.95f + (float)_rand.NextDouble() * 0.15f);
        return true;
    }

    private void UpdateBalls(float dt)
    {
        for (int i = _balls.Count - 1; i >= 0; i--)
        {
            var ball = _balls[i];
            ball.SegmentT += dt / ball.SegmentDuration;

            while (ball.SegmentT >= 1f && ball.SegmentIndex < ball.Waypoints.Count - 1)
            {
                ball.SegmentT -= 1f;
                ball.SegmentIndex++;

                // Did we just hit a peg?
                int pegIdx = ball.SegmentIndex - 1;
                if (pegIdx >= 0 && pegIdx < ball.PegCoords.Count)
                {
                    var coord = ball.PegCoords[pegIdx];
                    _pegFlashes[coord] = 1f;

                    if (_pegSoundCooldown <= 0f)
                    {
                        float pitch = 0.85f + (coord.Row / (float)ball.Rows) * 0.48f + ((float)_rand.NextDouble() - 0.5f) * 0.06f;
                        PlaySnd(_sndPeg, 0.35f, pitch);
                        _pegSoundCooldown = 0.025f;
                    }
                }

                // Did we reach the bucket?
                if (ball.SegmentIndex >= ball.Waypoints.Count - 1)
                {
                    SettleBall(ball);
                    _balls.RemoveAt(i);
                    break;
                }
            }

            if (i < _balls.Count && ReferenceEquals(_balls[i], ball))
            {
                Vector2 p0 = ball.Waypoints[ball.SegmentIndex];
                Vector2 p1 = ball.Waypoints[ball.SegmentIndex + 1];
                float t = Math.Clamp(ball.SegmentT, 0f, 1f);

                // Smooth horizontal ease-out + juicy gravity bounce arc vertically
                float xEase = ball.SegmentIndex == 0 ? t : 1f - MathF.Pow(1f - t, 1.35f);
                float x = p0.X + (p1.X - p0.X) * xEase;

                float hopHeight = ball.SegmentIndex == 0
                    ? 0f
                    : GetSpacingY(ball.Rows) * 0.44f;

                float yLinear = p0.Y + (p1.Y - p0.Y) * (ball.SegmentIndex == 0 ? t * t : t);
                float arc = hopHeight * 4f * t * (1f - t);
                float y = yLinear - arc;

                ball.Position = new Vector2(x, y);
                ball.Trail.Add(ball.Position);
                if (ball.Trail.Count > 10) ball.Trail.RemoveAt(0);
            }
        }
    }

    private void SettleBall(PlinkoBall ball)
    {
        decimal[] mults = PlinkoTables.GetMultipliers(ball.Rows, ball.Risk);
        decimal mult = mults[ball.TargetBucket];
        decimal payout = decimal.Round(ball.Bet * mult, 2);
        decimal profit = payout - ball.Bet;

        if (payout > 0m)
        {
            _currency.Add(payout);
        }

        _totalReturned += payout;
        if (payout > _bestWin) _bestWin = payout;

        // Bucket visual bounce & flash
        if (ball.TargetBucket < _bucketBounce.Length)
        {
            _bucketBounce[ball.TargetBucket] = 1f;
            _bucketFlash[ball.TargetBucket] = 1f;
        }

        Rectangle bucketRect = GetBucketRect(ball.Rows, ball.TargetBucket);
        Vector2 burstOrigin = new(bucketRect.X + bucketRect.Width * 0.5f, BucketTopY + 10f);
        Color bucketColor = PlinkoTables.GetBucketColor(ball.TargetBucket, mults.Length);

        // Record history
        _history.Insert(0, new PlinkoDropRecord(mult, ball.Bet, payout, profit, bucketColor));
        if (_history.Count > MaxHistory) _history.RemoveAt(_history.Count - 1);

        // Floating text popup
        string popText = mult >= 5m ? $"{PlinkoTables.FormatMultiplier(mult)}  +{payout:0.##}" : $"+{payout:0.##}";
        Color popColor = mult >= 5m ? Color.Gold : mult >= 1m ? Color.Lime : new Color(220, 170, 130, 255);
        _popups.Add(new FloatingPayout(burstOrigin + new Vector2(0, -14), popText, popColor, mult >= 5m ? 20 : 15));

        // Particle burst scaled by win tier
        int particleCount = mult >= 20m ? 36 : mult >= 5m ? 22 : mult >= 1m ? 12 : 5;
        for (int p = 0; p < particleCount; p++)
        {
            float angle = -MathF.PI * (0.15f + (float)_rand.NextDouble() * 0.70f);
            float speed = (mult >= 5m ? 130f : 75f) + (float)_rand.NextDouble() * (mult >= 10m ? 190f : 110f);
            Color pc = mult >= 5m && p % 2 == 0 ? Color.Gold : bucketColor;
            _particles.Add(new PlinkoParticle(
                burstOrigin,
                new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                pc,
                0.45f + (float)_rand.NextDouble() * 0.45f,
                mult >= 5m ? 4.5f : 3.2f));
        }

        // Audio & screen shake
        if (mult >= 5m)
        {
            PlaySnd(_sndBigWin, 0.85f, mult >= 25m ? 1.08f : 1.0f);
            if (mult >= 10m) _screenShake = Math.Min(1f, _screenShake + 0.65f);
        }
        else if (mult >= 1m)
        {
            PlaySnd(_sndWin, 0.65f, 1.0f);
        }
        else
        {
            PlaySnd(_sndLoss, 0.5f, 1.0f);
        }
    }

    private void UpdateParticlesAndPopups(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Velocity.Y += 360f * dt; // Gravity
            p.Position += p.Velocity * dt;
            p.Life -= dt;
            if (p.Life <= 0f) _particles.RemoveAt(i);
        }

        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var pop = _popups[i];
            pop.Position.Y -= 38f * dt;
            pop.Life -= dt;
            if (pop.Life <= 0f) _popups.RemoveAt(i);
        }
    }

    // ---------------- DRAW ----------------
    public void Draw()
    {
        // Background
        Raylib.ClearBackground(new Color(14, 19, 34, 255));

        // Subtle grid mesh
        for (int x = 0; x < GameCanvas.Width; x += 96)
            Raylib.DrawLine(x, 93, x, GameCanvas.Height, new Color(255, 255, 255, 4));
        for (int y = 93; y < GameCanvas.Height; y += 96)
            Raylib.DrawLine(0, y, GameCanvas.Width, y, new Color(255, 255, 255, 4));

        float shakeX = _screenShake > 0f ? ((float)_rand.NextDouble() * 2f - 1f) * 9f * _screenShake : 0f;
        float shakeY = _screenShake > 0f ? ((float)_rand.NextDouble() * 2f - 1f) * 9f * _screenShake : 0f;

        DrawLeftControls();
        DrawBoard(shakeX, shakeY);
        DrawRightHistory();

        bool backClicked = BingoPlusHeader.Draw("PLINKO", _currency);
        if (backClicked && !_showOutOfCoinsModal && !_consumedEscapeThisFrame)
        {
            IsFinished = true;
        }

        if (_showOutOfCoinsModal)
        {
            DrawOutOfCoinsModal();
        }
    }

    private void DrawLeftControls()
    {
        Raylib.DrawRectangleRec(LeftPanelRect, new Color(19, 25, 40, 255));
        Raylib.DrawRectangleLinesEx(LeftPanelRect, 2, new Color(255, 255, 255, 22));

        // Header Accent
        UiFont.DrawText("BET AMOUNT", 60, 144, 18, new Color(156, 168, 192, 255), isBold: true);

        // Minus / Pill / Plus
        DrawStyledButton(BetMinusRect, "-", 33, new Color(30, 40, 62, 255), Color.White);
        Raylib.DrawRectangleRec(BetPillRect, new Color(11, 15, 25, 255));
        Raylib.DrawRectangleLinesEx(BetPillRect, 1.5f, Color.Gold);
        UiFont.DrawCenteredInRect($"{_bet:N2}", BetPillRect, 27, Color.Gold, isBold: true);
        DrawStyledButton(BetPlusRect, "+", 33, new Color(30, 40, 62, 255), Color.White);

        // Quick Bet Presets
        for (int i = 0; i < BetPresets.Length; i++)
        {
            Rectangle r = GetPresetRect(i);
            bool active = _bet == BetPresets[i];
            DrawTogglePill(r, $"{BetPresets[i]:0}", active, Color.Gold, 19.5f);
        }

        // Modifiers
        DrawStyledButton(HalfBetRect, "1/2", 19.5f, new Color(26, 34, 52, 255), Color.LightGray);
        DrawStyledButton(DoubleBetRect, "2X", 19.5f, new Color(26, 34, 52, 255), Color.LightGray);
        DrawStyledButton(MaxBetRect, "MAX", 19.5f, new Color(38, 32, 16, 255), Color.Gold, borderOverride: Color.Gold);

        // Risk Level
        bool locked = _balls.Count > 0;
        UiFont.DrawText(locked ? "RISK LEVEL (LOCKED IN FLIGHT)" : "RISK LEVEL", 60, 381, 18,
            locked ? new Color(245, 158, 11, 220) : new Color(156, 168, 192, 255), isBold: true);

        DrawTogglePill(RiskLowRect, "LOW", _risk == PlinkoRisk.Low, new Color(34, 197, 94, 255), 19.5f, dimmed: locked && _risk != PlinkoRisk.Low);
        DrawTogglePill(RiskMedRect, "MED", _risk == PlinkoRisk.Medium, new Color(245, 158, 11, 255), 19.5f, dimmed: locked && _risk != PlinkoRisk.Medium);
        DrawTogglePill(RiskHighRect, "HIGH", _risk == PlinkoRisk.High, new Color(239, 68, 68, 255), 19.5f, dimmed: locked && _risk != PlinkoRisk.High);

        // Rows Selector
        UiFont.DrawText("PEG ROWS", 60, 486, 18, new Color(156, 168, 192, 255), isBold: true);
        DrawTogglePill(Rows8Rect, "8 ROWS", _rows == 8, Color.Gold, 18, dimmed: locked && _rows != 8);
        DrawTogglePill(Rows12Rect, "12 ROWS", _rows == 12, Color.Gold, 18, dimmed: locked && _rows != 12);
        DrawTogglePill(Rows16Rect, "16 ROWS", _rows == 16, Color.Gold, 18, dimmed: locked && _rows != 16);

        // Big Drop Ball CTA Button
        bool dropHov = UiImageButton.IsHovered(DropBtnRect);
        Color dropBg = dropHov ? new Color(34, 197, 94, 255) : new Color(22, 163, 74, 255);
        Raylib.DrawRectangleRec(DropBtnRect, dropBg);
        Raylib.DrawRectangleLinesEx(DropBtnRect, 3, dropHov ? Color.White : new Color(134, 239, 172, 255));
        UiFont.DrawCenteredInRect("DROP BALL", new Rectangle(DropBtnRect.X, DropBtnRect.Y + 15, DropBtnRect.Width, 42), 36, Color.White, isBold: true);
        UiFont.DrawCenteredInRect($"{_bet:N2} COINS  |  [SPACE]", new Rectangle(DropBtnRect.X, DropBtnRect.Y + 61.5f, DropBtnRect.Width, 30), 18, new Color(220, 255, 230, 255), isBold: true);

        // Auto Drop Toggle
        bool autoHov = UiImageButton.IsHovered(AutoToggleRect);
        Color autoBg = _autoDrop ? new Color(20, 48, 36, 255) : new Color(24, 31, 48, 255);
        Color autoBorder = _autoDrop ? Color.Lime : (autoHov ? Color.Gold : new Color(255, 255, 255, 35));
        Raylib.DrawRectangleRec(AutoToggleRect, autoBg);
        Raylib.DrawRectangleLinesEx(AutoToggleRect, _autoDrop ? 3 : 1.5f, autoBorder);
        UiFont.DrawText("AUTO DROP", AutoToggleRect.X + 24, AutoToggleRect.Y + 19.5f, 21, _autoDrop ? Color.White : Color.LightGray, isBold: true);

        Rectangle badgeRect = new(AutoToggleRect.X + AutoToggleRect.Width - 99, AutoToggleRect.Y + 12, 81, 42);
        Raylib.DrawRectangleRec(badgeRect, _autoDrop ? new Color(22, 163, 74, 255) : new Color(42, 50, 68, 255));
        Raylib.DrawRectangleLinesEx(badgeRect, 1.5f, _autoDrop ? Color.Lime : Color.Gray);
        UiFont.DrawCenteredInRect(_autoDrop ? "ON" : "OFF", badgeRect, 19.5f, _autoDrop ? Color.White : Color.Gray, isBold: true);

        // Session Stats Box
        Raylib.DrawRectangleRec(StatsBoxRect, new Color(13, 18, 30, 255));
        Raylib.DrawRectangleLinesEx(StatsBoxRect, 1.5f, new Color(255, 255, 255, 20));
        UiFont.DrawText("SESSION STATS", StatsBoxRect.X + 21, StatsBoxRect.Y + 15, 16.5f, Color.Gold, isBold: true);

        decimal netProfit = _totalReturned - _totalWagered;
        Color netColor = netProfit > 0m ? Color.Lime : netProfit < 0m ? new Color(248, 113, 113, 255) : Color.LightGray;

        DrawStatRow(StatsBoxRect.X + 21, StatsBoxRect.Y + 51, StatsBoxRect.Width - 42, "Balls Dropped", $"{_totalDrops}", Color.White);
        DrawStatRow(StatsBoxRect.X + 21, StatsBoxRect.Y + 84, StatsBoxRect.Width - 42, "In Flight", $"{_balls.Count}", new Color(147, 197, 253, 255));
        DrawStatRow(StatsBoxRect.X + 21, StatsBoxRect.Y + 117, StatsBoxRect.Width - 42, "Best Payout", $"{_bestWin:N2}", Color.Gold);
        DrawStatRow(StatsBoxRect.X + 21, StatsBoxRect.Y + 150, StatsBoxRect.Width - 42, "Net Profit", $"{(netProfit >= 0m ? "+" : "")}{netProfit:N2}", netColor);
    }

    private static void DrawStatRow(float x, float y, float width, string label, string value, Color valColor)
    {
        UiFont.DrawText(label, x, y, 19.5f, new Color(160, 172, 195, 255));
        float vw = UiFont.MeasureText(value, 19.5f, isBold: true).X;
        UiFont.DrawText(value, x + width - vw, y, 19.5f, valColor, isBold: true);
    }

    private void DrawBoard(float shakeX, float shakeY)
    {
        Rectangle shiftedBoard = new(BoardRect.X + shakeX, BoardRect.Y + shakeY, BoardRect.Width, BoardRect.Height);
        Raylib.DrawRectangleRec(shiftedBoard, new Color(15, 21, 36, 255));
        Raylib.DrawRectangleLinesEx(shiftedBoard, 2, new Color(255, 195, 0, 70));

        // Top Drop Chute / Emitter
        float cx = BoardCenterX + shakeX;
        float chuteY = BoardRect.Y + 18f + shakeY;
        Color riskAccent = PlinkoTables.GetRiskAccentColor(_risk);
        if (_chutePulse > 0f)
        {
            Raylib.DrawCircleV(new Vector2(cx, chuteY + 8), 16f + (1f - _chutePulse) * 26f,
                new Color(riskAccent.R, riskAccent.G, riskAccent.B, (byte)(120 * _chutePulse)));
        }
        Raylib.DrawRectangleRounded(new Rectangle(cx - 32, chuteY - 6, 64, 18), 0.5f, 6, new Color(30, 40, 64, 255));
        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(cx - 32, chuteY - 6, 64, 18), 0.5f, 6, 1.5f, new Color(255, 215, 0, 150));
        Raylib.DrawCircleV(new Vector2(cx, chuteY + 10), 6f, riskAccent);

        // Draw Peg Pyramid (rows 0.._rows-1, columns -1..r+1 so outer boundary pegs frame every bucket)
        float pegRadius = GetPegRadius(_rows);
        for (int r = 0; r < _rows; r++)
        {
            for (int col = -1; col <= r + 1; col++)
            {
                Vector2 pos = GetPegPosition(_rows, r, col) + new Vector2(shakeX, shakeY);
                bool isBoundary = col == -1 || col == r + 1;

                if (_pegFlashes.TryGetValue((r, col), out float flash) && flash > 0f)
                {
                    float ringR = pegRadius + (1f - flash) * 14f;
                    Raylib.DrawCircleV(pos, ringR, new Color(255, 215, 0, (int)(110 * flash)));
                    Raylib.DrawCircleV(pos, pegRadius + 1.5f * flash, Color.Gold);
                }
                else
                {
                    Color pegColor = isBoundary ? new Color(115, 130, 165, 220) : new Color(225, 232, 248, 255);
                    Raylib.DrawCircleV(pos + new Vector2(0, 1.5f), pegRadius, new Color(0, 0, 0, 110));
                    Raylib.DrawCircleV(pos, pegRadius, pegColor);
                }
            }
        }

        // Draw Multiplier Buckets & Track Hovered Bucket
        decimal[] mults = PlinkoTables.GetMultipliers(_rows, _risk);
        int hoveredBucket = -1;
        Vector2 mouse = GameCanvas.GetMousePosition();

        for (int b = 0; b < mults.Length; b++)
        {
            Rectangle rect = GetBucketRect(_rows, b);
            rect.X += shakeX;
            rect.Y += shakeY;

            if (Raylib.CheckCollisionPointRec(mouse, rect))
            {
                hoveredBucket = b;
            }

            Color baseColor = PlinkoTables.GetBucketColor(b, mults.Length);
            float flash = b < _bucketFlash.Length ? _bucketFlash[b] : 0f;

            // Shadow
            Raylib.DrawRectangleRounded(new Rectangle(rect.X, rect.Y + 4, rect.Width, rect.Height), 0.25f, 4, new Color(0, 0, 0, 140));

            // Fill
            Color fill = flash > 0f
                ? new Color(
                    (byte)Math.Min(255, baseColor.R + 120 * flash),
                    (byte)Math.Min(255, baseColor.G + 120 * flash),
                    (byte)Math.Min(255, baseColor.B + 120 * flash),
                    (byte)255)
                : baseColor;
            Raylib.DrawRectangleRounded(rect, 0.25f, 4, fill);

            // Top glossy rim
            Raylib.DrawRectangleRoundedLinesEx(rect, 0.25f, 4, hoveredBucket == b ? 2.5f : 1f,
                hoveredBucket == b ? Color.White : new Color(255, 255, 255, 70));

            string label = PlinkoTables.FormatMultiplier(mults[b]);
            float fontSize = _rows == 16 ? (label.Length >= 4 ? 9.5f : 11f) : (_rows == 12 ? 12.5f : 15f);
            Color textColor = b >= mults.Length / 2 - 1 && b <= mults.Length / 2 + 1 && _rows <= 12
                ? new Color(20, 20, 25, 255)
                : Color.White;
            UiFont.DrawCenteredInRect(label, rect, fontSize, textColor, isBold: true);
        }

        // Draw Balls & Glowing Trails
        float ballRadius = GetBallRadius(_rows);
        foreach (var ball in _balls)
        {
            for (int t = 0; t < ball.Trail.Count; t++)
            {
                float frac = (t + 1) / (float)ball.Trail.Count;
                Vector2 tp = ball.Trail[t] + new Vector2(shakeX, shakeY);
                Raylib.DrawCircleV(tp, ballRadius * (0.35f + 0.5f * frac),
                    new Color(ball.Color.R, ball.Color.G, ball.Color.B, (byte)(95 * frac)));
            }

            Vector2 bp = ball.Position + new Vector2(shakeX, shakeY);
            Raylib.DrawCircleV(bp, ballRadius + 3.5f, new Color(ball.Color.R, ball.Color.G, ball.Color.B, (byte)70));
            Raylib.DrawCircleV(bp, ballRadius, ball.Color);
            Raylib.DrawCircleLines((int)bp.X, (int)bp.Y, ballRadius, Color.White);
            Raylib.DrawCircleV(bp + new Vector2(-ballRadius * 0.3f, -ballRadius * 0.3f), ballRadius * 0.28f, new Color(255, 255, 255, 200));
        }

        // Draw Particles
        foreach (var p in _particles)
        {
            float alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            Raylib.DrawCircleV(p.Position + new Vector2(shakeX, shakeY), p.Size * alpha,
                new Color(p.Color.R, p.Color.G, p.Color.B, (byte)(255 * alpha)));
        }

        // Draw Floating Payout Popups
        foreach (var pop in _popups)
        {
            float alpha = Math.Clamp(pop.Life / pop.MaxLife, 0f, 1f);
            Color c = new(pop.Color.R, pop.Color.G, pop.Color.B, (byte)(255 * alpha));
            Color shadow = new(0, 0, 0, (int)(200 * alpha));
            float tw = UiFont.MeasureText(pop.Text, pop.FontSize, isBold: true).X;
            UiFont.DrawText(pop.Text, pop.Position.X - tw * 0.5f + 1.5f, pop.Position.Y + 1.5f, pop.FontSize, shadow, isBold: true);
            UiFont.DrawText(pop.Text, pop.Position.X - tw * 0.5f, pop.Position.Y, pop.FontSize, c, isBold: true);
        }

        // Bottom Info Bar (Bucket Hover Probability & Profit Preview)
        Rectangle infoBar = new(BoardRect.X + 27, BoardRect.Y + BoardRect.Height - 48, BoardRect.Width - 54, 36);
        Raylib.DrawRectangleRec(infoBar, new Color(11, 15, 26, 220));
        Raylib.DrawRectangleLinesEx(infoBar, 1.5f, new Color(255, 255, 255, 18));

        if (hoveredBucket >= 0)
        {
            decimal m = mults[hoveredBucket];
            double probPct = PlinkoTables.GetBucketProbability(_rows, hoveredBucket) * 100.0;
            decimal winAmt = decimal.Round(_bet * m, 2);
            string probText = probPct < 0.01 ? $"{probPct:0.0000}%" : $"{probPct:0.00}%";
            string info = $"SLOT #{hoveredBucket + 1}   |   PAYOUT: {PlinkoTables.FormatMultiplier(m)}   |   ODDS: {probText}   |   WIN: {winAmt:N2} COINS";
            UiFont.DrawCenteredInRect(info, infoBar, 18, Color.Gold, isBold: true);
        }
        else
        {
            decimal maxM = mults[0];
            string tip = $"MAX MULTIPLIER: {PlinkoTables.FormatMultiplier(maxM)}   |   HOVER ANY BUCKET FOR ODDS   |   99.0% RTP";
            UiFont.DrawCenteredInRect(tip, infoBar, 18, new Color(150, 165, 190, 255), isBold: false);
        }
    }

    private void DrawRightHistory()
    {
        Raylib.DrawRectangleRec(RightPanelRect, new Color(19, 25, 40, 255));
        Raylib.DrawRectangleLinesEx(RightPanelRect, 3, new Color(255, 255, 255, 22));

        UiFont.DrawText("RECENT DROPS", RightPanelRect.X + 24, RightPanelRect.Y + 24, 19.5f, Color.Gold, isBold: true);
        Raylib.DrawLine((int)RightPanelRect.X + 21, (int)RightPanelRect.Y + 57, (int)(RightPanelRect.X + RightPanelRect.Width - 21), (int)RightPanelRect.Y + 57, new Color(255, 255, 255, 20));

        if (_history.Count == 0)
        {
            UiFont.DrawCenteredInRect("Drop a ball to start!", new Rectangle(RightPanelRect.X, RightPanelRect.Y + 180, RightPanelRect.Width, 45), 19.5f, Color.Gray);
            return;
        }

        float y = RightPanelRect.Y + 72;
        for (int i = 0; i < _history.Count; i++)
        {
            var rec = _history[i];
            Rectangle rowRect = new(RightPanelRect.X + 18, y, RightPanelRect.Width - 36, 57);
            Raylib.DrawRectangleRec(rowRect, i == 0 ? new Color(28, 36, 58, 255) : new Color(14, 19, 32, 230));
            Raylib.DrawRectangleLinesEx(rowRect, 1.5f, i == 0 ? new Color(255, 215, 0, 100) : new Color(255, 255, 255, 14));

            // Multiplier Badge
            Rectangle badge = new(rowRect.X + 9, rowRect.Y + 9, 93, 39);
            Raylib.DrawRectangleRounded(badge, 0.3f, 4, rec.BucketColor);
            UiFont.DrawCenteredInRect(PlinkoTables.FormatMultiplier(rec.Multiplier), badge, 18, Color.White, isBold: true);

            // Payout & Net
            string payText = $"{rec.Payout:N2}";
            Color payColor = rec.Profit >= 0m ? Color.Lime : new Color(235, 120, 120, 255);
            float pw = UiFont.MeasureText(payText, 19.5f, isBold: true).X;
            UiFont.DrawText(payText, rowRect.X + rowRect.Width - pw - 12, rowRect.Y + 7.5f, 19.5f, payColor, isBold: true);

            string diffText = $"{(rec.Profit >= 0m ? "+" : "")}{rec.Profit:N2}";
            float dw = UiFont.MeasureText(diffText, 15).X;
            UiFont.DrawText(diffText, rowRect.X + rowRect.Width - dw - 12, rowRect.Y + 31.5f, 15, new Color(150, 162, 185, 255));

            y += 64.5f;
        }
    }

    private static void DrawStyledButton(Rectangle rect, string label, float fontSize, Color bg, Color textCol, Color? borderOverride = null)
    {
        bool hov = UiImageButton.IsHovered(rect);
        Color fill = hov
            ? new Color((byte)Math.Min(255, bg.R + 18), (byte)Math.Min(255, bg.G + 18), (byte)Math.Min(255, bg.B + 18), (byte)255)
            : bg;
        Raylib.DrawRectangleRec(rect, fill);
        Raylib.DrawRectangleLinesEx(rect, 1, hov ? Color.Gold : (borderOverride ?? new Color(255, 255, 255, 35)));
        UiFont.DrawCenteredInRect(label, rect, fontSize, hov ? Color.Gold : textCol, isBold: true);
    }

    private static void DrawTogglePill(Rectangle rect, string label, bool selected, Color accent, float fontSize, bool dimmed = false)
    {
        bool hov = !dimmed && UiImageButton.IsHovered(rect);
        Color bg = selected
            ? new Color((byte)(accent.R * 0.22f + 16), (byte)(accent.G * 0.22f + 20), (byte)(accent.B * 0.22f + 32), (byte)255)
            : new Color(24, 31, 48, dimmed ? 140 : 255);
        Color border = selected ? accent : (hov ? Color.Gold : new Color(255, 255, 255, dimmed ? 15 : 32));
        Color txt = selected ? accent : (dimmed ? new Color(110, 120, 140, 180) : (hov ? Color.White : Color.LightGray));

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, selected ? 2 : 1, border);
        UiFont.DrawCenteredInRect(label, rect, fontSize, txt, isBold: true);
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
        Raylib.DrawRectangleLinesEx(ModalBoxRect, 3, Color.Gold);

        bool closeHov = UiImageButton.IsHovered(ModalCloseBtnRect);
        Raylib.DrawRectangleRec(ModalCloseBtnRect, closeHov ? new Color(220, 38, 38, 200) : new Color(36, 44, 62, 255));
        Raylib.DrawRectangleLinesEx(ModalCloseBtnRect, 1.5f, closeHov ? Color.Red : Color.Gray);
        UiFont.DrawCenteredInRect("X", ModalCloseBtnRect, 21, Color.White, isBold: true);

        UiFont.DrawCenteredInRect("! OUT OF COINS !", new Rectangle(ModalBoxRect.X, ModalBoxRect.Y + 42, ModalBoxRect.Width, 51), 36, Color.Gold, isBold: true);
        UiFont.DrawCenteredInRect($"You need {_bet:N2} coins to drop a ball.", new Rectangle(ModalBoxRect.X + 30, ModalBoxRect.Y + 120, ModalBoxRect.Width - 60, 42), 27, Color.White);
        UiFont.DrawCenteredInRect($"Your current balance: {_currency.Balance:N2} coins", new Rectangle(ModalBoxRect.X + 30, ModalBoxRect.Y + 172, ModalBoxRect.Width - 60, 39), 24, Color.LightGray);
        UiFont.DrawCenteredInRect("Deposit coins now or lower your bet amount.", new Rectangle(ModalBoxRect.X + 30, ModalBoxRect.Y + 228, ModalBoxRect.Width - 60, 33), 21, new Color(160, 174, 192, 255));

        bool depHov = UiImageButton.IsHovered(ModalDepositBtnRect);
        Raylib.DrawRectangleRec(ModalDepositBtnRect, depHov ? new Color(40, 200, 100, 255) : new Color(34, 160, 80, 255));
        Raylib.DrawRectangleLinesEx(ModalDepositBtnRect, 3, depHov ? Color.Lime : Color.Gold);
        UiFont.DrawCenteredInRect("DEPOSIT MORE", ModalDepositBtnRect, 24, Color.White, isBold: true);

        bool lobHov = UiImageButton.IsHovered(ModalLobbyBtnRect);
        Raylib.DrawRectangleRec(ModalLobbyBtnRect, lobHov ? new Color(71, 85, 105, 255) : new Color(51, 65, 85, 255));
        Raylib.DrawRectangleLinesEx(ModalLobbyBtnRect, 1.5f, lobHov ? Color.White : Color.Gray);
        UiFont.DrawCenteredInRect("RETURN TO LOBBY", ModalLobbyBtnRect, 22.5f, Color.LightGray, isBold: true);
    }

    // ---------------- HELPER TYPES ----------------
    private sealed class PlinkoBall
    {
        public decimal Bet;
        public int Rows;
        public PlinkoRisk Risk;
        public int TargetBucket;
        public List<Vector2> Waypoints = [];
        public List<(int Row, int Col)> PegCoords = [];
        public int SegmentIndex;
        public float SegmentT;
        public float SegmentDuration;
        public Vector2 Position;
        public Color Color;
        public readonly List<Vector2> Trail = [];
    }

    private sealed class PlinkoParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Life;
        public float MaxLife;
        public float Size;

        public PlinkoParticle(Vector2 pos, Vector2 vel, Color color, float life, float size)
        {
            Position = pos;
            Velocity = vel;
            Color = color;
            Life = life;
            MaxLife = life;
            Size = size;
        }
    }

    private sealed class FloatingPayout
    {
        public Vector2 Position;
        public string Text;
        public Color Color;
        public float FontSize;
        public float Life = 1.15f;
        public float MaxLife = 1.15f;

        public FloatingPayout(Vector2 pos, string text, Color color, float fontSize)
        {
            Position = pos;
            Text = text;
            Color = color;
            FontSize = fontSize;
        }
    }

    private sealed record PlinkoDropRecord(decimal Multiplier, decimal Bet, decimal Payout, decimal Profit, Color BucketColor);
}
