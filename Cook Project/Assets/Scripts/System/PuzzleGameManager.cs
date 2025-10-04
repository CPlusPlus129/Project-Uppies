using R3;

public class PuzzleGameManager : SimpleSingleton<PuzzleGameManager>
{
    public ReactiveProperty<IPuzzle> CurrentPuzzleGame = new ReactiveProperty<IPuzzle>();
    public ReactiveProperty<PuzzleQuest> CurrentPuzzleQuest = new ReactiveProperty<PuzzleQuest>();
    public ReactiveProperty<bool> IsGameActive = new ReactiveProperty<bool>(false);

    public Subject<IPuzzle> OnGameStarted = new Subject<IPuzzle>();
    public Subject<string> OnGameCompleted = new Subject<string>();
    public Subject<string> OnGameClosed = new Subject<string>();

    public void StartPuzzleGame(PuzzleGameType puzzleType, PuzzleQuest quest)
    {
        if (IsGameActive.Value) return;

        CurrentPuzzleGame.Value = puzzleType switch
        {
            PuzzleGameType.NumberGuessing => new NumberGuessingGame(),
            PuzzleGameType.CardSwipe => new CardSwipeGame(),
            _ => null
        };
        CurrentPuzzleQuest.Value = quest;
        IsGameActive.Value = true;
        OnGameStarted.OnNext(CurrentPuzzleGame.Value);
    }

    public void CompletePuzzleGame(PuzzleGameType puzzleType)
    {
        if (!IsGameActive.Value || CurrentPuzzleGame.Value.puzzleType != puzzleType) return;

        CompleteGame();
    }

    private void CompleteGame()
    {
        if (!IsGameActive.Value) return;

        CurrentPuzzleQuest.Value.SolvePuzzle();
        var questId = CurrentPuzzleQuest.Value.Id;
        QuestManager.Instance.CompleteQuest(CurrentPuzzleQuest.Value.Id);

        EndGame();
        OnGameCompleted.OnNext(questId);
    }

    public void EndGame()
    {
        var questId = CurrentPuzzleQuest.Value.Id;
        CurrentPuzzleGame.Value = null;
        CurrentPuzzleQuest.Value = null;
        IsGameActive.Value = false;
        OnGameClosed.OnNext(questId);
    }
}