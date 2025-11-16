using Cysharp.Threading.Tasks;
using UnityEngine;

public class RestaurantDoor : MonoBehaviour, IInteractable
{
    private IShiftSystem shiftSystem;
    [SerializeField]
    [Tooltip("Signal emitted when the player chooses to exit AfterShift via this door.")]
    private string afterShiftExitSignalId = "after_shift_exit";

    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>(); 
    }

    public void Interact()
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
