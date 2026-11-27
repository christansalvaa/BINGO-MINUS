using BINGO_MINUS.Mines;

namespace BINGO_MINUS;

public sealed class MinesMinigame : IMinigame
{
    private readonly CurrencyManager _currency;

    public string Name => "Mines";
    public bool IsFinished { get; private set; }

    public MinesMinigame(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        IsFinished = false;
        AudioManager.Init();
        AssetManager.Init();
        AudioManager.PlayMenuMusicOnce();
        GameManager.Start(_currency);
    }

    public void Update(float deltaTime)
    {
        GameManager.Update(deltaTime);
    }

    public void Draw()
    {
        Renderer.Draw();

        if (BingoPlusHeader.Draw("MINES", _currency))
        {
            IsFinished = true;
        }
    }

    public void Stop()
    {
        AudioManager.Unload();
        AssetManager.Unload();
    }
}
