using R3;
using System.Collections.Generic;

public class InventorySystem : SimpleSingleton<InventorySystem>
{
    public Subject<ItemBase[]> OnInventoryChanged = new Subject<ItemBase[]>();
    private const int SlotCount = 4;
    private ItemBase[] slots = new ItemBase[SlotCount];
    public ReactiveProperty<int> SelectedIndex { get; } = new ReactiveProperty<int>(0);
    public ItemBase GetSelectedItem() => slots[SelectedIndex.Value];
    public IReadOnlyList<ItemBase> GetAllItems() => slots;

    public bool AddItem(ItemBase item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                OnInventoryChanged.OnNext(slots);
                return true;
            }
        }
        return false;
    }

    public void RemoveItem(string itemName)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].ItemName == itemName)
            {
                slots[i] = null;
                OnInventoryChanged.OnNext(slots);
                return;
            }
        }
    }

    public void SelectSlot(int index)
    {
        if (index == -1) index = SlotCount - 1;
        else if (index == SlotCount) index = 0;
        if (index >= 0 && index < SlotCount)
            SelectedIndex.Value = index;
    }

    public void RemoveSelectedItem()
    {
        slots[SelectedIndex.Value] = null;
        OnInventoryChanged.OnNext(slots);
    }
}