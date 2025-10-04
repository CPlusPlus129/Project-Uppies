using R3;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : SimpleSingleton<QuestManager>
{
    public Subject<Quest> OnQuestStarted = new Subject<Quest>();
    public Subject<Quest> OnQuestCompleted = new Subject<Quest>();
    public Subject<Quest> OnQuestFailed = new Subject<Quest>();

    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();

    public void AddQuest(Quest quest)
    {
        activeQuests.Add(quest);
        quest.StartQuest();
        OnQuestStarted.OnNext(quest);
    }

    public void CompleteQuest(string questId)
    {
        var quest = activeQuests.FirstOrDefault(q => q.Id == questId);
        if (quest != null && quest.CanComplete())
        {
            quest.CompleteQuest();
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            OnQuestCompleted.OnNext(quest);
        }
    }

    public void FailQuest(string questId)
    {
        var quest = activeQuests.FirstOrDefault(q => q.Id == questId);
        if (quest != null)
        {
            quest.FailQuest();
            activeQuests.Remove(quest);
            OnQuestFailed.OnNext(quest);
        }
    }

    public PuzzleQuest CreatePuzzleQuest(string questId, string title, string description, PuzzleGameType puzzleType, string doorId)
    {
        return new PuzzleQuest(questId, title, description, puzzleType, doorId);
    }

    public IReadOnlyList<Quest> GetActiveQuests()
    {
        return activeQuests;
    }

    public IReadOnlyList<Quest> GetCompletedQuests()
    {
        return completedQuests;
    }

    public Quest GetQuestById(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.Id == questId) ??
               completedQuests.FirstOrDefault(q => q.Id == questId);
    }

    public bool IsQuestCompleted(string questId)
    {
        return GetQuestById(questId)?.Status == QuestStatus.Completed;
    }

    public PuzzleQuest GetPuzzleQuestByTargetId(string targetId)
    {
        return activeQuests.OfType<PuzzleQuest>().FirstOrDefault(q => q.QuestTargetId == targetId);
    }

    public bool HasActiveQuestForTarget(string targetId)
    {
        return GetPuzzleQuestByTargetId(targetId) != null;
    }

    public void ClearAllQuests()
    {
        activeQuests.Clear();
        completedQuests.Clear();
    }
}