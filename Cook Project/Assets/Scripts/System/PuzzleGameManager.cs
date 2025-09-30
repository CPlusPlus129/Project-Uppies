using R3;

public class PuzzleGameManager : SimpleSingleton<PuzzleGameManager>
{
    public ReactiveProperty<NumberGuessingGame> CurrentNumberGame = new ReactiveProperty<NumberGuessingGame>();
    public ReactiveProperty<CardSwipeGame> CurrentCardGame = new ReactiveProperty<CardSwipeGame>();
    public ReactiveProperty<string> CurrentDoorId = new ReactiveProperty<string>();
    public ReactiveProperty<bool> IsGameActive = new ReactiveProperty<bool>(false);
    public ReactiveProperty<PuzzleGameType> CurrentGameType = new ReactiveProperty<PuzzleGameType>();

    public Subject<Unit> OnGameCompleted = new Subject<Unit>();
    public Subject<Unit> OnGameClosed = new Subject<Unit>();

    public void StartNumberGuessingGame(string doorId)
    {
        if (IsGameActive.Value) return;

        var game = new NumberGuessingGame();
        CurrentNumberGame.Value = game;
        CurrentDoorId.Value = doorId;
        CurrentGameType.Value = PuzzleGameType.NumberGuessing;
        IsGameActive.Value = true;
    }

    public void StartCardSwipeGame(string doorId)
    {
        if (IsGameActive.Value) return;

        var game = new CardSwipeGame();
        CurrentCardGame.Value = game;
        CurrentDoorId.Value = doorId;
        CurrentGameType.Value = PuzzleGameType.CardSwipe;
        IsGameActive.Value = true;
    }

    public bool GuessNumber(string guess)
    {
        if (!IsGameActive.Value || CurrentNumberGame.Value == null)
            return false;

        bool isCorrect = CurrentNumberGame.Value.Guess(guess);

        if (isCorrect)
        {
            CompleteGame();
        }

        return isCorrect;
    }

    public string GetCurrentHint()
    {
        return CurrentNumberGame.Value?.GetHint() ?? "";
    }

    public void CompleteCardSwipeGame()
    {
        if (!IsGameActive.Value || CurrentGameType.Value != PuzzleGameType.CardSwipe) return;

        CompleteGame();
    }

    private void CompleteGame()
    {
        if (!IsGameActive.Value) return;

        string doorId = CurrentDoorId.Value;
        QuestManager.Instance.SolvePuzzleForDoor(doorId);

        EndGame();
        OnGameCompleted.OnNext(Unit.Default);
    }

    public void EndGame()
    {
        CurrentNumberGame.Value = null;
        CurrentCardGame.Value = null;
        CurrentDoorId.Value = "";
        CurrentGameType.Value = PuzzleGameType.NumberGuessing;
        IsGameActive.Value = false;
        OnGameClosed.OnNext(Unit.Default);
    }
}