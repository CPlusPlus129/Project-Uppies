using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;

namespace BlackjackGame
{
    /// <summary>
    /// Interface for the Blackjack game system
    /// Exposes all necessary functionality for UI/View layer
    /// </summary>
    public interface IBlackjackSystem : IGameService
    {
        // ========== Observables for Reactive Binding ==========

        /// <summary>
        /// Observable for player's current chip count
        /// </summary>
        ReadOnlyReactiveProperty<int> PlayerChips { get; }

        /// <summary>
        /// Observable for current game state
        /// </summary>
        ReadOnlyReactiveProperty<GameState> CurrentGameState { get; }

        /// <summary>
        /// Event triggered when player's hand is updated
        /// </summary>
        Observable<Hand> OnPlayerHandUpdated { get; }

        /// <summary>
        /// Event triggered when dealer's hand is updated
        /// </summary>
        Observable<Hand> OnDealerHandUpdated { get; }

        /// <summary>
        /// Event triggered when a round ends with result and chips won/lost
        /// </summary>
        Observable<(RoundResult result, int chipsWon)> OnRoundEnded { get; }

        // ========== Game State Properties ==========

        /// <summary>
        /// Get current player's cards
        /// </summary>
        IReadOnlyList<Card> PlayerCards { get; }

        /// <summary>
        /// Get current dealer's cards
        /// </summary>
        IReadOnlyList<Card> DealerCards { get; }

        /// <summary>
        /// Get player's hand value
        /// </summary>
        int PlayerHandValue { get; }

        /// <summary>
        /// Get dealer's hand value
        /// </summary>
        int DealerHandValue { get; }

        /// <summary>
        /// Get current bet amount
        /// </summary>
        int CurrentBet { get; }

        /// <summary>
        /// Gets the minimum allowable bet for the game.
        /// </summary>
        int MinimumBet { get; }

        /// <summary>
        /// Gets the maximum allowable bet amount.
        /// </summary>
        int MaximumBet { get; }

        /// <summary>
        /// Gets the current round count.
        /// </summary>
        int RoundCount { get; }

        /// <summary>
        /// Check if cheat mode is active (player will win after accepting devil's deal)
        /// </summary>
        bool IsCheatingForPlayer { get; }

        // ========== Game Actions ==========

        /// <summary>
        /// Check if a bet amount is valid
        /// </summary>
        /// <param name="amount">The amount to bet</param>
        /// <returns>True if bet is valid</returns>
        bool CanPlaceBet(int amount);

        /// <summary>
        /// Place a bet and start a new round
        /// </summary>
        /// <param name="betAmount">Amount to bet</param>
        UniTask PlaceBetAndStartRound(int betAmount);

        /// <summary>
        /// Player chooses to hit (draw another card)
        /// </summary>
        UniTask PlayerHit();

        /// <summary>
        /// Player chooses to stand (end their turn)
        /// </summary>
        UniTask PlayerStand();

        /// <summary>
        /// Reset the game to initial state
        /// </summary>
        void ResetGame();

        /// <summary>
        /// Activate cheat mode (devil's deal mechanic)
        /// </summary>
        /// <param name="playerMustWin">True = player will win, False = player will lose</param>
        void ActivateCheatMode(bool? playerMustWin);

        /// <summary>
        /// Add chips to the player's balance
        /// </summary>
        /// <param name="amount">Amount to add (can be negative)</param>
        void AddChips(int amount);
    }
}