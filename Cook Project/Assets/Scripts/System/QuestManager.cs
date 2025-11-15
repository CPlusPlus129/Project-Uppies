using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : IQuestService
{
    public Subject<Quest> OnQuestStarted { get; } = new Subject<Quest>();
    public Subject<Quest> OnQuestCompleted { get; } = new Subject<Quest>();
    public Subject<Quest> OnQuestFailed { get; } = new Subject<Quest>();
    private readonly ITableManager tableManager;
    private Table<QuestRow> questTable;
    private List<Quest> ongoingQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();
    public IReadOnlyList<Quest> ongoingQuestList => ongoingQuests;
    public IReadOnlyList<Quest> completedQuestList => completedQuests;

    public QuestManager(ITableManager tableManager)
    {
        this.tableManager = tableManager;
    }

    public async UniTask Init()
    {
        questTable = tableManager.GetTable<QuestRow>();
        await UniTask.CompletedTask;
    }

    public void StartQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || ongoingQuestList.Any(x => x.Id == questId)) return;
        var row = questTable.GetRow(x => x.QuestId == questId);
        if (row == null)
        {
            UnityEngine.Debug.LogWarning($"Quest with ID '{questId}' not found.");
            return;
        }
        var quest = new Quest(row.QuestId, row.Name, row.Description, row.TargetId, row.QuestType, row.PuzzleType);
        ongoingQuests.Add(quest);
        quest.StartQuest();
        OnQuestStarted.OnNext(quest);
        UnityEngine.Debug.Log($"Quest '{questId}' started.");
    }

    public void CompleteQuest(string questId)
    {
        var quest = ongoingQuests.FirstOrDefault(q => q.Id == questId);
        if (quest != null && quest.CanComplete())
        {
            quest.CompleteQuest();
            ongoingQuests.Remove(quest);
            completedQuests.Add(quest);
            OnQuestCompleted.OnNext(quest);
        }
    }

    public void FailQuest(string questId)
    {
        var quest = ongoingQuests.FirstOrDefault(q => q.Id == questId);
        if (quest != null)
        {
            quest.FailQuest();
            ongoingQuests.Remove(quest);
            OnQuestFailed.OnNext(quest);
        }
    }

    public QuestStatus GetQuestStatus(string questId)
    {
        return GetQuestById(questId)?.Status ?? QuestStatus.NotStarted;
    }

    public Quest GetQuestById(string questId)
    {
        return ongoingQuests.FirstOrDefault(q => q.Id == questId) ??
               completedQuests.FirstOrDefault(q => q.Id == questId);
    }

    public void ClearAllQuests()
    {
        ongoingQuests.Clear();
        completedQuests.Clear();
    }

    public void Dispose()
    {
        OnQuestStarted?.Dispose();
        OnQuestCompleted?.Dispose();
        OnQuestFailed?.Dispose();
    }
}
