using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    public void Interact()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            return;
        var interactable = hit.collider.GetComponent<IInteractable>();
        interactable?.Interact();
        var item = hit.collider.GetComponent<ItemBase>();
        if (item != null)
        {
            if (InventorySystem.Instance.AddItem(item))
                item.gameObject.SetActive(false);
        }
    }
}