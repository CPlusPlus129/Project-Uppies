using UnityEngine;
using R3;

public class InventoryBarUI : MonoBehaviour
{
    public InventorySlotUI[] slots;
    public GameObject selectedIndicator;

    private void Awake()
    {
        InventorySystem.Instance.OnInventoryChanged.Subscribe(UpdateInventory).AddTo(this);
        InventorySystem.Instance.SelectedIndex.Subscribe(OnSelectedIndexChanged).AddTo(this);
        ClearItems();
    }

    private void UpdateInventory(ItemBase[] items)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Length)
            {
                slots[i].SetItem(items[i]?.ItemName);
            }
            else
            {
                slots[i].SetItem(null);
            }
        }
    }

    private void ClearItems()
    {
        foreach (var slot in slots)
        {
            slot.SetItem(null);
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (index >= 0 && index < slots.Length)
        {
            selectedIndicator.transform.SetParent(slots[index].transform, false);
        }
    }
}