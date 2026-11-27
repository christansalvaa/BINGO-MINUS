using Raylib_cs;

namespace BINGO_MINUS.Mines
{
    public sealed record LoadStep(string Label, Action Load);

    /// <summary>
    /// Loads every texture. Init() only loads what the loading screen itself needs;
    /// everything else is a LoadStep that LoadingScreen runs one at a time so the
    /// progress bar moves with real work.
    /// </summary>
    public static class AssetManager
    {
        public static Texture2D LoadingBg;
        public static Texture2D CardBack;   // the "?" card
        public static Texture2D Gem;
        public static Texture2D Bomb;
        public static Texture2D Smoke;      // grey cartoon smoke (bomb puff)
        public static Texture2D RedSmoke;   // red fog (2 second overlay)

        public static readonly List<LoadStep> Steps = new();

        public static void Init()
        {
            UI.LoadFonts(Resolve("Fonts/Poppins-Bold.ttf"));
            CardBack = LoadTex("card_back.png");
            Gem = LoadTex("gem.png");
            Bomb = LoadTex("bomb.png");
            Smoke = LoadTex("smoke.png");
            RedSmoke = LoadTex("smoke_red.png");
            AudioManager.LoadEffects();
            BoardManager.Build();
        }

        public static void Unload()
        {
            Unload(LoadingBg); Unload(CardBack); Unload(Gem);
            Unload(Bomb); Unload(Smoke); Unload(RedSmoke);
            UI.UnloadFonts();
        }

        private static void Unload(Texture2D t)
        {
            if (t.Id != 0) Raylib.UnloadTexture(t);
        }

        private static Texture2D LoadTex(string file)
        {
            string path = Resolve(file);
            if (!File.Exists(path)) return default;   // Id == 0 -> the game draws a fallback

            Texture2D t = Raylib.LoadTexture(path);
            Raylib.SetTextureFilter(t, TextureFilter.Bilinear);
            return t;
        }

        /// <summary>Looks next to the .exe first, then in the project folder.</summary>
        public static string Resolve(string relative)
        {
            string a = Path.Combine(AppContext.BaseDirectory, "scenes", "Minigames", "Mines", "assets", relative);
            if (File.Exists(a)) return a;
            string b = Path.Combine(Directory.GetCurrentDirectory(), "scenes", "Minigames", "Mines", "assets", relative);
            return File.Exists(b) ? b : a;
        }
    }
}
