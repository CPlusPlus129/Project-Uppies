using Cysharp.Threading.Tasks;
using R3;

public class PuzzleGameManager : IPuzzleGameManager
{
    public ReactiveProperty<IPuzzle> CurrentPuzzleGame = new ReactiveProperty<IPuzzle>();
    public ReactiveProperty<Quest> CurrentPuzzleQuest = new ReactiveProperty<Quest>();
    public ReactiveProperty<bool> IsGameActive { get;  } = new ReactiveProperty<bool>(false);

    public ReplaySubject<IPuzzle> OnGameStarted { get;  } = new ReplaySubject<IPuzzle>(1);
    public Subject<string> OnGameCompleted { get; } = new Subject<string>();
    public Subject<string> OnGameClosed = new Subject<string>();
    private readonly IQuestService questService;

    public PuzzleGameManager(IQuestService questService)
    {
        this.questService = questService;
    }

    public async UniTask Init()
    {
         await UniTask.CompletedTask;
    }

    public void StartPuzzleGame(PuzzleGameType puzzleType, Quest quest)
    {
        if (IsGameActive.Value) return;

        CurrentPuzzleGame.Value = puzzleType switch
        {
            PuzzleGameType.NumberGuessing => new NumberGuessingGame(this),
            PuzzleGameType.CardSwipe => new CardSwipeGame(this),
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

        CurrentPuzzleQuest.Value.CompleteQuest();
        var questId = CurrentPuzzleQuest.Value.Id;
        questService.CompleteQuest(CurrentPuzzleQuest.Value.Id);

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