// This class maps to the columns in Quest.csv
public class QuestRow
{
    [Column("ID")]
    public string QuestId;

    [Column("Title")]
    public string Name;

    [Column("Description")]
    public string Description;

    [Column("Type")]
    public QuestType QuestType;

    [Column("TargetID")]
    public string TargetId;

    [Column("PuzzleType")]
    public PuzzleGameType PuzzleType;
}