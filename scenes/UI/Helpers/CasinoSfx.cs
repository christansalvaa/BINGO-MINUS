// Small shared sound helper for betting games.
// It reuses the existing sound effects in Slots/assets/audio, so no new audio files are required.
// If a file or the audio device is missing, every call silently does nothing (the game never crashes over sound).

using Raylib_cs;

namespace BINGO_MINUS;

internal enum Sfx
{
    Click,
    Spin,
    Win,
    BigWin,
    Error,
    Pop
}

internal sealed class CasinoSfx
{
    private const string AudioDir = "scenes/Minigames/Slots/assets/audio/";

    private readonly Dictionary<Sfx, Sound> _sounds = new();

    public void Load()
    {
        if (_sounds.Count > 0 || !Raylib.IsAudioDeviceReady()) return;

        TryLoad(Sfx.Click, "sfx_ui_click.wav");
        TryLoad(Sfx.Spin, "sfx_spin.wav");
        TryLoad(Sfx.Win, "sfx_win_chime.wav");
        TryLoad(Sfx.BigWin, "sfx_bigwin.wav");
        TryLoad(Sfx.Error, "sfx_error.wav");
        TryLoad(Sfx.Pop, "sfx_match_pop.wav");
    }

    public void Play(Sfx id, float volume = 1f, float pitch = 1f)
    {
        if (!_sounds.TryGetValue(id, out Sound sound)) return;

        Raylib.SetSoundVolume(sound, volume);
        Raylib.SetSoundPitch(sound, pitch);
        Raylib.PlaySound(sound);
    }

    public void Stop(Sfx id)
    {
        if (_sounds.TryGetValue(id, out Sound sound) && Raylib.IsSoundPlaying(sound))
        {
            Raylib.StopSound(sound);
        }
    }

    public void Unload()
    {
        foreach (Sound sound in _sounds.Values)
        {
            Raylib.UnloadSound(sound);
        }

        _sounds.Clear();
    }

    private void TryLoad(Sfx id, string fileName)
    {
        string? path = ResolvePath(AudioDir + fileName);
        if (path is null) return;

        try
        {
            _sounds[id] = Raylib.LoadSound(path);
        }
        catch
        {
            // Ignore: this effect just stays silent.
        }
    }

    private static string? ResolvePath(string relativePath)
    {
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        if (File.Exists(p)) return p;

        p = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (File.Exists(p)) return p;

        return File.Exists(relativePath) ? relativePath : null;
    }
}
