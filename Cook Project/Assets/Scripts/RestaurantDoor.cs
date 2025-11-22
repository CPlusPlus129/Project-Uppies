using Cysharp.Threading.Tasks;
using UnityEngine;

public class RestaurantDoor : InteractableBase
{
    private IShiftSystem shiftSystem;
    [SerializeField]
    [Tooltip("Signal emitted when the player chooses to exit AfterShift via this door.")]
    private string afterShiftExitSignalId = "after_shift_exit";

    protected override void Awake()
    {
        base.Awake();
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>(); 
    }

    public override void Interact()
    {
        var state = shiftSystem.currentState.Value;
        if(state != ShiftSystem.ShiftState.AfterShift && state != ShiftSystem.ShiftState.None)
        {
            WorldBroadcastSystem.Instance.Broadcast("You can only start next shift when your shift is off.");
            return;
        }

        if (state != ShiftSystem.ShiftState.AfterShift)
        {
            WorldBroadcastSystem.Instance.Broadcast("You haven't completed the shift requirements yet!");
            return;
        }

        if (shiftSystem.HasTasksRequiredBeforeShiftStarts())
        {
            WorldBroadcastSystem.Instance.Broadcast("You must complete all required tasks before starting the next shift!", 4f);
            return;
        }

        EmitAfterShiftExitSignal();
        if (shiftSystem.IsAfterShiftReadyForNextShift)
        {
            shiftSystem.StartNextShift();
        }
        else
        {
            shiftSystem.RestartCurrentShift();
        }
    }

    private void EmitAfterShiftExitSignal()
    {
        var signalId = string.IsNullOrWhiteSpace(afterShiftExitSignalId) ? "after_shift_exit" : afterShiftExitSignalId.Trim();
        GameFlow.Instance?.Signal(signalId);
    }
}
