public enum SwipeResult
{
    TooSlow,
    TooFast,
    Success,
    Incomplete
}

public class CardSwipeGame
{
    public float MinSpeed { get; private set; }
    public float MaxSpeed { get; private set; }
    public bool IsCompleted { get; private set; }
    public int AttemptCount { get; private set; }

    public CardSwipeGame()
    {
        GenerateSpeedRange();
        Reset();
    }

    private void GenerateSpeedRange()
    {
        float baseSpeed = UnityEngine.Random.Range(600f, 1000f);
        float tolerance = UnityEngine.Random.Range(50f, 100f);

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