using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    /// <summary>
    /// All sound. game_starting.mp3 is a streamed "Music" (loops while loading, plays once on the
    /// main screen); flip / diamonds / bomb are short "Sound" effects.
    /// If there is no audio device or a file is missing, every call quietly does nothing.
    /// </summary>
    public static class AudioManager
    {
        private const float MusicVolume = 0.6f;

        private static bool _enabled;
        private static Music _music;
        private static bool _musicActive;
        private static Sound _flip, _bomb, _diamond;

        private static bool HasMusic => _enabled && _music.FrameCount > 0;

        public static void Init()
        {
            _enabled = Raylib.IsAudioDeviceReady();
            if (!_enabled) return;

            string path = AssetManager.Resolve("Sounds/game_starting.mp3");
            if (File.Exists(path))
            {
                _music = Raylib.LoadMusicStream(path);
                Raylib.SetMusicVolume(_music, MusicVolume);
            }
        }

        /// <summary>Loaded as a step behind the progress bar.</summary>
        public static void LoadEffects()
        {
            if (!_enabled) return;
            _flip = LoadSfx("flip.mp3", 0.7f);
            _diamond = LoadSfx("diamonds.mp3", 0.9f);
            _bomb = LoadSfx("bomb.mp3", 1.0f);
        }

        private static Sound LoadSfx(string file, float volume)
        {
            string path = AssetManager.Resolve("Sounds/" + file);
            if (!File.Exists(path)) return default;

            Sound s = Raylib.LoadSound(path);
            Raylib.SetSoundVolume(s, volume);
            return s;
        }

        // ------------------------------------------------------------- music
        /// <summary>Background noise while the game loads (loops).</summary>
        public static void PlayLoadingMusic()
        {
            if (!HasMusic) return;
            _music.Looping = true;
            Raylib.PlayMusicStream(_music);
            _musicActive = true;
        }

        public static void StopMusic()
        {
            if (!HasMusic) return;
            Raylib.StopMusicStream(_music);
            _musicActive = false;
        }

        public static void PlayMenuMusicOnce()
        {
            if (!HasMusic) return;
            _music.Looping = true;
            if (!Raylib.IsMusicStreamPlaying(_music))
            {
                Raylib.PlayMusicStream(_music);
            }
            _musicActive = true;
        }
        /// <summary>Streamed music must be fed every frame.</summary>
        public static void Update()
        {
            if (HasMusic && _musicActive) Raylib.UpdateMusicStream(_music);
        }

        // ----------------------------------------------------------- effects
        public static void PlayFlip()    => Play(_flip);
        public static void PlayDiamond() => Play(_diamond);
        public static void PlayBomb()    => Play(_bomb);

        /// <summary>The bomb clip is ~10 s long, so cut it when a new round starts.</summary>
        public static void StopBomb()
        {
            if (_enabled && _bomb.FrameCount > 0) Raylib.StopSound(_bomb);
        }

        private static void Play(Sound s)
        {
            if (_enabled && s.FrameCount > 0) Raylib.PlaySound(s);
        }

        public static void Unload()
        {
            if (!_enabled) return;

            StopMusic();
            StopBomb();
            if (_flip.FrameCount > 0) Raylib.UnloadSound(_flip);
            if (_diamond.FrameCount > 0) Raylib.UnloadSound(_diamond);
            if (_bomb.FrameCount > 0) Raylib.UnloadSound(_bomb);
            if (_music.FrameCount > 0) Raylib.UnloadMusicStream(_music);

            _flip = default;
            _diamond = default;
            _bomb = default;
            _music = default;
            _musicActive = false;
            _enabled = false;
        }
    }
}
