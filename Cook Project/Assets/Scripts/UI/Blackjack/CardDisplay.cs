using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackjackGame
{
    /// <summary>
    /// Component for displaying a single card visually
    /// </summary>
    public class CardDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _suitText;
        [SerializeField] private GameObject _cardFront;
        [SerializeField] private GameObject _cardBack;

        [Header("Suit Colors")]
        [SerializeField] private Color _redColor = Color.red;
        [SerializeField] private Color _blackColor = Color.black;

        public void ShowCard(Card card)
        {
            if (_cardFront != null) _cardFront.SetActive(true);
            if (_cardBack != null) _cardBack.SetActive(false);

            if (_rankText != null)
            {
                _rankText.text = GetRankDisplayText(card.Rank);
            }

            if (_suitText != null)
            {
                _suitText.text = GetSuitSymbol(card.Suit);
                _suitText.color = GetSuitColor(card.Suit);
            }

            if (_rankText != null)
            {
                _rankText.color = GetSuitColor(card.Suit);
            }
        }

        public void ShowCardBack()
        {
            if (_cardFront != null) _cardFront.SetActive(false);
            if (_cardBack != null) _cardBack.SetActive(true);
        }

        private string GetRankDisplayText(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "A",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                _ => ((int)rank).ToString()
            };
        }

        private string GetSuitSymbol(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };
        }

        private Color GetSuitColor(Suit suit)
        {
            return (suit == Suit.Hearts || suit == Suit.Diamonds) ? _redColor : _blackColor;
        }
    }
}