using System;

public enum StoryEventState
{
    Pending = 0,
    Waiting = 1,
    Running = 2,
    Completed = 3,
    Skipped = 4,
    Failed = 5,
    Cancelled = 6
}

public readonly struct StoryEventResult
{
    public StoryEventState FinalState { get; }
    public StorySequenceAsset NextSequence { get; }
    public bool PauseFlow { get; }
    public string Message { get; }

    public StoryEventResult(StoryEventState finalState, StorySequenceAsset nextSequence = null, bool pauseFlow = false, string message = null)
    {
        FinalState = finalState;
        NextSequence = nextSequence;
        PauseFlow = pauseFlow;
        Message = message;
    }

    public static StoryEventResult Completed(string message = null, StorySequenceAsset nextSequence = null)
        => new StoryEventResult(StoryEventState.Completed, nextSequence, false, message);

    public static StoryEventResult Skipped(string message = null)
        => new StoryEventResult(StoryEventState.Skipped, null, false, message);

    public static StoryEventResult Failed(string message = null)
        => new StoryEventResult(StoryEventState.Failed, null, false, message);

    public static StoryEventResult Pause(string message = null)
        => new StoryEventResult(StoryEventState.Waiting, null, true, message);

    public static StoryEventResult Cancelled(string message = null)
        => new StoryEventResult(StoryEventState.Cancelled, null, false, message);
}

public sealed class StoryEventRuntime
{
    public Guid RunId { get; } = Guid.NewGuid();
    public StoryEventAsset Asset { get; }
    public StorySequenceAsset SourceSequence { get; }
    public int SequenceIndex { get; }
    public int SequenceLength { get; }
    public bool IsLastEventInSequence => SequenceLength > 0 && SequenceIndex == SequenceLength - 1;
    public StoryEventState State { get; private set; }
    public DateTimeOffset EnqueuedAt { get; }
    public StoryEventResult? LastResult { get; private set; }

    internal StoryEventRuntime(StoryEventAsset asset, StorySequenceAsset sourceSequence, int sequenceIndex, int sequenceLength = 0)
    {
        Asset = asset;
        SourceSequence = sourceSequence;
        SequenceIndex = sequenceIndex;
        SequenceLength = sequenceLength;
        State = StoryEventState.Pending;
        EnqueuedAt = DateTimeOffset.UtcNow;
    }

    internal void SetState(StoryEventState state, StoryEventResult? result = null)
    {
        State = state;
        if (result.HasValue)
        {
            LastResult = result;
        }
    }

    public override string ToString()
    {
        var id = Asset != null ? Asset.EventId : "<null>";
        return $"StoryEventRuntime[{id}, State={State}, Sequence={SourceSequence?.name}]";
    }
}
