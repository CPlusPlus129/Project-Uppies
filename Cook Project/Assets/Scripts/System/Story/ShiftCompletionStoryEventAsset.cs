using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftCompletionStoryEvent", menuName = "Game Flow/Shift Events/Shift Completion Event")]
public sealed class ShiftCompletionStoryEventAsset : StoryEventAsset, IBackgroundStoryEvent
{
    private enum ShiftOutcome
    {
        Success,
        FailureTime,
        FailureVip,
        FailureMoney
    }

    [Serializable]
    private struct StoryOutcomeTrigger
    {
        [SerializeField]
        [Tooltip("Optional story sequence to enqueue when this outcome occurs.")]
        private StorySequenceAsset sequence;

        [SerializeField]
        [Tooltip("Optional single story event to enqueue when no sequence is provided.")]
        private StoryEventAsset storyEvent;

        public bool IsAssigned => sequence != null || storyEvent != null;

        public bool TryEnqueue(GameFlow flow, bool insertAtFront, StorySequenceAsset sourceSequence)
        {
            if (flow == null || !IsAssigned)
            {
                return false;
            }

            if (sequence != null)
            {
                flow.EnqueueSequence(sequence, insertAtFront);
                return true;
            }

            // Enqueue standalone events without tying them to the originating sequence so
            // they aren't misidentified as that sequence's "last" event (which would cause
            // premature chaining / queue restarts).
            flow.EnqueueEvent(storyEvent, insertAtFront, null);
            return true;
        }
    }

    [Header("Outcome Routing")]
    [SerializeField]
    private StoryOutcomeTrigger onSuccess;

    [SerializeField]
    private StoryOutcomeTrigger onFailureTime;

    [SerializeField]
    private StoryOutcomeTrigger onFailureVip;

    [SerializeField]
    private StoryOutcomeTrigger onFailureMoney;

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Automatically enqueue the configured follow-up content when the outcome resolves.")]
    private bool autoEnqueueOutcome = true;

    [SerializeField]
    [Tooltip("Insert follow-up content at the front of the story queue.")]
    private bool insertFollowUpAtFront = true;

    [SerializeField]
    [Tooltip("Process this event without blocking the story queue while it waits for shift completion.")]
    private bool runInBackground = true;
    public bool RunInBackground => runInBackground;
    public bool BlockSourceSequence => true;

    [SerializeField]
    [Tooltip("Log shift state transitions for debugging.")]
    private bool debugLogging = false;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var shiftSystem = await context.GetServiceAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogError($"[{nameof(ShiftCompletionStoryEventAsset)}] Unable to resolve IShiftSystem. Completing with failure.");
            return StoryEventResult.Failed("Shift system unavailable.");
        }

        var questService = context.IsServiceReady<IQuestService>() ? await context.GetServiceAsync<IQuestService>() : null;
        var shiftData = Database.Instance?.shiftData;
        var capturedShiftNumber = shiftSystem.shiftNumber.Value;
        var shiftDefinition = GetShiftDefinition(shiftData, capturedShiftNumber);

        var disposables = new CompositeDisposable();
        var overtimeDetected = shiftSystem.currentState.Value == ShiftSystem.ShiftState.Overtime;
        var moneyFailureDetected = false;
        var maxNegativeDebt = shiftData != null ? shiftData.maxNegativeDebt : int.MinValue;

        var terminalStateTcs = new UniTaskCompletionSource<ShiftSystem.ShiftState>();
        var initialState = shiftSystem.currentState.Value;

        if (IsTerminalState(initialState))
        {
            if (initialState == ShiftSystem.ShiftState.AfterShift)
            {
                // already in afterShift = success
                var outcome = ResolveOutcome(initialState, false, false);
                if (autoEnqueueOutcome)
                {
                    TryQueueOutcome(outcome, context.Flow, context.Runtime?.SourceSequence);
                }
                return StoryEventResult.Completed($"Shift already ended: {outcome}");
            }
            else
            {
                // GaveOver 
                return StoryEventResult.Failed("Shift already ended in failure state.");
            }
        }

        var sawStateChange = false;
        var sawActiveShift = initialState == ShiftSystem.ShiftState.InShift || initialState == ShiftSystem.ShiftState.Overtime;
        var sawNonTerminal = !IsTerminalState(initialState);
        if (debugLogging)
        {
            Debug.Log($"[{nameof(ShiftCompletionStoryEventAsset)}] subscribed. initialState={initialState}, sawActiveShift={sawActiveShift}, sawNonTerminal={sawNonTerminal}", this);
        }

        shiftSystem.currentState
            .Subscribe(state =>
            {
                if (state == ShiftSystem.ShiftState.Overtime)
                {
                    overtimeDetected = true;
                }

                if (state != initialState)
                {
                    sawStateChange = true;
                }

                if (state == ShiftSystem.ShiftState.InShift || state == ShiftSystem.ShiftState.Overtime)
                {
                    sawActiveShift = true;
                }

                if (!IsTerminalState(state))
                {
                    sawNonTerminal = true;
                }

                if (debugLogging)
                {
                    Debug.Log($"[{nameof(ShiftCompletionStoryEventAsset)}] state={state}, sawChange={sawStateChange}, sawActive={sawActiveShift}, sawNonTerminal={sawNonTerminal}", this);
                }

                if (sawActiveShift && sawNonTerminal && sawStateChange && IsTerminalState(state))
                {
                    terminalStateTcs.TrySetResult(state);
                }
            })
            .AddTo(disposables);

        var playerStats = PlayerStatSystem.Instance;
        if (playerStats != null && shiftData != null)
        {
            playerStats.Money
                .Subscribe(value =>
                {
                    if (value <= maxNegativeDebt)
                    {
                        moneyFailureDetected = true;
                    }
                })
                .AddTo(disposables);
        }

        try
        {
            var finalState = await terminalStateTcs.Task.AttachExternalCancellation(cancellationToken);

            var outcome = ResolveOutcome(finalState, overtimeDetected, moneyFailureDetected);

            if (autoEnqueueOutcome)
            {
                var enqueued = TryQueueOutcome(outcome, context.Flow, context.Runtime?.SourceSequence);
                if (!enqueued)
                {
                    Debug.LogWarning($"[{nameof(ShiftCompletionStoryEventAsset)}] Outcome '{outcome}' resolved but no follow-up content was configured.");
                }
            }

            var outcomeMessage = $"Shift outcome resolved: {outcome}";
            if (outcome == ShiftOutcome.Success)
            {
                return StoryEventResult.Completed(outcomeMessage);
            }

            // Let the flow know we failed so GameFlow will not auto-chain the success sequence.
            return StoryEventResult.Failed(outcomeMessage);
        }
        finally
        {
            disposables.Dispose();
        }
    }

    private bool TryQueueOutcome(ShiftOutcome outcome, GameFlow flow, StorySequenceAsset sourceSequence)
    {
        return outcome switch
        {
            ShiftOutcome.Success => onSuccess.TryEnqueue(flow, insertFollowUpAtFront, sourceSequence),
            ShiftOutcome.FailureTime => onFailureTime.TryEnqueue(flow, insertFollowUpAtFront, sourceSequence),
            ShiftOutcome.FailureVip => onFailureVip.TryEnqueue(flow, insertFollowUpAtFront, sourceSequence),
            ShiftOutcome.FailureMoney => onFailureMoney.TryEnqueue(flow, insertFollowUpAtFront, sourceSequence),
            _ => false
        };
    }

    private static bool IsTerminalState(ShiftSystem.ShiftState state)
    {
        switch (state)
        {
            case ShiftSystem.ShiftState.InShift:
            case ShiftSystem.ShiftState.Overtime:
            case ShiftSystem.ShiftState.None:
                return false;
            default:
                return true;
        }
    }

    private static ShiftOutcome ResolveOutcome(
        ShiftSystem.ShiftState terminalState,
        bool overtimeDetected,
        bool moneyFailureDetected)
    {
        if (terminalState == ShiftSystem.ShiftState.AfterShift)
        {
            return ShiftOutcome.Success;
        }

        if (moneyFailureDetected)
        {
            return ShiftOutcome.FailureMoney;
        }

        return overtimeDetected ? ShiftOutcome.FailureTime : ShiftOutcome.FailureTime;
    }

    private static ShiftData.Shift GetShiftDefinition(ShiftData data, int shiftNumber)
    {
        if (data?.shifts == null || shiftNumber < 0 || shiftNumber >= data.shifts.Length)
        {
            return null;
        }

        return data.shifts[shiftNumber];
    }
}
