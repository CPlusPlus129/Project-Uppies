using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlackjackGame
{
    public class BlackjackUI : MonoBehaviour, IUIInitializable
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _chipsText;
        [SerializeField] private TextMeshProUGUI _betText;
        [SerializeField] private TextMeshProUGUI _playerValueText;
        [SerializeField] private TextMeshProUGUI _dealerValueText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _betInfoText;

        [Header("Card Display")]
        [SerializeField] private Transform _playerCardContainer;
        [SerializeField] private Transform _dealerCardContainer;
        [SerializeField] private GameObject _cardPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _hitButton;
        [SerializeField] private Button _standButton;
        [SerializeField] private Button _betButton;
        [SerializeField] private TMP_InputField _betInputField;

        [Header("Devil's Deal Panel")]
        [SerializeField] private GameObject _devilsDealPanel;
        [SerializeField] private Button _acceptDealButton;
        [SerializeField] private Button _declineDealButton;

        private IBlackjackSystem _blackjackSystem;
        private List<GameObject> _playerCardObjects = new List<GameObject>();
        private List<GameObject> _dealerCardObjects = new List<GameObject>();
        private CompositeDisposable _disposables = new CompositeDisposable();

        // Event triggered when round ends (for dialogue integration)
        public event Action<RoundResult, int> OnRoundCompleted;

        // Event for when devil's deal is shown
        public event Action OnDevilsDealOffered;

        public async UniTask Init()
        {
            _cardPrefab.SetActive(false);
            _blackjackSystem = await ServiceLocator.Instance.GetAsync<IBlackjackSystem>();
            _betInfoText.text = $"BET AMOUNT: {_blackjackSystem.MinimumBet}~{_blackjackSystem.MaximumBet}";
            SetupButtonListeners();
            BindToSystem();

            if (_devilsDealPanel != null)
                _devilsDealPanel.SetActive(false);

            InputSystem.actions.FindActionMap("Blackjack").FindAction("Esc").performed += ctx => Close();

            await UniTask.CompletedTask;
        }

        public void OnEnable()
        {
            InputManager.Instance.PushActionMap("Blackjack");
        }

        public void OnDisable()
        {
            InputManager.Instance.PopActionMap("Blackjack");
        }

        private void SetupButtonListeners()
        {
            _hitButton.OnClickAsObservable().Subscribe(_ => OnHitClicked().Forget()).AddTo(this);
            _standButton.OnClickAsObservable().Subscribe(_ => OnStandClicked().Forget()).AddTo(this);
            _betButton.OnClickAsObservable().Subscribe(_ => OnBetClicked().Forget()).AddTo(this);
            _acceptDealButton.OnClickAsObservable().Subscribe(_ => OnAcceptDeal()).AddTo(this);
            _declineDealButton.OnClickAsObservable().Subscribe(_ => OnDeclineDeal()).AddTo(this);
        }

        private void BindToSystem()
        {
            // Bind chips display
            _blackjackSystem.PlayerChips
                .Subscribe(chips => UpdateChipsDisplay(chips))
                .AddTo(_disposables);

            // Bind game state to button availability
            _blackjackSystem.CurrentGameState
                .Subscribe(state => UpdateButtonStates(state))
                .AddTo(_disposables);

            // Bind hand updates
            _blackjackSystem.OnPlayerHandUpdated
                .Subscribe(hand => UpdatePlayerHandDisplay(hand))
                .AddTo(_disposables);

            _blackjackSystem.OnDealerHandUpdated
                .Subscribe(hand => UpdateDealerHandDisplay(hand))
                .AddTo(_disposables);

            // Bind round end
            _blackjackSystem.OnRoundEnded
                .Subscribe(result => OnRoundEnded(result.result, result.chipsWon))
                .AddTo(_disposables);
        }

        private void UpdateChipsDisplay(int chips)
        {
            if (_chipsText != null)
                _chipsText.text = $"${chips}";
        }

        private void UpdateButtonStates(GameState state)
        {
            bool isPlayerTurn = state == GameState.PlayerTurn;
            bool isWaitingForBet = state == GameState.WaitingForBet;
            bool DealingInitialCards = state == GameState.DealingInitialCards;

            if (_hitButton != null)
                _hitButton.interactable = isPlayerTurn;

            if (_standButton != null)
                _standButton.interactable = isPlayerTurn;

            if (_betButton != null)
                _betButton.interactable = isWaitingForBet;

            if (_betInputField != null)
                _betInputField.interactable = isWaitingForBet;

            // Clear result text when new round starts
            if (DealingInitialCards && _resultText != null)
                _resultText.text = "";
        }

        private void UpdatePlayerHandDisplay(Hand hand)
        {
            ClearCardObjects(_playerCardObjects);

            foreach (Card card in hand.Cards)
            {
                GameObject cardObj = InstantiateCard(card, _playerCardContainer);
                _playerCardObjects.Add(cardObj);
            }

            if (_playerValueText != null)
                _playerValueText.text = $"{hand.GetValue()}";
        }

        private void UpdateDealerHandDisplay(Hand hand)
        {
            ClearCardObjects(_dealerCardObjects);

            bool hideFirstCard = _blackjackSystem.CurrentGameState.CurrentValue <= GameState.PlayerTurn;

            for (int i = 0; i < hand.Cards.Count; i++)
            {
                Card card = hand.Cards[i];
                bool shouldHide = hideFirstCard && i == 0;
                GameObject cardObj = InstantiateCard(card, _dealerCardContainer, shouldHide);
                _dealerCardObjects.Add(cardObj);
            }

            if (_dealerValueText != null)
            {
                if (hideFirstCard && hand.CardCount > 0)
                {
                    if(hand.CardCount == 1)
                        _dealerValueText.text = "?";
                    else
                    {                        
                        // Exclude first card value
                        _dealerValueText.text = $"? + {hand.Cards.Skip(1).Sum(card => card.GetValue())}";
                    }
                }
                else
                {
                    _dealerValueText.text = $"{hand.GetValue()}";
                }
            }
        }

        private GameObject InstantiateCard(Card card, Transform parent, bool hideCard = false)
        {
            GameObject cardObj = Instantiate(_cardPrefab, parent);
            cardObj.SetActive(true);
            // Get card display component (you'll need to create this)
            CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                if (hideCard)
                    cardDisplay.ShowCardBack();
                else
                    cardDisplay.ShowCard(card);
            }
            else
            {
                // Fallback: display card text
                TextMeshProUGUI cardText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (cardText != null)
                {
                    cardText.text = hideCard ? "?" : card.ToString();
                }
            }

            return cardObj;
        }

        private void ClearCardObjects(List<GameObject> cardObjects)
        {
            foreach (GameObject obj in cardObjects)
            {
                Destroy(obj);
            }
            cardObjects.Clear();
        }

        private async UniTaskVoid OnBetClicked()
        {
            if (int.TryParse(_betInputField.text, out int betAmount))
            {
                if (_blackjackSystem.CanPlaceBet(betAmount))
                {
                    if (_betText != null)
                        _betText.text = $"${betAmount}";

                    await _blackjackSystem.PlaceBetAndStartRound(betAmount);
                }
                else
                {
                    ShowMessage("Invalid bet amount!");
                }
            }
        }

        private async UniTaskVoid OnHitClicked()
        {
            await _blackjackSystem.PlayerHit();
        }

        private async UniTaskVoid OnStandClicked()
        {
            await _blackjackSystem.PlayerStand();
        }

        private void OnRoundEnded(RoundResult result, int chipsWon)
        {
            DisplayRoundResult(result, chipsWon);

            // Trigger event for external systems (dialogue, etc.)
            OnRoundCompleted?.Invoke(result, chipsWon);

            // Check if should offer devil's deal (only if player hasn't accepted yet)
            if (!_blackjackSystem.IsCheatingForPlayer && ShouldOfferDevilsDeal())
            {
                ShowDevilsDealAsync().Forget();
            }
        }

        private void DisplayRoundResult(RoundResult result, int chipsWon)
        {
            if (_resultText == null) return;

            string resultMessage = result switch
            {
                RoundResult.PlayerBlackjack => $"BLACKJACK! You won ${chipsWon}!",
                RoundResult.PlayerWin => $"You WIN! +${chipsWon}",
                RoundResult.DealerWin => $"Dealer wins. You lost ${-chipsWon}",
                RoundResult.Push => "Push - Bet returned",
                _ => ""
            };

            _resultText.text = resultMessage;
        }

        private bool ShouldOfferDevilsDeal()
        {
            // Offer devil's deal after player has lost several rounds
            return _blackjackSystem.RoundCount >= 3;
        }

        private async UniTaskVoid ShowDevilsDealAsync()
        {
            await UniTask.Delay(1500);

            if (_devilsDealPanel != null)
            {
                _devilsDealPanel.SetActive(true);
                OnDevilsDealOffered?.Invoke();
            }
        }

        private void OnAcceptDeal()
        {
            _blackjackSystem.ActivateCheatMode(true); // Player will now win

            if (_devilsDealPanel != null)
                _devilsDealPanel.SetActive(false);

            ShowMessage("The devil's power flows through you...");
        }

        private void OnDeclineDeal()
        {
            if (_devilsDealPanel != null)
                _devilsDealPanel.SetActive(false);

            ShowMessage("You refused the devil's offer.");
        }

        private void ShowMessage(string message)
        {
            if (_resultText != null)
                _resultText.text = message;
        }

        // Public method to manually trigger devil's deal (for dialogue system)
        public void TriggerDevilsDeal()
        {
            if (_devilsDealPanel != null)
                _devilsDealPanel.SetActive(true);
        }

        // Public method to check cheat mode status
        public bool HasAcceptedDeal() => _blackjackSystem.IsCheatingForPlayer;

        public void Open() { gameObject.SetActive(true); }

        public void Close() { gameObject.SetActive(false); }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}