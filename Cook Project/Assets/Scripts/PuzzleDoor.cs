using Cysharp.Threading.Tasks;
using R3;
using System.Linq;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string requiredTaskId;
    [SerializeField] private PuzzleGameType puzzleType = PuzzleGameType.CardSwipe;
    [SerializeField] private Animator anim;
    private IPuzzleGameManager puzzleGameManager;
    private IPuzzle gameInstance;
    private bool doorOpen;

    private async void Awake()
    {
        // Auto-assign task ID for the specific door mentioned
        if (string.IsNullOrEmpty(requiredTaskId) && gameObject.name == "StorageRoom1_DoorButton 1")
        {
            requiredTaskId = "StorageRoom1Task";
        }

        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        puzzleGameManager = await ServiceLocator.Instance.GetAsync<IPuzzleGameManager>();
    }

    public void Interact()
    {
        if (string.IsNullOrEmpty(requiredTaskId))
        {
            Debug.LogWarning($"PuzzleDoor: No requiredTaskId set for {gameObject.name}.");
            return;
        }

        // Check if task exists in the active tasks list
        var taskExists = TaskManager.Instance.Tasks.Value.Any(t => t.Id == requiredTaskId);
        
        if (!taskExists)
        {
            Debug.Log($"PuzzleDoor: Task '{requiredTaskId}' not found/active. Door remains locked.");
            return;
        }

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
                PlayDoorAnimation();
                gameInstance = null;
            })
            .AddTo(this);
    }

    private void PlayDoorAnimation()
    {
        doorOpen = !doorOpen;
        anim.SetBool("IsOpen", doorOpen);
    }
}