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

                bool wantsDeposit =
                    (activeGame is CrashMinigame crash && crash.WantsDeposit) ||
                    (activeGame is SlotsMinigame slots && slots.WantsDeposit);

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
                MainMenuAction action = mainMenu.Update();
                switch (action)
                {
                    case MainMenuAction.PlayCrash:
                        activeGame = new CrashMinigame(currency);
                        activeGame.Start();
                        break;
                    case MainMenuAction.PlaySlots:
                        activeGame = new SlotsMinigame(currency);
                        activeGame.Start();
                        break;
                    case MainMenuAction.PlayBlackjack:
                        activeGame = new Blackjack(currency);
                        activeGame.Start();
                        break;
                    case MainMenuAction.PlayCrossroads:
                        activeGame = new Crossyroad(currency);
                        activeGame.Start();
                        break;
                    case MainMenuAction.PlayMines:
                        activeGame = new MinesMinigame(currency);
                        activeGame.Start();
                        break;
                    case MainMenuAction.OpenPaywall:
                        inPaywall = true;
                        paywall.Start();
                        break;
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
