using UnityEngine;

public class RestaurantDoor : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if(ShiftSystem.Instance.currentState.Value != ShiftSystem.ShiftState.AfterShift)
        {
            WorldBroadcastSystem.Instance.Broadcast("You can only start next shift when your shift is off.");
            return;
        }

        if (ShiftSystem.Instance.IsCurrentShiftQuestCompleted())
            ShiftSystem.Instance.StartNextShift();
        else
            WorldBroadcastSystem.Instance.Broadcast("You haven't completed the shift requirements yet!");
    }
}