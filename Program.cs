// main program
// edit only when adding minigames

using Raylib_cs;

namespace BINGO_MINUS;

internal static class Program
{
    private static void Main()
    {
        int monitor = Raylib.GetCurrentMonitor();
        int screenWidth = Raylib.GetMonitorWidth(monitor);
        int screenHeight = Raylib.GetMonitorHeight(monitor);

        Raylib.SetConfigFlags(ConfigFlags.FullscreenMode);
        Raylib.InitWindow(screenWidth, screenHeight, "BINGO MINUS");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        // 1. THIS STOPS THE GAME FROM CLOSING WHEN YOU PRESS ESCAPE
        Raylib.SetExitKey(KeyboardKey.Null);

        var currency = new CurrencyManager(startingBalance: 100m);
        var mainMenu = new MainMenuScene(currency);
        var paywall = new PaywallScene(currency);

        IMinigame? minigame = null;
        Screen currentScreen = Screen.MainMenu;

        try
        {
            while (!Raylib.WindowShouldClose())
            {
                // 2. F11 TOGGLE FOR WINDOWED / FULLSCREEN MODE
                if (Raylib.IsKeyPressed(KeyboardKey.F11))
                {
                    Raylib.ToggleFullscreen();
                }

                float deltaTime = Raylib.GetFrameTime();

                switch (currentScreen)
                {
                    case Screen.MainMenu:
                        MainMenuAction action = mainMenu.Update();
                        HandleMainMenuAction(action, ref currentScreen, ref minigame, paywall, currency);
                        break;

                    case Screen.Paywall:
                        if (paywall.Update())
                        {
                            currentScreen = Screen.MainMenu;
                        }
                        break;

                    case Screen.Minigame when minigame is not null:
                        minigame.Update(deltaTime);

                        if (minigame.IsFinished)
                        {
                            bool goToDeposit = (minigame as CrashMinigame)?.WantsDeposit == true || (minigame as SlotsMinigame)?.WantsDeposit == true;
                            minigame.Stop();
                            minigame = null;

                            if (goToDeposit)
                            {
                                paywall.Start();
                                currentScreen = Screen.Paywall;
                            }
                            else
                            {
                                currentScreen = Screen.MainMenu;
                            }
                        }
                        break;
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(14, 19, 34, 255));
                GameCanvas.Begin();

                switch (currentScreen)
                {
                    case Screen.MainMenu:
                        mainMenu.Draw();
                        break;
                    case Screen.Paywall:
                        paywall.Draw();
                        break;
                    case Screen.Minigame:
                        minigame?.Draw();
                        break;
                }

                GameCanvas.End();
                Raylib.EndDrawing();
            }
        }
        finally
        {
            minigame?.Stop();
            UiFont.Unload();
            UiImageButton.UnloadAll();
            if (Raylib.IsAudioDeviceReady())
            {
                Raylib.CloseAudioDevice();
            }
            Raylib.CloseWindow();
        }
    }

    private static void HandleMainMenuAction(
        MainMenuAction action,
        ref Screen currentScreen,
        ref IMinigame? minigame,
        PaywallScene paywall,
        CurrencyManager currency)
    {
        switch (action)
        {
            case MainMenuAction.PlayCrash:
                minigame = new CrashMinigame(currency);
                minigame.Start();
                currentScreen = Screen.Minigame;
                break;

            case MainMenuAction.PlaySlots:
                minigame = new SlotsMinigame(currency);
                minigame.Start();
                currentScreen = Screen.Minigame;
                break;
                
            case MainMenuAction.PlayBlackjack:
                minigame = new Blackjack(currency);
                minigame.Start();
                currentScreen = Screen.Minigame;
                break;

            case MainMenuAction.PlayCrossroads:
                minigame = new Crossyroad(currency);
                minigame.Start();
                currentScreen = Screen.Minigame;
                break;

            case MainMenuAction.OpenPaywall:
                paywall.Start();
                currentScreen = Screen.Paywall;
                break;
        }
    }

    private enum Screen
    {
        MainMenu,
        Paywall,
        Minigame
    }
}
