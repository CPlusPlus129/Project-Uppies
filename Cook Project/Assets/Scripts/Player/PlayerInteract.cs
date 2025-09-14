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

        if (hit.collider.TryGetComponent(out IInteractable interactable))
            interactable.Interact();

        if (hit.collider.TryGetComponent(out ItemBase item))
        {
            if (InventorySystem.Instance.AddItem(item))
                item.gameObject.SetActive(false);
        }
    }
}