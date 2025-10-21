using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;

public class InventorySystem : IInventorySystem
{
    public Subject<IReadOnlyList<ItemBase>> OnInventoryChanged { get; } = new Subject<IReadOnlyList<ItemBase>>();
    public ReactiveProperty<int> SelectedIndex { get; } = new ReactiveProperty<int>(0);
    public ReactiveProperty<int> SlotCount { get; } = new ReactiveProperty<int>(4);
    private List<ItemBase> slots = new List<ItemBase>();
    private Dictionary<string, int> itemCache = new Dictionary<string, int>();
    public ItemBase GetSelectedItem() => slots[SelectedIndex.Value];
    public IReadOnlyList<ItemBase> GetAllItems() => slots;

    public async UniTask Init()
    {
        InitSlots();
        await UniTask.CompletedTask;
    }

    public bool AddItem(ItemBase item)
    {
        for (int i = 0; i < slots.Count; i++)
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
        for (int i = 0; i < slots.Count; i++)
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
        if (index == -1) index = SlotCount.Value - 1;
        else if (index == SlotCount.Value) index = 0;
        if (index >= 0 && index < SlotCount.Value)
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

    private void InitSlots()
    {
        slots.Clear();
        for (int i = 0; i < SlotCount.Value; i++)
        {
            slots.Add(null);
        }
    }

    enum CollectionChangeType
    {
        Add,
        Remove,
        RemoveAll
    }
}