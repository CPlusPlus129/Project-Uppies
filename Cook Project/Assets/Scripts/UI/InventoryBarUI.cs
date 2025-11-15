using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

public class InventoryBarUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private InventorySlotUI slotPrefab;
    private IAssetLoader assetLoader;
    private IInventorySystem inventorySystem;
    private List<InventorySlotUI> slots = new List<InventorySlotUI>();

    public async UniTask Init()
    {
        slotPrefab.gameObject.SetActive(false);
        assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        inventorySystem.OnInventoryChanged.Subscribe(UpdateInventory).AddTo(this);
        inventorySystem.SelectedIndex.Subscribe(OnSelectedIndexChanged).AddTo(this);
        UpdateInventory(inventorySystem.GetAllItems());
        OnSelectedIndexChanged(0);
    }

    private void UpdateInventory(IReadOnlyList<ItemBase>items )
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (i < slots.Count)
            {
                slots[i].SetItem(item?.ItemName);
            }
            else
            {
                var newSlot = Instantiate(slotPrefab, slotPrefab.transform.parent);
                newSlot.gameObject.SetActive(true);
                slots.Add(newSlot);
                newSlot.assetLoader = assetLoader;
                newSlot.SetItem(item?.ItemName);
            }
        }
        if(slots.Count > items.Count)
        {
            for (int i = slots.Count - 1; i >= items.Count; i--)
            {
                var slot = slots[i];
                slots.RemoveAt(i);
                Destroy(slot);
            }
        }
    }

    private void ClearSlotItems()
    {
        foreach (var slot in slots)
        {
            slot.SetItem(null);
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            selectedIndicator.transform.SetParent(slots[index].transform, false);
        }
    }
}