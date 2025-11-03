using Cysharp.Threading.Tasks;
using UnityEngine;

public class RestaurantDoor : MonoBehaviour, IInteractable
{
    private IShiftSystem shiftSystem;

    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>(); 
    }

    public void Interact()
    {
        if(shiftSystem.currentState.Value != ShiftSystem.ShiftState.AfterShift)
        {
            WorldBroadcastSystem.Instance.Broadcast("You can only start next shift when your shift is off.");
            return;
        }

        if (shiftSystem.IsCurrentShiftQuestCompleted())
            shiftSystem.StartNextShift();
        else
            WorldBroadcastSystem.Instance.Broadcast("You haven't completed the shift requirements yet!");
    }
}