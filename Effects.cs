using System.Numerics;
using Raylib_cs;

namespace MinesCasino
{
    /// <summary>
    /// Everything that happens when you flip a bomb:
    ///  - grey smoke puffs that spin, grow, drift and fade
    ///  - the whole UI shakes
    ///  - the red smoke overlay drifts across the screen for 2 seconds
    /// </summary>
    public static class Effects
    {
        public const float BombDuration = 2.0f;
        private const float SmokeBaseWidth = 420f;

        private sealed class Puff
        {
            public Vector2 Pos, Vel;
            public float Rot, RotSpeed;
            public float Scale0, Scale1;
            public float Delay, Life, Age;
            public bool Mirror;
        }

        private static readonly List<Puff> Puffs = new();
        private static readonly Random Rng = new();
        private static float _flash;

        public static float BombTimer = -1f;               // -1 = not playing
        public static bool BombActive => BombTimer >= 0f;
        public static Vector2 ShakeOffset;
        public static float ShakeRotation;                 // degrees

        public static void TriggerBomb(Vector2 origin)
        {
            BombTimer = 0f;
            _flash = 1f;
            Puffs.Clear();

            // two big swirls right on the card
            Puffs.Add(new Puff { Pos = origin, Vel = new Vector2(0, -30), Rot = -10, RotSpeed = -70, Scale0 = 0.30f, Scale1 = 1.15f, Delay = 0.05f, Life = 1.8f });
            Puffs.Add(new Puff { Pos = origin, Vel = new Vector2(0, -55), Rot = 25, RotSpeed = 95, Scale0 = 0.20f, Scale1 = 0.85f, Delay = 0.12f, Life = 1.6f, Mirror = true });

            // small puffs flying outwards
            for (int i = 0; i < 6; i++)
            {
                float ang = R(0f, MathF.PI * 2f);
                float speed = R(70f, 180f);
                Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                Puffs.Add(new Puff
                {
                    Pos = origin + dir * 10f,
                    Vel = dir * speed + new Vector2(0, -40),
                    Rot = R(0f, 360f),
                    RotSpeed = R(-120f, 120f),
                    Scale0 = 0.12f,
                    Scale1 = R(0.38f, 0.68f),
                    Delay = 0.08f + i * 0.06f,
                    Life = R(1.0f, 1.6f),
                    Mirror = i % 2 == 0
                });
            }
        }

        public static void Update(float dt)
        {
            _flash = MathF.Max(0f, _flash - dt * 4f);
            if (!BombActive) return;

            BombTimer += dt;
            foreach (Puff p in Puffs) p.Age += dt;

            // shake: a few sine waves at different speeds, fading out over the 2 seconds
            float t = BombTimer;
            float env = MathF.Pow(MathF.Max(0f, 1f - t / BombDuration), 1.4f);
            float amp = 18f * env;
            ShakeOffset = new Vector2(
                MathF.Sin(t * 63f) * amp + MathF.Sin(t * 29f + 1.7f) * amp * 0.5f,
                MathF.Cos(t * 57f) * amp + MathF.Sin(t * 37f + 0.4f) * amp * 0.5f);
            ShakeRotation = MathF.Sin(t * 47f) * 1.6f * env;

            if (BombTimer >= BombDuration)
            {
                BombTimer = -1f;
                Puffs.Clear();
                ShakeOffset = Vector2.Zero;
                ShakeRotation = 0f;
            }
        }

        /// <summary>Grey smoke. Drawn inside the shaking camera, on top of the board.</summary>
        public static void DrawSmoke()
        {
            Texture2D tex = AssetManager.Smoke;
            if (!BombActive || tex.Id == 0) return;

            foreach (Puff p in Puffs)
            {
                float age = p.Age - p.Delay;
                if (age <= 0f || age >= p.Life) continue;

                float u = age / p.Life;
                float ease = 1f - (1f - u) * (1f - u);
                float scale = p.Scale0 + (p.Scale1 - p.Scale0) * ease;
                float alpha = MathF.Min(age / 0.12f, 1f) * (1f - SmoothStep(0.45f, 1f, u)) * 0.95f;

                float w = SmokeBaseWidth * scale;
                float h = w * tex.Height / tex.Width;
                Vector2 pos = p.Pos + p.Vel * age;

                Rectangle src = new Rectangle(0, 0, p.Mirror ? -tex.Width : tex.Width, tex.Height);
                Rectangle dst = new Rectangle(pos.X, pos.Y, w, h);
                Raylib.DrawTexturePro(tex, src, dst, new Vector2(w / 2f, h / 2f), p.Rot + p.RotSpeed * age, Theme.Alpha(Color.White, alpha));
            }
        }

        /// <summary>Red smoke over the whole screen for 2 seconds (additive, so black = invisible).</summary>
        public static void DrawRedSmoke(int w, int h)
        {
            if (!BombActive && _flash <= 0f) return;

            float t = MathF.Max(0f, BombTimer);
            float a = 0f;
            if (BombActive)
                a = Math.Clamp(MathF.Min(t / 0.2f, 1f) * MathF.Min((BombDuration - t) / 0.7f, 1f), 0f, 1f);

            Texture2D tex = AssetManager.RedSmoke;
            Raylib.BeginBlendMode(BlendMode.Additive);

            if (tex.Id != 0)
            {
                DrawRedLayer(tex, w, h, t, a, 1.35f, 60f, -45f, false, 0f);
                DrawRedLayer(tex, w, h, t, a * 0.8f, 1.75f, -90f, -25f, true, 1.7f);
            }
            else
            {
                Raylib.DrawRectangleGradientV(0, 0, w, h, new Color(0, 0, 0, 0), Theme.Alpha(Theme.Red, a * 0.6f));
            }

            if (_flash > 0f)
                Raylib.DrawRectangle(0, 0, w, h, Theme.Alpha(new Color(255, 40, 40, 255), _flash * 0.35f));

            Raylib.EndBlendMode();
        }

        private static void DrawRedLayer(Texture2D tex, int w, int h, float t, float alpha,
                                         float zoom, float driftX, float driftY, bool mirror, float phase)
        {
            if (alpha <= 0f) return;

            float cover = MathF.Max(w / (float)tex.Width, h / (float)tex.Height) * zoom;
            float dw = tex.Width * cover;
            float dh = tex.Height * cover;

            // the smoke slides sideways and rises while it fades
            float cx = w / 2f + MathF.Sin(t * 1.4f + phase) * driftX;
            float cy = h / 2f + MathF.Cos(t * 1.1f + phase) * 20f + t * driftY;

            Rectangle src = new Rectangle(0, 0, mirror ? -tex.Width : tex.Width, tex.Height);
            Rectangle dst = new Rectangle(cx - dw / 2f, cy - dh / 2f, dw, dh);
            Raylib.DrawTexturePro(tex, src, dst, Vector2.Zero, 0f, Theme.Alpha(Color.White, alpha));
        }

        private static float R(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
