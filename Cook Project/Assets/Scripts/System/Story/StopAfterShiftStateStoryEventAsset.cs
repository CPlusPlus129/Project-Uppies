using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "StopAfterShiftState", menuName = "Game Flow/Shift Events/Stop After Shift State")]
public sealed class StopAfterShiftStateStoryEventAsset : StoryEventAsset
{
    private const string DefaultDoorSignalId = "after_shift_exit";

    [SerializeField]
    [Tooltip("Signal emitted by RestaurantDoor when the player chooses to leave AfterShift.")]
    private string doorSignalId = DefaultDoorSignalId;

    [SerializeField]
    [Tooltip("Time in seconds to show the task as completed before removing it.")]
    private float taskRemovalDelay = 2.0f;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var shiftSystem = await context.GetServiceAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogError("[StopAfterShiftStateStoryEvent] Unable to resolve IShiftSystem.");
            return StoryEventResult.Failed("Shift system unavailable.");
        }

        if (shiftSystem.currentState.Value != ShiftSystem.ShiftState.AfterShift)
        {
            return StoryEventResult.Completed("After shift already ended.");
        }

        var signalId = string.IsNullOrWhiteSpace(doorSignalId) ? DefaultDoorSignalId : doorSignalId.Trim();

        await context.WaitForSignalAsync(signalId, cancellationToken);

        TaskManager.Instance.CompleteTask("AfterShiftTask", taskRemovalDelay);

        return StoryEventResult.Completed("After shift exit signal received.");
    }
}
