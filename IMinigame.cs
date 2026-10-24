namespace BINGO_MINUS;

// Shared contract used by the game manager to run every minigame the same way.
// When adding a minigame, implement this interface instead of editing this file.
// Only edit it when changing requirements that should apply to every minigame.
public interface IMinigame
{
    string Name { get; }

    bool IsFinished { get; }

    void Start();

    void Update(float deltaTime);

    void Draw();

    void Stop();
}
