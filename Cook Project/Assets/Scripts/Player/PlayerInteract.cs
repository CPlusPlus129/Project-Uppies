using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public IInventorySystem inventorySystem { get; set; }

    public void UpdateCurrentInteractableTarget(Camera cam)
    {
        var currentInteractableTarget = PlayerStatSystem.Instance.CurrentInteractableTarget;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            currentInteractableTarget.Value = null;
            return;
        }

        if (hit.collider.TryGetComponent(out IInteractable interactable))
            currentInteractableTarget.Value = interactable;
        else
            currentInteractableTarget.Value = null;
    }

    public void Interact(Camera cam)
    {
        var currentInteractableTarget = PlayerStatSystem.Instance.CurrentInteractableTarget;
        if (currentInteractableTarget.Value == null)
            return;

        IInteractable interactable = currentInteractableTarget.Value;
        interactable.Interact();

        if (interactable is ItemBase item)
        {
            if (inventorySystem.AddItem(item))
                item.gameObject.SetActive(false);
        }

        if (interactable is Customer customer)
        {
            var heldItem = inventorySystem.GetSelectedItem();
            if (heldItem != null && customer.CanReceiveMeal(heldItem))
            {
                customer.ReceiveMeal(heldItem);
            }
        }

        if (interactable is FoodSource foodSource && !inventorySystem.IsInventoryFull())
        {
            var itemPrefab = Database.Instance.itemPrefabData.GetItemByName(foodSource.ItemName);
            var foodObj = itemPrefab != null ? Instantiate(itemPrefab) : null;
            if (foodObj != null && inventorySystem.AddItem(foodObj))
            {
                // Optionally, you can add some feedback here, like a sound or animation
            }
        }

    }
}