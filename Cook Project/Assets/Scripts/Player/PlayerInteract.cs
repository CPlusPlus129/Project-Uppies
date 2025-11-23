using UnityEngine;
using System.Collections.Generic;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public IInventorySystem inventorySystem { get; set; }

    public void UpdateCurrentInteractableTarget(Camera cam)
    {
        var currentInteractableTarget = PlayerStatSystem.Instance.CurrentInteractableTarget;
        var rayOrigin = cam.transform.position;
        var rayDirection = cam.transform.forward;
        IInteractable nextTarget = null;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                nextTarget = interactable;
            }
        }

        if (ReferenceEquals(currentInteractableTarget.Value, nextTarget))
        {
            return;
        }

        currentInteractableTarget.Value?.ToggleOutline(false);
        nextTarget?.ToggleOutline(true);
        currentInteractableTarget.Value = nextTarget;
    }

    public void Interact(Camera cam)
    {
        var currentInteractableTarget = PlayerStatSystem.Instance.CurrentInteractableTarget;
        if (currentInteractableTarget.Value == null)
            return;

        // Handle multiple interactables on the same object (e.g. Story Event triggers + Normal interaction)
        var targetInteractable = currentInteractableTarget.Value;
        IEnumerable<IInteractable> interactablesToTrigger;

        if (targetInteractable is Component component)
        {
            interactablesToTrigger = component.GetComponents<IInteractable>();
        }
        else
        {
            interactablesToTrigger = new[] { targetInteractable };
        }

        foreach (var interactable in interactablesToTrigger)
        {
            interactable.Interact();
        }
    }
}
