using R3;

public interface IPuzzleGameManager : IGameService
{
    ReactiveProperty<bool> IsGameActive { get; }
    ReplaySubject<IPuzzle> OnGameStarted { get; }
    Subject<IPuzzle> OnGameCompleted { get; }
    IPuzzle StartPuzzleGame(PuzzleGameType puzzleType, Quest quest);
    void CompletePuzzleGame(PuzzleGameType puzzleType);
    void EndGame();
}