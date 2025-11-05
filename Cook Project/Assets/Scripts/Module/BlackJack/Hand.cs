using System.Collections.Generic;
using System.Linq;

namespace BlackjackGame
{
    public class Hand
    {
        private List<Card> _cards;

        public IReadOnlyList<Card> Cards => _cards.AsReadOnly();
        public int CardCount => _cards.Count;

        public Hand()
        {
            _cards = new List<Card>();
        }

        public void AddCard(Card card)
        {
            _cards.Add(card);
        }

        public void Clear()
        {
            _cards.Clear();
        }

        // Calculate the best value for this hand (handling Aces as 1 or 11)
        public int GetValue()
        {
            int value = 0;
            int aceCount = 0;

            foreach (Card card in _cards)
            {
                value += card.GetValue();
                if (card.IsAce())
                    aceCount++;
            }

            // Try to use Aces as 11 if possible
            while (aceCount > 0 && value + 10 <= 21)
            {
                value += 10;
                aceCount--;
            }

            return value;
        }

        public bool IsBusted() => GetValue() > 21;
        public bool IsBlackjack() => CardCount == 2 && GetValue() == 21;

        public Card GetFirstCard() => _cards.Count > 0 ? _cards[0] : null;
    }
}