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
        SetPlayerPosition(new Vector3(-18.25f, 11.5f, -73f));
    }
    #endregion

    private void SetPlayerPosition(Vector3 position)
    {
        var player = GetPlayer();
        if (player == null)
            return;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return;

        cc.enabled = false;
        cc.transform.position = position;
        cc.enabled = true;
    }

    private PlayerController GetPlayer()
    {
        return UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    }
}