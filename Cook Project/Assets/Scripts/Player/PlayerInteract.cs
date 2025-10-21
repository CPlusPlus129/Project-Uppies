using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public IInventorySystem inventorySystem { get; set; }

    public void Interact(Camera cam)
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
#if UNITY_EDITOR
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 1f);
#endif
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            return;

        if (hit.collider.TryGetComponent(out IInteractable interactable))
            interactable.Interact();

        if (hit.collider.TryGetComponent(out ItemBase item))
        {
            if (inventorySystem.AddItem(item))
                item.gameObject.SetActive(false);
        }

        if (hit.collider.TryGetComponent(out Customer customer))
        {
            var heldItem = inventorySystem.GetSelectedItem();
            if (heldItem != null && customer.CanReceiveMeal(heldItem))
            {
                customer.ReceiveMeal(heldItem);
            }
        }

        if (hit.collider.TryGetComponent(out FoodSource foodSource) && !    inventorySystem.IsInventoryFull())
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