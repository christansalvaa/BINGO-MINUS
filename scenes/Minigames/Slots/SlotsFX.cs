using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BINGO_MINUS;

internal enum FXType
{
    CandyShard,
    Sparkle,
    Coin,
    Smoke
}

internal struct SlotParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color ParticleColor;
    public float Scale;
    public float Alpha;
    public float Life;
    public float MaxLife;
    public float Rotation;
    public float AngularVelocity;
    public FXType Type;
}

internal sealed class SlotsFX
{
    private readonly List<SlotParticle> _particles = new(500);
    private readonly Random _rand = new();

    // Screen Shake (Trauma model)
    private float _trauma;
    public Vector2 ShakeOffset { get; private set; }

    // Textures
    private Texture2D _texSparkle;
    private Texture2D _texCoin;

    public void LoadAssets()
    {
        _texSparkle = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/sparkle.png");
        _texCoin = UiImageButton.LoadTexture("scenes/Minigames/Slots/assets/coin_particle.png");
    }

    public void AddTrauma(float amount)
    {
        _trauma = Math.Clamp(_trauma + amount, 0f, 1f);
    }

    public void TriggerExplosion(Vector2 pos, Color baseColor, int count = 25)
    {
        AddTrauma(0.35f);

        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float speed = (float)(_rand.Next(120, 500));
            Vector2 vel = new((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 100f);

            float life = (float)(0.4 + _rand.NextDouble() * 0.5);

            _particles.Add(new SlotParticle
            {
                Position = pos,
                Velocity = vel,
                ParticleColor = baseColor,
                Scale = (float)(0.6 + _rand.NextDouble() * 0.8),
                Alpha = 1f,
                Life = life,
                MaxLife = life,
                Rotation = (float)(_rand.NextDouble() * 360),
                AngularVelocity = (float)(_rand.Next(-500, 500)),
                Type = _rand.NextDouble() > 0.4 ? FXType.CandyShard : FXType.Sparkle
            });
        }
    }

    public void TriggerBombBlast(Vector2 pos)
    {
        AddTrauma(0.75f);

        // Huge shockwave particles
        for (int i = 0; i < 70; i++)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float speed = (float)(_rand.Next(200, 750));
            Vector2 vel = new((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);

            float life = (float)(0.5 + _rand.NextDouble() * 0.6);
            Color c = (_rand.Next(3)) switch
            {
                0 => Color.Gold,
                1 => Color.Orange,
                _ => Color.White
            };

            _particles.Add(new SlotParticle
            {
                Position = pos,
                Velocity = vel,
                ParticleColor = c,
                Scale = (float)(0.8 + _rand.NextDouble() * 1.2),
                Alpha = 1f,
                Life = life,
                MaxLife = life,
                Rotation = (float)(_rand.NextDouble() * 360),
                AngularVelocity = (float)(_rand.Next(-700, 700)),
                Type = FXType.Sparkle
            });
        }
    }

    public void SpawnCoinFountain(int count = 6)
    {
        for (int i = 0; i < count; i++)
        {
            float x = (float)(GameCanvas.Width * 0.2 + _rand.NextDouble() * GameCanvas.Width * 0.6);
            float y = GameCanvas.Height + 10f;
            float vx = (float)(_rand.Next(-180, 180));
            float vy = (float)(-_rand.Next(450, 750));

            float life = (float)(1.2 + _rand.NextDouble() * 0.8);

            _particles.Add(new SlotParticle
            {
                Position = new Vector2(x, y),
                Velocity = new Vector2(vx, vy),
                ParticleColor = Color.White,
                Scale = (float)(0.8 + _rand.NextDouble() * 0.5),
                Alpha = 1f,
                Life = life,
                MaxLife = life,
                Rotation = (float)(_rand.NextDouble() * 360),
                AngularVelocity = (float)(_rand.Next(-400, 400)),
                Type = FXType.Coin
            });
        }
    }

    public void Update(float deltaTime)
    {
        // Update screen shake
        if (_trauma > 0f)
        {
            float shakePower = _trauma * _trauma * 18f;
            float offsetX = (float)((_rand.NextDouble() * 2 - 1) * shakePower);
            float offsetY = (float)((_rand.NextDouble() * 2 - 1) * shakePower);
            ShakeOffset = new Vector2(offsetX, offsetY);

            _trauma = Math.Max(0f, _trauma - deltaTime * 1.8f);
        }
        else
        {
            ShakeOffset = Vector2.Zero;
        }

        // Update particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= deltaTime;

            if (p.Life <= 0f)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.Position += p.Velocity * deltaTime;

            // Gravity
            if (p.Type == FXType.Coin || p.Type == FXType.CandyShard)
            {
                p.Velocity.Y += 800f * deltaTime;
            }
            else
            {
                p.Velocity.Y += 200f * deltaTime;
                p.Velocity *= 0.95f; // drag
            }

            p.Rotation += p.AngularVelocity * deltaTime;
            p.Alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);

            _particles[i] = p;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            Color c = new(
                p.ParticleColor.R,
                p.ParticleColor.G,
                p.ParticleColor.B,
                (byte)(p.Alpha * 255));

            switch (p.Type)
            {
                case FXType.CandyShard:
                    // Draw diamond shard
                    Raylib.DrawRectanglePro(
                        new Rectangle(p.Position.X, p.Position.Y, 14 * p.Scale, 14 * p.Scale),
                        new Vector2(7 * p.Scale, 7 * p.Scale),
                        p.Rotation,
                        c);
                    break;

                case FXType.Sparkle:
                    if (_texSparkle.Id != 0)
                    {
                        float sz = 40 * p.Scale;
                        Rectangle src = new(0, 0, _texSparkle.Width, _texSparkle.Height);
                        Rectangle dest = new(p.Position.X, p.Position.Y, sz, sz);
                        Raylib.DrawTexturePro(_texSparkle, src, dest, new Vector2(sz / 2f, sz / 2f), p.Rotation, c);
                    }
                    else
                    {
                        Raylib.DrawCircleV(p.Position, 5 * p.Scale, c);
                    }
                    break;

                case FXType.Coin:
                    if (_texCoin.Id != 0)
                    {
                        float sz = 34 * p.Scale;
                        Rectangle src = new(0, 0, _texCoin.Width, _texCoin.Height);
                        Rectangle dest = new(p.Position.X, p.Position.Y, sz, sz);
                        Raylib.DrawTexturePro(_texCoin, src, dest, new Vector2(sz / 2f, sz / 2f), p.Rotation, Color.White);
                    }
                    else
                    {
                        Raylib.DrawCircleV(p.Position, 8 * p.Scale, Color.Gold);
                    }
                    break;
            }
        }
    }

    public void Clear()
    {
        _particles.Clear();
        _trauma = 0f;
        ShakeOffset = Vector2.Zero;
    }
}

