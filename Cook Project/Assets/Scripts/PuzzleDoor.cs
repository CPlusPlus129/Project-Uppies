using R3;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string questTargetId;
    [SerializeField] private Animator anim;
    private string questId;
    private bool doorOpen;

    private void Awake()
    {
        if (string.IsNullOrEmpty(questTargetId))
        {
            questTargetId = $"door_{GetInstanceID()}";
        }
    }

    public void Interact()
    {
        if (!QuestManager.Instance.HasActiveQuestForTarget(questTargetId))
        {
            Debug.Log($"No active quest for door {questTargetId}");
            return;
        }

        var puzzleQuest = QuestManager.Instance.GetPuzzleQuestByTargetId(questTargetId);
        if (puzzleQuest == null || puzzleQuest.IsSolved)
        {
            Debug.Log($"Door {questTargetId} puzzle already solved");
            return;
        }

        questId = puzzleQuest.Id;
        switch (puzzleQuest.PuzzleType)
        {
            case PuzzleGameType.NumberGuessing:
                OpenNumberGuessingGame(puzzleQuest);
                break;
            case PuzzleGameType.CardSwipe:
                OpenCardSwipeGame(puzzleQuest);
                break;
            default:
                Debug.LogWarning($"Unsupported puzzle type: {puzzleQuest.PuzzleType}");
                break;
        }
    }

    private void OpenNumberGuessingGame(PuzzleQuest quest)
    {
        UIRoot.Instance.GetUIComponent<NumberGuessingGameUI>()?.Open();
        PuzzleGameManager.Instance.StartPuzzleGame(PuzzleGameType.NumberGuessing, quest);
    }

    private void OpenCardSwipeGame(PuzzleQuest quest)
    {
        UIRoot.Instance.GetUIComponent<CardSwipeGameUI>()?.Open();
        PuzzleGameManager.Instance.StartPuzzleGame(PuzzleGameType.CardSwipe, quest);
        PuzzleGameManager.Instance.OnGameCompleted
            .Where(x => x == questId)
            .Take(1)
            .Subscribe(_ => PlayDoorAnimation())
            .AddTo(this);
    }


    private void PlayDoorAnimation()
    {
        doorOpen = !doorOpen;
        anim.SetBool("IsOpen", doorOpen);
    }
}