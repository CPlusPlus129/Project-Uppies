using R3;

public interface IPuzzleGameManager : IGameService
{
    ReactiveProperty<bool> IsGameActive { get; }
    Subject<IPuzzle> OnGameStarted { get; }
    Subject<string> OnGameCompleted { get; }
    void StartPuzzleGame(PuzzleGameType puzzleType, Quest quest);
    void CompletePuzzleGame(PuzzleGameType puzzleType);
    void EndGame();
}