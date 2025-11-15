using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : IInventorySystem
{
    public Subject<IReadOnlyList<ItemBase>> OnInventoryChanged { get; } = new Subject<IReadOnlyList<ItemBase>>();
    public ReactiveProperty<int> SelectedIndex { get; } = new ReactiveProperty<int>(0);
    public ReactiveProperty<int> SlotCount { get; } = new ReactiveProperty<int>(4);
    private readonly List<ItemBase> slots = new List<ItemBase>();
    private readonly Dictionary<string, int> itemCache = new Dictionary<string, int>();
    public ItemBase GetSelectedItem() => slots.Count == 0 ? null : slots[Mathf.Clamp(SelectedIndex.Value, 0, slots.Count - 1)];
    public IReadOnlyList<ItemBase> GetAllItems() => slots;

    public async UniTask Init()
    {
        var playerStats = PlayerStatSystem.Instance;
        if (playerStats != null)
        {
            var initialSize = Mathf.Max(0, playerStats.InventorySize.Value);
            if (SlotCount.Value != initialSize)
            {
                SlotCount.Value = initialSize;
            }

            playerStats.InventorySize
                .Subscribe(size =>
                {
                    var clamped = Mathf.Max(0, size);
                    if (SlotCount.Value != clamped)
                    {
                        SlotCount.Value = clamped;
                    }
                })
                .AddTo(disposables);
        }

        InitSlots();

        SlotCount
            .Subscribe(count => ResizeSlots(count))
            .AddTo(disposables);

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
        if (slots.Count == 0)
            return;

        if (index == -1) index = slots.Count - 1;
        else if (index == slots.Count) index = 0;
        if (index >= 0 && index < slots.Count)
            SelectedIndex.Value = index;
    }

    public void RemoveSelectedItem()
    {
        if (slots.Count == 0)
            return;

        var originalItem = slots[SelectedIndex.Value];
        slots[SelectedIndex.Value] = null;
        if (originalItem != null)
        {
            UpdateItemCache(originalItem.ItemName, CollectionChangeType.Remove);
        }
        OnInventoryChanged.OnNext(slots);
    }

    public void ClearInventory()
    {
        if (slots.Count == 0)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] = null;
        }

        UpdateItemCache(string.Empty, CollectionChangeType.RemoveAll);
        SelectedIndex.Value = 0;
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
        ResizeSlots(SlotCount.Value);
    }

    private void ResizeSlots(int desiredCount)
    {
        desiredCount = Mathf.Max(0, desiredCount);
        if (slots.Count == desiredCount)
            return;

        if (slots.Count < desiredCount)
        {
            slots.AddRange(new ItemBase[desiredCount - slots.Count]);
        }
        else
        {
            for (int i = slots.Count - 1; i >= desiredCount; i--)
            {
                if (slots[i] != null)
                {
                    UpdateItemCache(slots[i].ItemName, CollectionChangeType.Remove);
                }
                slots.RemoveAt(i);
            }
        }

        if (slots.Count == 0)
        {
            SelectedIndex.Value = 0;
        }
        else
        {
            var clampedIndex = Mathf.Clamp(SelectedIndex.Value, 0, slots.Count - 1);
            if (clampedIndex != SelectedIndex.Value)
            {
                SelectedIndex.Value = clampedIndex;
            }
        }

        var playerStats = PlayerStatSystem.Instance;
        if (playerStats != null && playerStats.InventorySize.Value != slots.Count)
        {
            playerStats.InventorySize.Value = slots.Count;
        }

        OnInventoryChanged.OnNext(slots);
    }

    enum CollectionChangeType
    {
        Add,
        Remove,
        RemoveAll
    }

    private readonly CompositeDisposable disposables = new CompositeDisposable();
}
