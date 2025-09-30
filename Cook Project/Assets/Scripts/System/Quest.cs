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

public abstract class Quest
{
    public string Id { get; protected set; }
    public string Title { get; protected set; }
    public string Description { get; protected set; }
    public QuestType Type { get; protected set; }
    public QuestStatus Status { get; protected set; }
    public DateTime CreatedTime { get; protected set; }

    protected Quest(string id, string title, string description, QuestType type)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
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

    public abstract bool CanComplete();
}