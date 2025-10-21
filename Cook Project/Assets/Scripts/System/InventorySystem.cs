using R3;
using System.Collections.Generic;

public class InventorySystem : SimpleSingleton<InventorySystem>
{
    public Subject<ItemBase[]> OnInventoryChanged = new Subject<ItemBase[]>();
    public ReactiveProperty<int> SelectedIndex { get; } = new ReactiveProperty<int>(0);
    private const int SlotCount = 4;
    private ItemBase[] slots = new ItemBase[SlotCount];
    private Dictionary<string, int> itemCache = new Dictionary<string, int>();
    public ItemBase GetSelectedItem() => slots[SelectedIndex.Value];
    public IReadOnlyList<ItemBase> GetAllItems() => slots;

    public bool AddItem(ItemBase item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;                
                UpdateItemCache(item.ItemName, CollectionChangeType.Add);
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
                UpdateItemCache(itemName, CollectionChangeType.Remove);
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
        var originalItem = slots[SelectedIndex.Value];
        slots[SelectedIndex.Value] = null;
        if (originalItem != null)
        {
            UpdateItemCache(originalItem.ItemName, CollectionChangeType.Remove);
        }
        OnInventoryChanged.OnNext(slots);
    }

    public bool IsInventoryFull()
    {
        foreach (var slot in slots)
        {
            if (slot == null) return false;
        }
        return true;
    }

    public bool HasItem(string itemName)
    {
        return itemCache.TryGetValue(itemName, out int itemCount) && itemCount > 0;
    }

    private void UpdateItemCache(string itemName, CollectionChangeType changeType)
    {
        switch (changeType)
        {
            case CollectionChangeType.Add:
                if (!itemCache.ContainsKey(itemName))
                    itemCache[itemName] = 0;

                itemCache[itemName]++;
                break;
            case CollectionChangeType.Remove:
                if (itemCache.ContainsKey(itemName))
                {
                    itemCache[itemName]--;
                    if (itemCache[itemName] <= 0)
                        itemCache.Remove(itemName);
                }
                break;
            case CollectionChangeType.RemoveAll:
                itemCache.Clear();
                break;
            default:
                break;
        }
    }


    enum CollectionChangeType
    {
        Add,
        Remove,
        RemoveAll
    }
}