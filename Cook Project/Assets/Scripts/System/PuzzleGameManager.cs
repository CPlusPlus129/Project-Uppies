using R3;

public class PuzzleGameManager : SimpleSingleton<PuzzleGameManager>
{
    public ReactiveProperty<NumberGuessingGame> CurrentNumberGame = new ReactiveProperty<NumberGuessingGame>();
    public ReactiveProperty<string> CurrentDoorId = new ReactiveProperty<string>();
    public ReactiveProperty<bool> IsGameActive = new ReactiveProperty<bool>(false);

    public Subject<Unit> OnGameCompleted = new Subject<Unit>();
    public Subject<Unit> OnGameClosed = new Subject<Unit>();

    public void StartNumberGuessingGame(string doorId)
    {
        if (IsGameActive.Value) return;

        var game = new NumberGuessingGame();
        CurrentNumberGame.Value = game;
        CurrentDoorId.Value = doorId;
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
        CurrentDoorId.Value = "";
        IsGameActive.Value = false;
        OnGameClosed.OnNext(Unit.Default);
    }
}