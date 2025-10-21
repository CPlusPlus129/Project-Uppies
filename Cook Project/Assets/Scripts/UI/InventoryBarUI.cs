using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryBarUI : MonoBehaviour
{
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private InventorySlotUI slotPrefab;
    private List<InventorySlotUI> slots = new List<InventorySlotUI>();

    private void Awake()
    {
        slotPrefab.gameObject.SetActive(false);
        InventorySystem.Instance.OnInventoryChanged.Subscribe(UpdateInventory).AddTo(this);
        InventorySystem.Instance.SelectedIndex.Subscribe(OnSelectedIndexChanged).AddTo(this);
        ClearItems();
        UpdateInventory(InventorySystem.Instance.GetAllItems().ToArray());
        OnSelectedIndexChanged(0);
    }

    private void UpdateInventory(ItemBase[] items)
    {
        for (int i = 0; i < items.Length; i++)
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
                newSlot.SetItem(item?.ItemName);
            }
        }
        if(slots.Count > items.Length)
        {
            for (int i = slots.Count - 1; i >= items.Length; i--)
            {
                var slot = slots[i];
                slots.RemoveAt(i);
                Destroy(slot);
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
        if (index >= 0 && index < slots.Count)
        {
            selectedIndicator.transform.SetParent(slots[index].transform, false);
        }
    }
}