using System;
using System.Globalization;
using Raylib_cs;

namespace BINGO_MINUS;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.FullscreenMode);
        Raylib.InitWindow(GameCanvas.Width, GameCanvas.Height, "BINGO MINUS");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);

        Raylib.InitAudioDevice();
        UiFont.Initialize();

        var currency = new CurrencyManager(1000m);
        var mainMenu = new MainMenuScene(currency);
        IMinigame? activeGame = null;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (activeGame != null)
            {
                activeGame.Update(dt);

                bool wantsDeposit = activeGame is IDepositRequester deposit && deposit.WantsDeposit;

                if (wantsDeposit)
                {
                    activeGame.Stop();
                    activeGame = null;
                    mainMenu.OpenFunds();
                }
                else if (activeGame.IsFinished)
                {
                    activeGame.Stop();
                    activeGame = null;
                }
            }
            else
            {
                MainMenuResult result = mainMenu.Update();
                if (result.Game != null)
                {
                    activeGame = result.Game;
                    activeGame.Start();
                }
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (activeGame != null)
            {
                activeGame.Draw();
            }
            else
            {
                mainMenu.Draw();
            }

            Raylib.EndDrawing();
        }

        activeGame?.Stop();
        UiFont.Unload();
        if (Raylib.IsAudioDeviceReady())
        {
            Raylib.CloseAudioDevice();
        }
        Raylib.CloseWindow();
    }
}
