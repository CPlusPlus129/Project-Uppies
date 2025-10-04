using R3;

public class PuzzleGameManager : SimpleSingleton<PuzzleGameManager>
{
    public ReactiveProperty<NumberGuessingGame> CurrentNumberGame = new ReactiveProperty<NumberGuessingGame>();
    public ReactiveProperty<CardSwipeGame> CurrentCardGame = new ReactiveProperty<CardSwipeGame>();
    public ReactiveProperty<string> CurrentDoorId = new ReactiveProperty<string>();
    public ReactiveProperty<bool> IsGameActive = new ReactiveProperty<bool>(false);
    public ReactiveProperty<PuzzleGameType> CurrentGameType = new ReactiveProperty<PuzzleGameType>();

    public Subject<string> OnGameCompleted = new Subject<string>();
    public Subject<string> OnGameClosed = new Subject<string>();

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

    public void CompleteNumberGuessingGame()
    {
        if (!IsGameActive.Value || CurrentGameType.Value != PuzzleGameType.NumberGuessing) return;

        CompleteGame();
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
        OnGameCompleted.OnNext(doorId);
    }

    public void EndGame()
    {
        var doorId = CurrentDoorId.Value;
        CurrentNumberGame.Value = null;
        CurrentCardGame.Value = null;
        CurrentDoorId.Value = "";
        CurrentGameType.Value = PuzzleGameType.NumberGuessing;
        IsGameActive.Value = false;
        OnGameClosed.OnNext(doorId);
    }
}