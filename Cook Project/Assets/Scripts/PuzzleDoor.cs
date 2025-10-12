using R3;
using System.Linq;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string questTargetId;
    [SerializeField] private Animator anim;
    private IQuestService questService;
    private string questId;
    private bool doorOpen;

    private async void Awake()
    {
        if (string.IsNullOrEmpty(questTargetId))
        {
            questTargetId = $"door_{GetInstanceID()}";
        }
        questService = await ServiceLocator.Instance.GetAsync<IQuestService>();
    }

    public void Interact()
    {
        var quest = GetActiveQuestForSelf();
        if (quest == null)
        {
            Debug.Log($"No ongoing quest for Door {questTargetId}.");
            return;
        }

        switch (quest.PuzzleType)
        {
            case PuzzleGameType.NumberGuessing:
                OpenNumberGuessingGame(quest);
                break;
            case PuzzleGameType.CardSwipe:
                OpenCardSwipeGame(quest);
                break;
            default:
                Debug.LogWarning($"Unsupported puzzle type: {quest.PuzzleType}");
                break;
        }
    }

    private void OpenNumberGuessingGame(Quest quest)
    {
        UIRoot.Instance.GetUIComponent<NumberGuessingGameUI>()?.Open();
        PuzzleGameManager.Instance.StartPuzzleGame(PuzzleGameType.NumberGuessing, quest);
    }

    private void OpenCardSwipeGame(Quest quest)
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

    private Quest GetActiveQuestForSelf()
    {
        return questService.ongoingQuestList.FirstOrDefault(q => q.TargetId == questTargetId);
    }
}