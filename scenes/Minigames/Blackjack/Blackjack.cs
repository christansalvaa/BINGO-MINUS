using System.Collections.Generic;
using Raylib_cs;

namespace BINGO_MINUS;

public class Blackjack : IMinigame
{
    private enum GameState { Betting, PlayerTurn, DealerTurn, GameOver }

    private readonly CurrencyManager _currency;
    public string Name => "Blackjack";
    public bool IsFinished { get; private set; }

    private Deck _deck = new();
    private Hand _dealerHand = new();
    private List<Hand> _playerHands = new();
    private List<decimal> _playerBets = new();
    
    private BlackjackUI _ui = new();
    private GameState _currentState;
    private int _currentHandIndex;
    private decimal _currentBet;
    private string _resultMessage = "";

    public Blackjack(CurrencyManager currency) { _currency = currency; }

    public void Start()
    {
        IsFinished = false;
        _ui.LoadAssets();
        ResetRound();
    }

    private void ResetRound()
    {
        _playerHands.Clear();
        _playerBets.Clear();
        _dealerHand.Clear();
        _currentHandIndex = 0;
        _currentBet = 0;
        _resultMessage = "";
        _currentState = GameState.Betting;
    }

    public void Update(float deltaTime)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || UiButton.WasClicked(BlackjackUI.BtnBack))
        {
            IsFinished = true;
            return;
        }

        switch (_currentState)
        {
            case GameState.Betting: UpdateBettingPhase(); break;
            case GameState.PlayerTurn: UpdatePlayerTurn(); break;
            case GameState.DealerTurn: ExecuteDealerTurn(); break;
            case GameState.GameOver:
                if (UiButton.WasClicked(BlackjackUI.BtnPlayAgain)) ResetRound();
                break;
        }
    }

    private void UpdateBettingPhase()
    {
        for (int i = 0; i < _ui.ChipValues.Length; i++)
        {
            if (UiButton.WasClicked(BlackjackUI.GetChipBounds(i, _ui.ChipValues.Length)) && 
                _currentBet + _ui.ChipValues[i] <= _currency.Balance)
            {
                _currentBet += _ui.ChipValues[i];
            }
        }

        if (_currentBet > 0)
        {
            if (UiButton.WasClicked(BlackjackUI.BtnDeal) && _currency.TrySpend(_currentBet))
                DealInitialCards();
            
            if (UiButton.WasClicked(BlackjackUI.BtnClear))
                _currentBet = 0;
        }
    }

    private void DealInitialCards()
    {
        _deck.ResetAndShuffle();
        
        Hand initialHand = new Hand();
        initialHand.AddCard(_deck.DrawCard());
        _dealerHand.AddCard(_deck.DrawCard());
        initialHand.AddCard(_deck.DrawCard());
        _dealerHand.AddCard(_deck.DrawCard());

        _playerHands.Add(initialHand);
        _playerBets.Add(_currentBet);
        _currentHandIndex = 0;

        if (initialHand.GetScore() == 21)
        {
            if (_dealerHand.GetScore() == 21)
            {
                _resultMessage = "Push";
                _currency.Add(_currentBet); 
            }
            else
            {
                _resultMessage = "Blackjack!";
                _currency.Add(_currentBet + (_currentBet * 1.5m)); 
            }
            _currentState = GameState.GameOver;
        }
        else
        {
            _currentState = GameState.PlayerTurn;
        }
    }

    private void UpdatePlayerTurn()
    {
        Hand currentHand = _playerHands[_currentHandIndex];
        decimal currentHandBet = _playerBets[_currentHandIndex];
        bool isInitialTwoCards = currentHand.Cards.Count == 2;

        if (UiButton.WasClicked(BlackjackUI.BtnHit)) 
        {
            currentHand.AddCard(_deck.DrawCard());
            if (currentHand.GetScore() >= 21) AdvanceHand();
        }
        else if (UiButton.WasClicked(BlackjackUI.BtnStand)) 
        {
            AdvanceHand();
        }
        else if (isInitialTwoCards && UiButton.WasClicked(BlackjackUI.BtnDouble)) 
        {
            if (_currency.TrySpend(currentHandBet))
            {
                _playerBets[_currentHandIndex] *= 2;
                currentHand.AddCard(_deck.DrawCard());
                AdvanceHand();
            }
        }
        else if (isInitialTwoCards && currentHand.Cards[0].GetValue() == currentHand.Cards[1].GetValue() && 
                 UiButton.WasClicked(BlackjackUI.BtnSplit)) 
        {
            if (_currency.TrySpend(currentHandBet))
            {
                Hand splitHand = new Hand();
                splitHand.AddCard(currentHand.Cards[1]);
                currentHand.Cards.RemoveAt(1);
                currentHand.AddCard(_deck.DrawCard());
                splitHand.AddCard(_deck.DrawCard());

                _playerHands.Insert(_currentHandIndex + 1, splitHand);
                _playerBets.Insert(_currentHandIndex + 1, currentHandBet);

                if (currentHand.Cards[0].Rank == Rank.Ace)
                {
                    AdvanceHand(); 
                    AdvanceHand(); 
                }
                else if (currentHand.GetScore() >= 21) AdvanceHand();
            }
        }
    }

    private void AdvanceHand()
    {
        _currentHandIndex++;
        if (_currentHandIndex >= _playerHands.Count) _currentState = GameState.DealerTurn;
    }

    private void ExecuteDealerTurn()
    {
        bool allBusted = true;
        foreach (var hand in _playerHands) if (hand.GetScore() <= 21) allBusted = false;

        if (!allBusted)
        {
            while (_dealerHand.GetScore() < 17) _dealerHand.AddCard(_deck.DrawCard());
        }

        DetermineWinner();
        _currentState = GameState.GameOver;
    }

    private void DetermineWinner()
    {
        int dealerScore = _dealerHand.GetScore();
        _resultMessage = dealerScore > 21 ? "Dealer Busts" : "Dealer Wins";

        bool playerWonAny = false;
        for (int i = 0; i < _playerHands.Count; i++)
        {
            int playerScore = _playerHands[i].GetScore();
            decimal bet = _playerBets[i];

            if (playerScore > 21) continue;
            
            if (dealerScore > 21 || playerScore > dealerScore)
            {
                _currency.Add(bet * 2); 
                playerWonAny = true;
            }
            else if (playerScore == dealerScore)
            {
                _currency.Add(bet); 
                _resultMessage = "Push";
            }
        }

        if (playerWonAny) _resultMessage = "You Won";
    }

    public void Draw()
    {
        _ui.DrawBackground();
        UiButton.Draw(BlackjackUI.BtnBack, "BACK", new Color(150, 50, 50, 255));
        _ui.DrawBalance(_currency.Balance);

        if (_currentState == GameState.Betting)
        {
            _ui.DrawBettingPhase(_currentBet, _currency.Balance);
            return;
        }

        bool isPlayerTurn = _currentState == GameState.PlayerTurn;
        
        int activeIndex = isPlayerTurn ? _currentHandIndex : -1;
        _ui.DrawHands(_dealerHand, _playerHands, _playerBets, isPlayerTurn, activeIndex);

        if (isPlayerTurn)
        {
            Hand current = _playerHands[_currentHandIndex];
            bool firstMove = current.Cards.Count == 2;
            bool canSplit = firstMove && current.Cards[0].GetValue() == current.Cards[1].GetValue();

            _ui.DrawPlayerControls(canSplit, firstMove);
        }
        else if (_currentState == GameState.GameOver)
        {
            _ui.DrawGameOver(_resultMessage);
        }
    }

    public void Stop()
    {
        _playerHands.Clear();
        _dealerHand.Clear();
        _ui.UnloadAssets();
    }
}
