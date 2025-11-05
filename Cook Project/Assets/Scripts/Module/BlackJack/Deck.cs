using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackjackGame
{
    public class Deck
    {
        private List<Card> _cards;
        private Random _random;

        public int CardsRemaining => _cards.Count;

        public Deck(int seed = -1)
        {
            _random = seed == -1 ? new Random() : new Random(seed);
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            _cards = new List<Card>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card(suit, rank));
                }
            }
        }

        public void Shuffle()
        {
            // Fisher-Yates shuffle
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card DrawCard()
        {
            if (_cards.Count == 0)
            {
                InitializeDeck();
                Shuffle();
            }

            Card card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        // For cheating mode: draw a specific card if available
        public Card DrawSpecificCard(Rank rank)
        {
            Card card = _cards.FirstOrDefault(c => c.Rank == rank);
            if (card != null)
            {
                _cards.Remove(card);
                return card;
            }
            // Fallback to normal draw if specific card not found
            return DrawCard();
        }

        // For cheating mode: peek at the next card without drawing
        public Card PeekNextCard()
        {
            return _cards.Count > 0 ? _cards[0] : null;
        }

        public void Reset()
        {
            InitializeDeck();
            Shuffle();
        }
    }
}