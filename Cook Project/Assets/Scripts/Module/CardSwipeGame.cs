public enum SwipeResult
{
    TooSlow,
    TooFast,
    Success,
    Incomplete
}

public class CardSwipeGame : IPuzzle
{
    readonly IPuzzleGameManager puzzleGameManager;
    public PuzzleGameType puzzleType => PuzzleGameType.CardSwipe;
    public float MinSpeed { get; private set; }
    public float MaxSpeed { get; private set; }
    public bool IsCompleted { get; private set; }
    public int AttemptCount { get; private set; }

    public CardSwipeGame(IPuzzleGameManager puzzleGameManager)
    {
        this.puzzleGameManager = puzzleGameManager;
        GenerateSpeedRange();
        Reset();
    }

    private void GenerateSpeedRange()
    {
        float baseSpeed = UnityEngine.Random.Range(600f, 1000f);
        float tolerance = 125;

        MinSpeed = baseSpeed - tolerance;
        MaxSpeed = baseSpeed + tolerance;
    }

    public SwipeResult CheckSwipe(float speed, bool reachedEnd)
    {
        if (!reachedEnd)
        {
            return SwipeResult.Incomplete;
        }

        AttemptCount++;

        if (speed < MinSpeed)
        {
            return SwipeResult.TooSlow;
        }
        else if (speed > MaxSpeed)
        {
            return SwipeResult.TooFast;
        }
        else
        {
            IsCompleted = true;
            return SwipeResult.Success;
        }
    }

    public void Reset()
    {
        IsCompleted = false;
        AttemptCount = 0;
    }

    public string GetSpeedRangeHint()
    {
        return $"Target speed: {MinSpeed:F0} - {MaxSpeed:F0} units/sec";
    }
}