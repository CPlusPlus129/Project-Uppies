using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftStartStoryEvent", menuName = "Game Flow/Story Events/Shift Start Event")]
public class ShiftStartStoryEventAsset : StoryEventAsset
{
    private enum ShiftSelectionMode
    {
        StartNextShift,
        RestartCurrentShift,
        StartSpecificShift
    }

    [SerializeField]
    [Tooltip("Determines how the target shift is chosen when this event runs.")]
    private ShiftSelectionMode selectionMode = ShiftSelectionMode.StartNextShift;

    [SerializeField]
    [Tooltip("Shift index to use when Selection Mode is 'Start Specific Shift'.")]
    private int specificShiftIndex = 0;

    [SerializeField]
    [Tooltip("Clamp the specific shift index to the number of shifts defined in ShiftData.")]
    private bool clampSpecificIndexToData = true;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var shiftSystem = await context.GetServiceAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogError($"[{nameof(ShiftStartStoryEventAsset)}] Could not resolve IShiftSystem.");
            return StoryEventResult.Failed("Shift system unavailable.");
        }

        switch (selectionMode)
        {
            case ShiftSelectionMode.StartNextShift:
                shiftSystem.StartNextShift();
                break;
            case ShiftSelectionMode.RestartCurrentShift:
                shiftSystem.RestartCurrentShift();
                break;
            case ShiftSelectionMode.StartSpecificShift:
                var targetIndex = specificShiftIndex;
                if (clampSpecificIndexToData)
                {
                    var data = Database.Instance?.shiftData;
                    if (data?.shifts != null && data.shifts.Length > 0)
                    {
                        targetIndex = Mathf.Clamp(targetIndex, 0, data.shifts.Length - 1);
                    }
                    else
                    {
                        targetIndex = Mathf.Max(0, targetIndex);
                    }
                }
                shiftSystem.RestartShift(targetIndex);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return StoryEventResult.Completed($"Shift start triggered via {selectionMode}.");
    }
}
