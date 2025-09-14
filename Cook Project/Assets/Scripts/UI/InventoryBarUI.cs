using UnityEngine;

public class InventoryBarUI : MonoBehaviour
{
    public InventorySlotUI[] slots;
    public GameObject selectedIndicator;

    private void Awake()
    {
        InventorySystem.Instance.OnInventoryChanged += UpdateInventory;
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= UpdateInventory;
    }

    public void UpdateInventory(ItemBase[] items)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Length)
            {
                slots[i].SetItem(items[i].ItemName);
            }
            else
            {
                slots[i].SetItem(null);
            }
        }
    }
}