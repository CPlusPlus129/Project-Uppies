using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftCompletionStoryEvent", menuName = "Game Flow/Shift Events/Shift Completion Event")]
public sealed class ShiftCompletionStoryEventAsset : StoryEventAsset
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
        var questId = shiftDefinition?.questId;
        var vipRequired = shiftDefinition?.requiresVipCustomer ?? false;

        var disposables = new CompositeDisposable();
        var overtimeDetected = shiftSystem.currentState.Value == ShiftSystem.ShiftState.Overtime;
        var moneyFailureDetected = false;
        var maxNegativeDebt = shiftData != null ? shiftData.maxNegativeDebt : int.MinValue;

        var terminalStateTcs = new UniTaskCompletionSource<ShiftSystem.ShiftState>();
        var initialState = shiftSystem.currentState.Value;
        if (IsTerminalState(initialState))
        {
            terminalStateTcs.TrySetResult(initialState);
        }

        shiftSystem.currentState
            .Subscribe(state =>
            {
                if (state == ShiftSystem.ShiftState.Overtime)
                {
                    overtimeDetected = true;
                }

                if (IsTerminalState(state))
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

            var vipCompleted = true;
            if (vipRequired)
            {
                if (!string.IsNullOrWhiteSpace(questId) && questService != null)
                {
                    vipCompleted = questService.GetQuestStatus(questId) == QuestStatus.Completed;
                }
                else if (vipRequired)
                {
                    Debug.LogWarning($"[{nameof(ShiftCompletionStoryEventAsset)}] Shift {capturedShiftNumber} requires a VIP but no questId is configured. Assuming VIP completed.");
                    vipCompleted = true;
                }
            }

            var outcome = ResolveOutcome(finalState, vipRequired, vipCompleted, overtimeDetected, moneyFailureDetected);

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
        bool vipRequired,
        bool vipCompleted,
        bool overtimeDetected,
        bool moneyFailureDetected)
    {
        if (terminalState == ShiftSystem.ShiftState.AfterShift)
        {
            if (vipRequired && !vipCompleted)
            {
                return ShiftOutcome.FailureVip;
            }

            return ShiftOutcome.Success;
        }

        if (moneyFailureDetected)
        {
            return ShiftOutcome.FailureMoney;
        }

        if (vipRequired && !vipCompleted)
        {
            return ShiftOutcome.FailureVip;
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
