using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SRDebugger;
using SRDebugger.Services;
using SRF;
using SRF.Service;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public partial class SROptions
{
    private bool _isInvincible = false;
    private bool _isInfiniteStamina = false;
    private bool _isInfiniteAmmo = false;

    #region General
    [Category("General")]
    [DisplayName("Test function")]
    public void Testt()
    {
        Debug.Log("Testt");
    }

    [Category("General")]
    [DisplayName("Give Every Ingredient")]
    public async void GiveEveryIngredient()
    {
        Debug.Log("GiveEveryIngredient");
        var i_arr = Database.Instance.recipeData.datas.SelectMany(x => x.ingredients).ToArray();
        PlayerStatSystem.Instance.InventorySize.Value = i_arr.Length;
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        foreach (var itemName in i_arr)
        {
            var itemPrefab = Database.Instance.itemPrefabData.GetItemByName(itemName);
            var itemObject = itemPrefab != null ? GameObject.Instantiate(itemPrefab) : null;
            if (itemObject != null)
                inventorySystem.AddItem(itemObject);
        }
    }

    [Category("General")]
    [DisplayName("Give Every Meal")]
    public async void GiveEveryMeal()
    {
        Debug.Log("GiveEveryMeal");
        var i_arr = Database.Instance.recipeData.datas.Select(x => x.mealName).ToArray();
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        var needToExpandCount = inventorySystem.GetUsedSlotCount + i_arr.Length - inventorySystem.SlotCount.CurrentValue;
        if (needToExpandCount > 0)
            PlayerStatSystem.Instance.InventorySize.Value += needToExpandCount;
        foreach (var itemName in i_arr)
        {
            var itemPrefab = Database.Instance.itemPrefabData.GetItemByName(itemName);
            var itemObject = itemPrefab != null ? GameObject.Instantiate(itemPrefab) : null;
            if (itemObject != null)
                inventorySystem.AddItem(itemObject);
        }
    }
    #endregion

    #region Cheat
    [Category("Cheat")]
    public bool IsInvincible
    {
        get => _isInvincible; set
        {
            _isInvincible = value;
            OnPropertyChanged(nameof(IsInvincible));
        }
    }

    [Category("Cheat")]
    public bool IsInfiniteStamina
    {
        get => _isInfiniteStamina; set
        {
            _isInfiniteStamina = value;
            OnPropertyChanged(nameof(IsInfiniteStamina));
        }
    }

    [Category("Cheat")]
    public bool IsInfiniteAmmo
    {
        get => _isInfiniteAmmo; set
        {
            _isInfiniteAmmo = value;
            OnPropertyChanged(nameof(IsInfiniteAmmo));
        }
    }

    [Category("Cheat")]
    [DisplayName("Add 10000 Money")]
    public void AddMoney()
    {
        PlayerStatSystem.Instance.Money.Value += 10000;
    }

    [Category("Cheat")]
    [DisplayName("Add 1 Item Slot")]
    public void AddItemSlot()
    {
        PlayerStatSystem.Instance.InventorySize.Value++;
    }

    [Category("Cheat")]
    [DisplayName("Refill Soul")]
    public void RefillSoul()
    {
        PlayerStatSystem.Instance.AddSouls(PlayerStatSystem.Instance.MaxSouls.CurrentValue);
    }

    [Category("Cheat")]
    [DisplayName("Skip To Shift End")]
    public async void SkipToShiftEnd()
    {
        var shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogWarning("[SROptions] Shift system unavailable.");
            return;
        }

        var state = shiftSystem.currentState.Value;
        switch (state)
        {
            case ShiftSystem.ShiftState.AfterShift:
                WorldBroadcastSystem.Instance?.Broadcast("Already off the clock.", 4f);
                return;
            case ShiftSystem.ShiftState.None:
            case ShiftSystem.ShiftState.GaveOver:
                WorldBroadcastSystem.Instance?.Broadcast("No active shift to skip.", 4f);
                return;
        }

        if (!shiftSystem.ForceCompleteActiveShift())
        {
            WorldBroadcastSystem.Instance?.Broadcast("Unable to fast forward this shift.", 4f);
            return;
        }

        WorldBroadcastSystem.Instance?.Broadcast("Shift fast forwarded. Quota, quests, and timers resolved.", 4f);
        if (SRDebug.Instance != null && SRDebug.Instance.IsDebugPanelVisible)
        {
            SRDebug.Instance.HideDebugPanel();
        }
    }
    #endregion

    #region Main Level
    [Category("Teleport")]
    [DisplayName("Home")]
    public void GoHome()
    {
        SetPlayerPosition(new Vector3(82f, 5.2f, 0.83f));
    }

    [Category("Teleport")]
    [DisplayName("StorageRoom1 Door")]
    public void GoStorageRoom1_Door()
    {
        SetPlayerPosition(new Vector3(7.3f, 5.2f, -31.9f));
    }

    [Category("Teleport")]
    [DisplayName("StorageRoom2 Door")]
    public void GoStorageRoom2_Door()
    {
        SetPlayerPosition(new Vector3(-51f, 5.4f, -4.5f));
    }

    [Category("Teleport")]
    [DisplayName("StorageRoom3 Door")]
    public void GoStorageRoom3_Door()
    {
        SetPlayerPosition(new Vector3(-18.4f, 12.8f, 71.3f));
    }
    #endregion

    private void SetPlayerPosition(Vector3 position)
    {
        var player = GetPlayer();
        if (player == null)
            return;

        player.Teleport(position);
    }

    private PlayerController GetPlayer()
    {
        return UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    }
}
