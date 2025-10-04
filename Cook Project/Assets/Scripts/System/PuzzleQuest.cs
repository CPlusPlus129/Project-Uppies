public class PuzzleQuest : Quest
{
    public PuzzleGameType PuzzleType { get; private set; }
    public string QuestTargetId { get; private set; }
    public bool IsSolved { get; private set; }

    public PuzzleQuest(string id, string title, string description, PuzzleGameType puzzleType, string targetId)
        : base(id, title, description, QuestType.Puzzle)
    {
        PuzzleType = puzzleType;
        QuestTargetId = targetId;
        IsSolved = false;
    }

    public void SolvePuzzle()
    {
        IsSolved = true;
        CompleteQuest();
    }

    public override bool CanComplete()
    {
        return IsSolved;
    }
}