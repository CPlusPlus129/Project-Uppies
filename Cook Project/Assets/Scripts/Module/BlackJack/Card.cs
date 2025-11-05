using System;

namespace BlackjackGame
{
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    [Serializable]
    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        // Get the value for blackjack (Ace = 1 or 11, Face cards = 10)
        public int GetValue()
        {
            if (Rank >= Rank.Jack)
                return 10;
            return (int)Rank;
        }

        public bool IsAce() => Rank == Rank.Ace;

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}