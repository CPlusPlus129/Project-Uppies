using UnityEngine;

public class RestaurantDoor : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (ShiftSystem.Instance.currentState.Value == ShiftSystem.ShiftState.AfterShift
            && ShiftSystem.Instance.IsCurrentShiftQuestCompleted())
            ShiftSystem.Instance.StartNextShift();
        else
            Debug.Log("You haven't completed the shift requirements yet!"); //TODO UI popup
    }
}