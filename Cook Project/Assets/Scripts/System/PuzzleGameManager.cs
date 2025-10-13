using Cysharp.Threading.Tasks;
using R3;

public class PuzzleGameManager : IPuzzleGameManager
{
    public ReactiveProperty<IPuzzle> CurrentPuzzleGame = new ReactiveProperty<IPuzzle>();
    public ReactiveProperty<Quest> CurrentPuzzleQuest = new ReactiveProperty<Quest>();
    public ReactiveProperty<bool> IsGameActive { get; } = new ReactiveProperty<bool>(false);

    public ReplaySubject<IPuzzle> OnGameStarted { get; } = new ReplaySubject<IPuzzle>(1);
    public Subject<IPuzzle> OnGameCompleted { get; } = new Subject<IPuzzle>();
    private readonly IQuestService questService;

    public PuzzleGameManager(IQuestService questService)
    {
        this.questService = questService;
    }

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    public IPuzzle StartPuzzleGame(PuzzleGameType puzzleType, Quest quest = null)
    {
        if (IsGameActive.Value) return null;

        CurrentPuzzleGame.Value = puzzleType switch
        {
            PuzzleGameType.NumberGuessing => new NumberGuessingGame(this),
            PuzzleGameType.CardSwipe => new CardSwipeGame(this),
            _ => null
        };
        CurrentPuzzleQuest.Value = quest;
        IsGameActive.Value = true;
        OnGameStarted.OnNext(CurrentPuzzleGame.Value);
        return CurrentPuzzleGame.Value;
    }

    public void CompletePuzzleGame(PuzzleGameType puzzleType)
    {
        if (!IsGameActive.Value || CurrentPuzzleGame.Value.puzzleType != puzzleType) return;

        CompleteGame();
    }

    private void CompleteGame()
    {
        if (!IsGameActive.Value) return;

        if (CurrentPuzzleQuest.Value != null)
        {
            CurrentPuzzleQuest.Value.IsSolved = true;
            var questId = CurrentPuzzleQuest.Value.Id;
            questService.CompleteQuest(CurrentPuzzleQuest.Value.Id);
        }

        var game = CurrentPuzzleGame.Value;
        EndGame();
        OnGameCompleted.OnNext(game);
    }

    public void EndGame()
    {
        CurrentPuzzleGame.Value = null;
        CurrentPuzzleQuest.Value = null;
        IsGameActive.Value = false;
    }
}