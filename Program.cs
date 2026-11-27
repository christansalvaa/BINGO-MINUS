using System.Globalization;
using Raylib_cs;

namespace MinesCasino
{
    class Program
    {
        static void Main(string[] args)
        {
            // Always print money as 12.34 (never 12,34), whatever the PC's region is.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
            Raylib.InitWindow(Theme.WindowWidth, Theme.WindowHeight, "MINES CASINO");
            Raylib.SetTargetFPS(60);

            AudioManager.Init();   // opens the sound device + loads the start music
            AssetManager.Init();   // loading-screen art + font; the rest loads behind the progress bar
            AudioManager.PlayLoadingMusic();   // background noise while loading

            while (!Raylib.WindowShouldClose())
            {
                GameManager.Update();
                Renderer.Draw();
            }

            AudioManager.Unload();
            AssetManager.Unload();
            Raylib.CloseWindow();
        }
    }
}
