using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using UnityEngine.InputSystem;

public class CardSwipeGameUI : MonoBehaviour
{
    [Header("Game Components")]
    [SerializeField] private DraggableCard draggableCard;
    [SerializeField] private CardReader cardReader;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Button closeButton;

    [Header("Card Positions")]
    [SerializeField] private RectTransform startPosition;
    [SerializeField] private RectTransform endPosition;

    private CardSwipeGame currentGame;
    private float swipeDistance;

    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindActionMap("CardSwipe").FindAction("Esc").performed += ctx => Close();

        closeButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(this);

        CalculateSwipeDistance();
        SetupCardEvents();

        PuzzleGameManager.Instance.OnGameStarted
            .Where(x => x is CardSwipeGame)
            .Subscribe(game =>
            {
                currentGame = game as CardSwipeGame;
                InitializeGame();
            })
            .AddTo(this);
    }

    private void CalculateSwipeDistance()
    {
        swipeDistance = Vector2.Distance(startPosition.anchoredPosition, endPosition.anchoredPosition);
    }

    private void SetupCardEvents()
    {
        draggableCard.OnSwipeStart.Subscribe(_ => OnSwipeStart());
        draggableCard.OnSwipeComplete.Subscribe(tuple => OnSwipeComplete(tuple.Item1, tuple.Item2));
    }

    private void OnEnable()
    {
        InputManager.Instance.PushActionMap("CardSwipe");
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("CardSwipe");
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void InitializeGame()
    {
        draggableCard.Initialize(
            startPosition.anchoredPosition,
            endPosition.anchoredPosition,
            swipeDistance
        );

        instructionText.text = "Swipe the card at the right speed";
        feedbackText.text = currentGame.GetSpeedRangeHint();
        cardReader.SetStatus(CardReaderStatus.Ready);
        draggableCard.SetCardEnabled(true);
    }

    private void OnSwipeStart()
    {
        feedbackText.text = "Swiping...";
    }

    private void OnSwipeComplete(float speed, bool reachedEnd)
    {
        SwipeResult result = currentGame.CheckSwipe(speed, reachedEnd);

        switch (result)
        {
            case SwipeResult.Success:
                OnSwipeSuccess();
                break;
            case SwipeResult.TooSlow:
                OnSwipeFail("Too slow! Try swiping faster.");
                break;
            case SwipeResult.TooFast:
                OnSwipeFail("Too fast! Try swiping slower.");
                break;
            case SwipeResult.Incomplete:
                OnSwipeFail("Swipe the card all the way through!");
                break;
        }
    }

    private void OnSwipeSuccess()
    {
        feedbackText.text = "Access Granted!";
        cardReader.PlaySuccessAnimation();
        draggableCard.SetCardEnabled(false);

        PuzzleGameManager.Instance.CompletePuzzleGame(PuzzleGameType.CardSwipe);
    }

    private void OnSwipeFail(string message)
    {
        feedbackText.text = $"{message} (Attempt {currentGame.AttemptCount})";
        cardReader.PlayFailAnimation();
        var animationDuration = 1.5f;
        draggableCard.MoveToResetPosition(animationDuration);
        Observable.Timer(System.TimeSpan.FromSeconds(animationDuration))
            .Subscribe(_ =>
            {
                cardReader.SetStatus(CardReaderStatus.Ready);
                feedbackText.text = currentGame.GetSpeedRangeHint();
            })
            .AddTo(this);
    }

    private void ResetGame()
    {
        draggableCard.ResetPosition();
        cardReader.SetStatus(CardReaderStatus.Ready);
        currentGame.Reset();
        feedbackText.text = currentGame.GetSpeedRangeHint();
        draggableCard.SetCardEnabled(true);
    }
}