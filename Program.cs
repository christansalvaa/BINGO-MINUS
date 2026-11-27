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
        Raylib.InitWindow(0, 0, "BINGO MINUS");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);

        Raylib.InitAudioDevice();
        UiFont.Initialize();

        var currency = new CurrencyManager(1000m);
        var mainMenu = new MainMenuScene(currency);
        var paywall = new PaywallScene(currency);
        IMinigame? activeGame = null;
        bool inPaywall = false;

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
                    inPaywall = true;
                    paywall.Start();
                }
                else if (activeGame.IsFinished)
                {
                    activeGame.Stop();
                    activeGame = null;
                }
            }
            else if (inPaywall)
            {
                if (paywall.Update())
                {
                    inPaywall = false;
                }
            }
            else
            {
                MainMenuResult result = mainMenu.Update();
                if (result.OpenPaywall)
                {
                    inPaywall = true;
                    paywall.Start();
                }
                else if (result.Game != null)
                {
                    activeGame = result.Game;
                    activeGame.Start();
                }
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            GameCanvas.Begin();

            if (activeGame != null)
            {
                activeGame.Draw();
            }
            else if (inPaywall)
            {
                paywall.Draw();
            }
            else
            {
                mainMenu.Draw();
            }

            GameCanvas.End();
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
