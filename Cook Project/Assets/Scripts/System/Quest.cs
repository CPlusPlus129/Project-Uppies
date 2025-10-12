using System;

public enum QuestType
{
    Puzzle
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public class Quest
{
    public string Id { get; protected set; }
    public string Title { get; protected set; }
    public string Description { get; protected set; }
    public string TargetId { get; protected set; }
    public PuzzleGameType PuzzleType { get; protected set; }
    public QuestType Type { get; protected set; }

    public bool IsSolved { get; private set; }
    public QuestStatus Status { get; protected set; }
    public DateTime CreatedTime { get; protected set; }

    public Quest(string id, string title, string description, string targetId, QuestType type, PuzzleGameType puzzleType)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        TargetId = targetId;
        PuzzleType = puzzleType;
        Status = QuestStatus.NotStarted;
        CreatedTime = DateTime.Now;
    }

    public virtual void StartQuest()
    {
        Status = QuestStatus.InProgress;
    }

    public virtual void CompleteQuest()
    {
        Status = QuestStatus.Completed;
    }

    public virtual void FailQuest()
    {
        Status = QuestStatus.Failed;
    }

    public bool CanComplete()
    {
        return IsSolved;
    }
}