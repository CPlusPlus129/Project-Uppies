using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class PickupableItem : ItemBase, IInteractable
{
    public virtual async void Interact()
    {
        // Wait for game initialization
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);

        // Get inventory system
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();

        // Try to add item to inventory
        if (inventorySystem.AddItem(this))
        {
            gameObject.SetActive(false);
        }
    }

    public virtual void ToggleOutline(bool isOn)
    {
        // Override in derived classes if outline behavior is needed
    }
}
