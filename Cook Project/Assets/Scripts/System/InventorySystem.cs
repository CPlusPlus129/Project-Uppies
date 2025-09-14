using System;

public class InventorySystem : SimpleSingleton<InventorySystem>
{
    public event Action<ItemBase[]> OnInventoryChanged;
    private const int SlotCount = 4;
    private ItemBase[] slots = new ItemBase[SlotCount];
    private int selectedIndex = 0;

    public ItemBase GetSelectedItem() => slots[selectedIndex];

    public bool AddItem(ItemBase item)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                OnInventoryChanged?.Invoke(slots);
                return true;
            }
        }
        return false;
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < SlotCount)
            selectedIndex = index;
    }

    public void RemoveSelectedItem()
    {
        slots[selectedIndex] = null;
        OnInventoryChanged?.Invoke(slots);
    }
}