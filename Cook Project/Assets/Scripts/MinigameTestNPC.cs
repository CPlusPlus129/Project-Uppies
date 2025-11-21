using Cysharp.Threading.Tasks;
using R3;
using System.Linq;
using UnityEngine;

public class MinigameTestNPC : InteractableBase
{
    [SerializeField] private PuzzleGameType puzzleType = PuzzleGameType.CardSwipe;
    private IPuzzleGameManager puzzleGameManager;
    private IPuzzle gameInstance;

    protected async override void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        puzzleGameManager = await ServiceLocator.Instance.GetAsync<IPuzzleGameManager>();
    }

    public override void Interact()
    {
        switch (puzzleType)
        {
            case PuzzleGameType.CardSwipe:
                OpenCardSwipeGame();
                break;
            default:
                Debug.LogWarning($"Unsupported puzzle type: {puzzleType}");
                break;
        }
    }

    private void OpenCardSwipeGame()
    {
        UIRoot.Instance.GetUIComponent<CardSwipeGameUI>()?.Open();
        // Pass null for quest as we are using task-based unlocking
        gameInstance ??= puzzleGameManager.StartPuzzleGame(PuzzleGameType.CardSwipe, null);
        
        puzzleGameManager.OnGameCompleted
            .Where(x => x == gameInstance)
            .Take(1)
            .Subscribe(_ =>
            {
                Debug.Log("[MinigameTestNPC] on minigame completed");
                gameInstance = null;
            })
            .AddTo(this);
    }

}