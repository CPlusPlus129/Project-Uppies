using R3;
using System.Collections.Generic;

public interface IQuestService : IGameService
{
    Subject<Quest> OnQuestStarted { get; }
    Subject<Quest> OnQuestCompleted { get; }
    Subject<Quest> OnQuestFailed { get; }
    IReadOnlyList<Quest> ongoingQuestList { get; }
    IReadOnlyList<Quest> completedQuestList { get; }
    QuestStatus GetQuestStatus(string questId);
    void StartQuest(string questId);
    void CompleteQuest(string questId);
    void FailQuest(string questId);
}