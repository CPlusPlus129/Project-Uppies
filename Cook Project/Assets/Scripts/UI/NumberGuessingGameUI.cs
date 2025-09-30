using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using UnityEngine.InputSystem;

[System.Serializable]
public class DigitLock
{
    public TextMeshProUGUI digitText;
    public Button upButton;
    public Button downButton;
}

public class NumberGuessingGameUI : MonoBehaviour
{
    [SerializeField] private DigitLock[] digitLocks = new DigitLock[4];
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private int[] currentDigits = new int[4];

    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindActionMap("NumberGuessing").FindAction("Esc").performed += ctx => Close();

        submitButton.OnClickAsObservable().Subscribe(_ => OnSubmitGuess()).AddTo(this);
        closeButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(this);

        SetupDigitButtons();
    }

    private void Start()
    {
        var puzzleManager = PuzzleGameManager.Instance;

        puzzleManager.CurrentNumberGame.Subscribe(game =>
        {
            if (game != null)
            {
                UpdateHint();
                ClearFeedback();
                ResetDigits();
            }
        }).AddTo(this);

        puzzleManager.OnGameCompleted.Subscribe(_ =>
        {
            ShowFeedback("Correct! Puzzle solved!", Color.green);
            Invoke(nameof(Close), 2f);
        }).AddTo(this);

        puzzleManager.IsGameActive.Subscribe(isActive =>
        {
            if (!isActive)
            {
                Close();
            }
        }).AddTo(this);
    }

    private void SetupDigitButtons()
    {
        for (int i = 0; i < digitLocks.Length; i++)
        {
            int index = i;
            digitLocks[i].upButton.OnClickAsObservable().Subscribe(_ => IncreaseDigit(index)).AddTo(this);
            digitLocks[i].downButton.OnClickAsObservable().Subscribe(_ => DecreaseDigit(index)).AddTo(this);
        }
    }

    private void IncreaseDigit(int index)
    {
        currentDigits[index] = (currentDigits[index] + 1) % 10;
        UpdateDigitDisplay(index);
    }

    private void DecreaseDigit(int index)
    {
        currentDigits[index] = (currentDigits[index] - 1 + 10) % 10;
        UpdateDigitDisplay(index);
    }

    private void UpdateDigitDisplay(int index)
    {
        digitLocks[index].digitText.text = currentDigits[index].ToString();
    }

    private void UpdateAllDigitDisplays()
    {
        for (int i = 0; i < digitLocks.Length; i++)
        {
            UpdateDigitDisplay(i);
        }
    }

    private void OnEnable()
    {
        InputManager.Instance.PushActionMap("NumberGuessing");
        UpdateHint();
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("NumberGuessing");
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ResetDigits();
        ClearFeedback();
    }

    public void Close()
    {
        PuzzleGameManager.Instance.EndGame();
        gameObject.SetActive(false);
    }

    private void OnSubmitGuess()
    {
        string guess = "";
        for (int i = 0; i < currentDigits.Length; i++)
        {
            guess += currentDigits[i].ToString();
        }

        bool isCorrect = PuzzleGameManager.Instance.GuessNumber(guess);

        if (!isCorrect)
        {
            ShowFeedback("Wrong answer! Try again.", Color.red);
        }
    }

    private void UpdateHint()
    {
        string hint = PuzzleGameManager.Instance.GetCurrentHint();
        if (hintText != null)
        {
            hintText.text = hint;
        }
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    private void ResetDigits()
    {
        for (int i = 0; i < currentDigits.Length; i++)
        {
            currentDigits[i] = 0;
        }
        UpdateAllDigitDisplays();
    }
}