using System;
using Raylib_cs;

namespace BINGO_MINUS;

public class Blackjack : IMinigame
{
    private enum GameState { Betting, PlayerTurn, DealerTurn, GameOver }

    private readonly CurrencyManager _currency;
    public string Name => "Blackjack";
    public bool IsFinished { get; private set; }

    private Deck _deck = null!;
    private Hand _playerHand = null!;
    private Hand _dealerHand = null!;
    
    private GameState _currentState;
    private decimal _currentBet = 10m;
    private string _resultMessage = "";

    public Blackjack(CurrencyManager currency)
    {
        _currency = currency;
    }

    public void Start()
    {
        IsFinished = false;
        _deck = new Deck();
        _playerHand = new Hand();
        _dealerHand = new Hand();
        ResetRound();
    }

    private void ResetRound()
    {
        _playerHand.Clear();
        _dealerHand.Clear();
        _currentState = GameState.Betting;
        _resultMessage = "";
    }

    public void Update(float deltaTime)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            IsFinished = true;
            return;
        }

        switch (_currentState)
        {
            case GameState.Betting:
                HandleBettingInput();
                break;
            case GameState.PlayerTurn:
                HandlePlayerTurn();
                break;
            case GameState.DealerTurn:
                ExecuteDealerTurn();
                break;
            case GameState.GameOver:
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    ResetRound();
                }
                break;
        }
    }

    private void HandleBettingInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) && _currentBet + 10 <= _currency.Balance) _currentBet += 10;
        if (Raylib.IsKeyPressed(KeyboardKey.Down) && _currentBet - 10 >= 10) _currentBet -= 10;

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (_currency.TrySpend(_currentBet))
            {
                DealInitialCards();
            }
            else
            {
                _resultMessage = "Not enough coins!";
            }
        }
    }

    private void DealInitialCards()
    {
        _deck.ResetAndShuffle();
        _playerHand.AddCard(_deck.DrawCard());
        _dealerHand.AddCard(_deck.DrawCard());
        _playerHand.AddCard(_deck.DrawCard());
        _dealerHand.AddCard(_deck.DrawCard());

        // Check for immediate Natural Blackjack
        if (_playerHand.GetScore() == 21)
        {
            if (_dealerHand.GetScore() == 21)
            {
                _resultMessage = "Push! Both have Blackjack.";
                _currency.Add(_currentBet); // Refund
            }
            else
            {
                _resultMessage = "Blackjack! You win 3:2.";
                _currency.Add(_currentBet + (_currentBet * 1.5m)); 
            }
            _currentState = GameState.GameOver;
        }
        else
        {
            _currentState = GameState.PlayerTurn;
        }
    }

    private void HandlePlayerTurn()
    {
        // Hit
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            _playerHand.AddCard(_deck.DrawCard());
            if (_playerHand.GetScore() > 21)
            {
                _resultMessage = "Bust! You lose.";
                _currentState = GameState.GameOver;
            }
        }
        // Stand
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            _currentState = GameState.DealerTurn;
        }
    }

    private void ExecuteDealerTurn()
    {
        // Standard casino rule: Dealer must hit on 16 and stand on 17
        while (_dealerHand.GetScore() < 17)
        {
            _dealerHand.AddCard(_deck.DrawCard());
        }

        DetermineWinner();
        _currentState = GameState.GameOver;
    }

    private void DetermineWinner()
    {
        int playerScore = _playerHand.GetScore();
        int dealerScore = _dealerHand.GetScore();

        if (dealerScore > 21)
        {
            _resultMessage = "Dealer busts! You win.";
            _currency.Add(_currentBet * 2);
        }
        else if (playerScore > dealerScore)
        {
            _resultMessage = "You win!";
            _currency.Add(_currentBet * 2);
        }
        else if (playerScore < dealerScore)
        {
            _resultMessage = "Dealer wins.";
        }
        else
        {
            _resultMessage = "Push! It's a tie.";
            _currency.Add(_currentBet); // Refund the bet
        }
    }

    public void Draw()
    {
        if (_currentState == GameState.Betting)
        {
            Raylib.DrawText("PLACE YOUR BET", 300, 200, 30, Color.RayWhite);
            Raylib.DrawText($"Bet: {_currentBet:0.00}", 330, 250, 25, Color.Gold);
            Raylib.DrawText("UP/DOWN to adjust, ENTER to deal", 220, 300, 20, Color.LightGray);
            return;
        }

        // Draw Dealer Info
        // Hide dealer's second card if it is the player's turn
        bool hideDealerCard = _currentState == GameState.PlayerTurn;
        int displayDealerScore = hideDealerCard ? _dealerHand.Cards[0].GetValue() : _dealerHand.GetScore();
        
        Raylib.DrawText($"Dealer Score: {displayDealerScore}", 50, 50, 20, Color.RayWhite);
        Raylib.DrawText(_dealerHand.Cards[0].ToString(), 50, 80, 20, Color.White);
        
        if (hideDealerCard)
        {
            Raylib.DrawText("[Hidden Card]", 50, 100, 20, Color.Gray);
        }
        else
        {
            for (int i = 1; i < _dealerHand.Cards.Count; i++)
            {
                Raylib.DrawText(_dealerHand.Cards[i].ToString(), 50, 80 + (i * 20), 20, Color.White);
            }
        }

        // Draw Player Info
        Raylib.DrawText($"Player Score: {_playerHand.GetScore()}", 50, 300, 20, Color.RayWhite);
        for (int i = 0; i < _playerHand.Cards.Count; i++)
        {
            Raylib.DrawText(_playerHand.Cards[i].ToString(), 50, 330 + (i * 20), 20, Color.White);
        }

        // Draw Controls and Results
        if (_currentState == GameState.PlayerTurn)
        {
            Raylib.DrawText("SPACE to Hit, ENTER to Stand", 250, 500, 20, Color.LightGray);
        }
        else if (_currentState == GameState.GameOver)
        {
            Raylib.DrawText(_resultMessage, 300, 250, 30, Color.Gold);
            Raylib.DrawText("Press ENTER to play again", 270, 500, 20, Color.LightGray);
        }
    }

    public void Stop()
    {
        _playerHand.Clear();
        _dealerHand.Clear();
    }
}