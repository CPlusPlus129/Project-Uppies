using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;

namespace BlackjackGame
{
    public class BlackjackSystem : IBlackjackSystem
    {
        // Configuration
        private const int INITIAL_CHIPS = 1000;
        private const int MINIMUM_BET = 100;
        private const int MAXIMUM_BET = 250;
        private const int DEALER_STAND_VALUE = 17;

        // State
        private Random _rand;
        private Deck _deck;
        private Hand _playerHand;
        private Hand _dealerHand;
        private int _playerChips;
        private int _currentBet;
        private int _roundCount;
        private GameState _gameState;
        private RoundResult _lastRoundResult;
        private bool? _CheatMode; // true = player must win, false = player must lose, null = normal

        // Observables for reactive binding
        private readonly ReactiveProperty<int> _playerChipsProperty;
        private readonly ReactiveProperty<GameState> _gameStateProperty;
        private readonly Subject<Hand> _playerHandUpdated;
        private readonly Subject<Hand> _dealerHandUpdated;
        private readonly Subject<(RoundResult result, int chipsWon)> _roundEnded;

        // Public observables
        public ReadOnlyReactiveProperty<int> PlayerChips => _playerChipsProperty;
        public ReadOnlyReactiveProperty<GameState> CurrentGameState => _gameStateProperty;
        public Observable<Hand> OnPlayerHandUpdated => _playerHandUpdated;
        public Observable<Hand> OnDealerHandUpdated => _dealerHandUpdated;
        public Observable<(RoundResult result, int chipsWon)> OnRoundEnded => _roundEnded;

        // Public properties
        public IReadOnlyList<Card> PlayerCards => _playerHand.Cards;
        public IReadOnlyList<Card> DealerCards => _dealerHand.Cards;
        public int PlayerHandValue => _playerHand.GetValue();
        public int DealerHandValue => _dealerHand.GetValue();
        public int CurrentBet => _currentBet;
        public int MinimumBet => MINIMUM_BET;
        public int MaximumBet => MAXIMUM_BET;
        public int RoundCount => _roundCount;
        public bool IsCheatingForPlayer => _CheatMode == true;

        public BlackjackSystem()
        {
            _rand = new Random();
            _deck = new Deck();
            _playerHand = new Hand();
            _dealerHand = new Hand();
            _playerChips = INITIAL_CHIPS;
            _currentBet = 0;
            _gameState = GameState.WaitingForBet;
            _CheatMode = false; // Start with player must lose

            _playerChipsProperty = new ReactiveProperty<int>(_playerChips);
            _gameStateProperty = new ReactiveProperty<GameState>(_gameState);
            _playerHandUpdated = new Subject<Hand>();
            _dealerHandUpdated = new Subject<Hand>();
            _roundEnded = new Subject<(RoundResult, int)>();
        }

        public async UniTask Init()
        {
            ResetGame();

            await UniTask.CompletedTask;
        }

        public void ResetGame()
        {
            _rand = new Random();
            _playerChips = INITIAL_CHIPS;
            _playerChipsProperty.Value = _playerChips;
            _CheatMode = false;
            SetGameState(GameState.WaitingForBet);
        }

        // Toggle cheat mode (called when player accepts devil's deal)
        public void ActivateCheatMode(bool? playerMustWin)
        {
            _CheatMode = playerMustWin;
        }

        public bool CanPlaceBet(int amount)
        {
            return amount >= MINIMUM_BET && amount <= _playerChips && amount <= MAXIMUM_BET && _gameState == GameState.WaitingForBet;
        }

        public async UniTask PlaceBetAndStartRound(int betAmount)
        {
            if (!CanPlaceBet(betAmount))
                return;

            _currentBet = betAmount;
            _playerChips -= betAmount;
            _playerChipsProperty.Value = _playerChips;

            _playerHand.Clear();
            _dealerHand.Clear();
            _deck.Reset();

            SetGameState(GameState.DealingInitialCards);
            _roundCount++;

            await DealInitialCards();
        }

        private async UniTask DealInitialCards()
        {
            // Deal two cards to player
            await DealCardToPlayer();
            await UniTask.Delay(300);

            await DealCardToDealer();
            await UniTask.Delay(300);

            await DealCardToPlayer();
            await UniTask.Delay(300);

            await DealCardToDealer();
            await UniTask.Delay(300);

            // Check for blackjacks
            if (_playerHand.IsBlackjack())
            {
                SetGameState(GameState.RoundEnd);
                await ResolveRound();
            }
            else
            {
                SetGameState(GameState.PlayerTurn);
            }
        }

        private async UniTask DealCardToPlayer()
        {
            Card card = DrawCardForPlayer();
            _playerHand.AddCard(card);
            _playerHandUpdated.OnNext(_playerHand);
            await UniTask.Yield();
        }

        private async UniTask DealCardToDealer()
        {
            Card card = DrawCardForDealer();
            _dealerHand.AddCard(card);
            _dealerHandUpdated.OnNext(_dealerHand);
            await UniTask.Yield();
        }

        // Strategic card drawing based on cheat mode
        private Card DrawCardForPlayer()
        {
            if (_CheatMode.HasValue)
            {
                if (_CheatMode.Value)
                {
                    // Player must win: give favorable cards
                    return DrawCardToMakePlayerWin();
                }
                else
                {
                    // Player must lose: try to make them bust or get a weak hand
                    return DrawCardToMakePlayerLose();
                }
            }
            else
                return DrawCardNormally();
        }

        private Card DrawCardForDealer()
        {
            if (_CheatMode.HasValue)
            {
                if (_CheatMode.Value)
                {
                    // Player must win: dealer gets unfavorable cards
                    return DrawCardToMakeDealerLose();
                }
                else
                {
                    // Player must lose: dealer gets favorable cards
                    return DrawCardToMakeDealerWin();
                }
            }
            else
                return DrawCardNormally();
        }

        private Card DrawCardNormally()
        {
            return _deck.DrawCard();
        }

        private Card DrawCardToMakePlayerWin()
        {
            int playerValue = _playerHand.GetValue();

            if (playerValue <= 10)
            {
                // Give random cards
                return _deck.DrawCard();
            }
            else if (playerValue <= 20)
            {
                // Give cards that get player closer to 21
                int targetValue = _rand.Next(playerValue + 1, 22);
                int needed = targetValue - playerValue;
                if (needed <= 10 && needed >= 1)
                {
                    Rank targetRank = (Rank)needed;
                    Card card = _deck.DrawSpecificCard(targetRank);
                    if (card.Rank == targetRank) return card;
                }
            }

            return _deck.DrawCard();
        }

        private Card DrawCardToMakePlayerLose()
        {
            int playerValue = _playerHand.GetValue();

            if (playerValue <= 6)
            {
                // Give random cards
                return _deck.DrawCard();
            }
            else if (playerValue < 14)
            {
                // no Ace for <= 10
                bool smallerEqualTen = playerValue <= 10;
                var value = _rand.Next(smallerEqualTen ? 2 : 1, 17 - playerValue); //make player <= 16
                Rank targetRank = (Rank)value;
                Card card = _deck.DrawSpecificCard(targetRank);
                if (card.Rank == targetRank) return card;
            }
            else
            {
                // bust
                int neededToBust = 22 - playerValue;
                var value = _rand.Next(neededToBust, 11); //make player bust
                Rank targetRank = (Rank)value;
                Card card = _deck.DrawSpecificCard(targetRank);
                if (card.Rank == targetRank) return card;
            }

            return _deck.DrawCard();
        }

        private Card DrawCardToMakeDealerWin()
        {
            int dealerValue = _dealerHand.GetValue();

            if (dealerValue <= 10)
            {
                // Give random cards
                return _deck.DrawCard();
            }
            else if (dealerValue <= 16)
            {
                int playerValue = _playerHand.GetValue();
                // Give cards that get dealer to > player value but <= 21
                int targetValue = playerValue == 21 ? 21 : _rand.Next(playerValue + 1, 22);
                int needed = targetValue - dealerValue;
                if (needed <= 10 && needed >= 1)
                {
                    Rank targetRank = (Rank)needed;
                    Card card = _deck.DrawSpecificCard(targetRank);
                    if (card.Rank == targetRank) return card;
                }
            }

            return _deck.DrawCard();
        }

        private Card DrawCardToMakeDealerLose()
        {
            int dealerValue = _dealerHand.GetValue();

            if (dealerValue <= 6)
            {
                // Give random cards
                return _deck.DrawCard();
            }
            else if (dealerValue < 14)
            {
                // no Ace for <= 10
                bool smallerEqualTen = dealerValue <= 10;
                var value = _rand.Next(smallerEqualTen ? 2 : 1, 17 - dealerValue); //make dealer <= 16
                Rank targetRank = (Rank)value;
                Card card = _deck.DrawSpecificCard(targetRank);
                if (card.Rank == targetRank) return card;
            }
            else
            {
                // bust
                int neededToBust = 22 - dealerValue;
                var value = _rand.Next(neededToBust, 11); //make dealer bust
                Rank targetRank = (Rank)value;
                Card card = _deck.DrawSpecificCard(targetRank);
                if (card.Rank == targetRank) return card;
            }

            return _deck.DrawCard();
        }

        public async UniTask PlayerHit()
        {
            if (_gameState != GameState.PlayerTurn)
                return;

            await DealCardToPlayer();

            if (_playerHand.IsBusted())
            {
                SetGameState(GameState.RoundEnd);
                await ResolveRound();
            }
        }

        public async UniTask PlayerStand()
        {
            if (_gameState != GameState.PlayerTurn)
                return;

            SetGameState(GameState.DealerTurn);
            _dealerHandUpdated.OnNext(_dealerHand); //reveal dealer's hand
            await PlayDealerTurn();
        }

        private async UniTask PlayDealerTurn()
        {
            // Dealer draws until reaching 17 or busts
            while (_dealerHand.GetValue() < DEALER_STAND_VALUE)
            {
                await UniTask.Delay(500);
                await DealCardToDealer();

                if (_dealerHand.IsBusted())
                    break;
            }

            await UniTask.Delay(300);
            SetGameState(GameState.RoundEnd);
            await ResolveRound();
        }

        private async UniTask ResolveRound()
        {
            RoundResult result = DetermineWinner();
            int chipsWon = CalculateWinnings(result);

            _playerChips += chipsWon;
            _playerChipsProperty.Value = _playerChips;
            _lastRoundResult = result;

            // Emit round ended event
            _roundEnded.OnNext((result, chipsWon - _currentBet));

            await UniTask.Delay(100);

            // Check for game over
            if (_playerChips < MINIMUM_BET)
            {
                SetGameState(GameState.GameOver);
            }
            else
            {
                SetGameState(GameState.WaitingForBet);
            }
        }

        private RoundResult DetermineWinner()
        {
            // Player busted
            if (_playerHand.IsBusted())
                return RoundResult.DealerWin;

            // Dealer busted
            if (_dealerHand.IsBusted())
                return RoundResult.PlayerWin;

            // Player blackjack
            if (_playerHand.IsBlackjack() && !_dealerHand.IsBlackjack())
                return RoundResult.PlayerBlackjack;

            // Dealer blackjack
            if (_dealerHand.IsBlackjack() && !_playerHand.IsBlackjack())
                return RoundResult.DealerWin;

            // Compare values
            int playerValue = _playerHand.GetValue();
            int dealerValue = _dealerHand.GetValue();

            if (playerValue > dealerValue)
                return RoundResult.PlayerWin;
            else if (dealerValue > playerValue)
                return RoundResult.DealerWin;
            else
                return RoundResult.Push;
        }

        private int CalculateWinnings(RoundResult result)
        {
            switch (result)
            {
                case RoundResult.PlayerBlackjack:
                    return _currentBet + (int)(_currentBet * 1.5f); // 3:2 payout
                case RoundResult.PlayerWin:
                    return _currentBet * 2; // 1:1 payout
                case RoundResult.Push:
                    return _currentBet; // Return bet
                case RoundResult.DealerWin:
                    return 0; // Lose bet
                default:
                    return 0;
            }
        }

        private void SetGameState(GameState newState)
        {
            _gameState = newState;
            _gameStateProperty.Value = newState;
        }

        public void Dispose()
        {
            _playerChipsProperty?.Dispose();
            _gameStateProperty?.Dispose();
            _playerHandUpdated?.Dispose();
            _dealerHandUpdated?.Dispose();
            _roundEnded?.Dispose();
        }
    }
}