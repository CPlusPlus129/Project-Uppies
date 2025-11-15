using R3;
using System.Collections.Generic;

public interface IInventorySystem : IGameService
{
    Subject<IReadOnlyList<ItemBase>> OnInventoryChanged { get; }
    ReactiveProperty<int> SelectedIndex { get; }
    ReactiveProperty<int> SlotCount { get; }
    ItemBase GetSelectedItem();
    IReadOnlyList<ItemBase> GetAllItems();
    bool AddItem(ItemBase item);
    void RemoveItem(string itemName);
    void SelectSlot(int index);
    void RemoveSelectedItem();
    bool IsInventoryFull();
    bool HasItem(string itemName);
    void ClearInventory();
}
