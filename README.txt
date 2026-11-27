MINES CASINO  (Raylib-cs, .NET 9)

Run:     dotnet run          (from this folder, where MinesCasino.csproj is)
   or    make run

Controls
  Click a card ........ flip it (gem = multiplier goes up, bomb = you lose the bet)
  SPACE ............... BET / CASHOUT
  R ................... Random Pick
  Click the bet box ... type an amount, ENTER to confirm

Sound (Assets/Sounds)
  game_starting.mp3 ... loops while loading, stops, then plays once on the main screen
  flip.mp3 ............ every card pick
  diamonds.mp3 ........ the card was a gem
  bomb.mp3 ............ the card was a bomb (cut off if a new round starts)

Files (old name -> new name)
  Game.cs          -> Program.cs         (main loop)
  Tile.cs          -> Card.cs
  TileManager.cs   -> BoardManager.cs    (5x5 board, Fisher-Yates shuffle, multiplier)
  GameManager.cs   -> GameManager.cs     (state machine: Loading, Idle, Playing, BombHit)
  Texturehandler   -> AssetManager.cs (loading) + Renderer.cs (drawing)
  new: AudioManager.cs, Effects.cs (smoke, shake, red fog), LoadingScreen.cs,
       Wallet.cs, Stats.cs, Layout.cs, Theme.cs, UI.cs

Assets/Fonts/Poppins-Bold.ttf is under the SIL Open Font License.
Play money only - no real gambling.
