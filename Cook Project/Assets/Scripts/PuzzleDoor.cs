using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string doorId;
    [SerializeField] private PuzzleGameType puzzleType = PuzzleGameType.NumberGuessing;

    public string DoorId => doorId;

    private void Awake()
    {
        if (string.IsNullOrEmpty(doorId))
        {
            doorId = $"door_{GetInstanceID()}";
        }
    }

    public void Interact()
    {
        if (!QuestManager.Instance.HasActiveQuestForDoor(doorId))
        {
            Debug.Log($"No active quest for door {doorId}");
            return;
        }

        var puzzleQuest = QuestManager.Instance.GetPuzzleQuestByDoorId(doorId);
        if (puzzleQuest == null || puzzleQuest.IsSolved)
        {
            Debug.Log($"Door {doorId} puzzle already solved");
            return;
        }

        switch (puzzleType)
        {
            case PuzzleGameType.NumberGuessing:
                OpenNumberGuessingGame();
                break;
            default:
                Debug.LogWarning($"Unsupported puzzle type: {puzzleType}");
                break;
        }
    }

    private void OpenNumberGuessingGame()
    {
        PuzzleGameManager.Instance.StartNumberGuessingGame(doorId);

        var numberGuessingUI = UIRoot.Instance.GetUIComponent<NumberGuessingGameUI>();
        if (numberGuessingUI != null)
        {
            numberGuessingUI.Open();
        }
        else
        {
            Debug.LogError("NumberGuessingGameUI not found in UIRoot");
        }
    }
}