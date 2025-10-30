using UnityEngine;

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

        currentInteractableTarget.Value = nextTarget;
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
            if (foodObj != null)
            {
                var wasAdded = inventorySystem.AddItem(foodObj);
                if (wasAdded)
                {
                    foodObj.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(foodObj.gameObject);
                }
            }
        }

    }
}
