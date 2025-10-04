using Unity.VisualScripting;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string doorId;

    [SerializeField] private GameObject door;
    private bool doorOpen;
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

        switch (puzzleQuest.PuzzleType)
        {
            case PuzzleGameType.NumberGuessing:
                OpenNumberGuessingGame();
                break;
            case PuzzleGameType.CardSwipe:
                OpenCardSwipeGame();
                break;
            default:
                Debug.LogWarning($"Unsupported puzzle type: {puzzleQuest.PuzzleType}");
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

    private void OpenCardSwipeGame()
    {
        PuzzleGameManager.Instance.StartCardSwipeGame(doorId);

        var cardSwipeUI = UIRoot.Instance.GetUIComponent<CardSwipeGameUI>();
        if (cardSwipeUI != null)
        {
            cardSwipeUI.Open();
        }
        else
        {
            Debug.LogError("CardSwipeGameUI not found in UIRoot");
        }
    }

    
    private void PlayDoorAnimation()
    {
        doorOpen =! doorOpen;
        door.GetComponent<Animator>().SetBool("IsOpen",doorOpen);
    }
}