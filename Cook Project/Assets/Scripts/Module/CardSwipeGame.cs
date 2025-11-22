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
    private float swipeDistance;

    public CardSwipeGame(IPuzzleGameManager puzzleGameManager)
    {
        this.puzzleGameManager = puzzleGameManager;
        GenerateSpeedRange();
        Reset();
    }

    public void SetSwipeDistance(float distance)
    {
        swipeDistance = distance;
    }

    private void GenerateSpeedRange()
    {
        float baseSpeed = UnityEngine.Random.Range(600f, 1000f);
        float tolerance = 175;

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
        if (swipeDistance <= 0)
        {
            return $"Target speed: {MinSpeed:F0} - {MaxSpeed:F0} units/sec";
        }

        float minTime = swipeDistance / MaxSpeed;
        float maxTime = swipeDistance / MinSpeed;
        return $"Swipe in {minTime:F2} - {maxTime:F2} seconds";
    }
}