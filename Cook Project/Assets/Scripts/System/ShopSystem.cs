using R3;
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : SimpleSingleton<ShopSystem>
{
    public Subject<IReadOnlyList<ShopItem>> OnShopItemsUpdated = new Subject<IReadOnlyList<ShopItem>>();
    
    private List<ShopItem> inStock = new List<ShopItem>();
    private Dictionary<string, int> purchaseCount = new Dictionary<string, int>();
    private StatUpgradeConfig upgradeConfig;

    public void Initialize(StatUpgradeConfig config)
    {
        upgradeConfig = config;
        RefreshShopItems();
    }

    public IReadOnlyList<ShopItem> GetShopItems() => inStock;

    /// <summary>
    /// Refreshes the shop inventory with random stat upgrades
    /// </summary>
    public void RefreshShopItems()
    {
        if (upgradeConfig == null)
        {
            Debug.LogWarning("StatUpgradeConfig not set in ShopSystem. Cannot refresh shop items.");
            return;
        }

        inStock.Clear();
        
        // Get all available upgrades that haven't been maxed out
        List<StatUpgradeData> availableUpgrades = new List<StatUpgradeData>();
        foreach (var upgrade in upgradeConfig.AvailableUpgrades)
        {
            int timesPurchased = GetPurchaseCount(upgrade.upgradeId);
            if (timesPurchased < upgrade.maxPurchases)
            {
                availableUpgrades.Add(upgrade);
            }
        }

        // Pick random upgrades for the shop (max 6 items)
        int numItemsToShow = Mathf.Min(6, availableUpgrades.Count);
        if (numItemsToShow > 0)
        {
            var picked = RandomHelper.PickWithoutReplacement(availableUpgrades.ToArray(), numItemsToShow);
            foreach (var upgrade in picked)
            {
                int remainingStock = upgrade.maxPurchases - GetPurchaseCount(upgrade.upgradeId);
                inStock.Add(new ShopItem 
                { 
                    itemId = upgrade.upgradeId,
                    price = upgrade.price,
                    stock = remainingStock,
                    upgradeData = upgrade
                });
            }
        }

        OnShopItemsUpdated.OnNext(inStock);
    }

    /// <summary>
    /// Attempts to purchase an item from the shop and apply the upgrade
    /// </summary>
    public bool PurchaseItem(string itemId)
    {
        var item = inStock.Find(i => i.itemId == itemId);
        if (item == null || item.stock <= 0)
        {
            Debug.LogWarning($"Item {itemId} not available for purchase");
            return false;
        }

        // Check if player has enough money
        if (item.price > PlayerStatSystem.Instance.Money.Value)
        {
            Debug.Log($"Not enough money to purchase {itemId}. Need: {item.price}, Have: {PlayerStatSystem.Instance.Money.Value}");
            return false;
        }

        // Deduct money
        PlayerStatSystem.Instance.Money.Value -= item.price;
        
        // Apply the upgrade
        if (item.upgradeData != null)
        {
            ApplyStatUpgrade(item.upgradeData);
        }

        // Update stock and purchase count
        item.stock--;
        IncrementPurchaseCount(itemId);
        
        Debug.Log($"Successfully purchased {itemId}");
        OnShopItemsUpdated.OnNext(inStock);
        return true;
    }

    /// <summary>
    /// Applies a stat upgrade to the PlayerStatSystem
    /// </summary>
    private void ApplyStatUpgrade(StatUpgradeData upgrade)
    {
        var playerStats = PlayerStatSystem.Instance;
        float upgradeAmount = upgrade.upgradeValue;

        switch (upgrade.statType)
        {
            case StatType.MaxHP:
                if (upgrade.isPercentage)
                {
                    int increase = Mathf.RoundToInt(playerStats.MaxHP.Value * (upgradeAmount / 100f));
                    playerStats.MaxHP.Value += increase;
                    playerStats.CurrentHP.Value += increase;
                }
                else
                {
                    playerStats.MaxHP.Value += Mathf.RoundToInt(upgradeAmount);
                    playerStats.CurrentHP.Value += Mathf.RoundToInt(upgradeAmount);
                }
                Debug.Log($"Max HP upgraded to {playerStats.MaxHP.Value}");
                break;

            case StatType.MaxStamina:
                if (upgrade.isPercentage)
                {
                    float increase = playerStats.MaxStamina.Value * (upgradeAmount / 100f);
                    playerStats.MaxStamina.Value += increase;
                    playerStats.CurrentStamina.Value += increase;
                }
                else
                {
                    playerStats.MaxStamina.Value += upgradeAmount;
                    playerStats.CurrentStamina.Value += upgradeAmount;
                }
                Debug.Log($"Max Stamina upgraded to {playerStats.MaxStamina.Value}");
                break;

            case StatType.StaminaRecoverySpeed:
                if (upgrade.isPercentage)
                {
                    float increase = playerStats.StaminaRecoverySpeed.Value * (upgradeAmount / 100f);
                    playerStats.StaminaRecoverySpeed.Value += increase;
                }
                else
                {
                    playerStats.StaminaRecoverySpeed.Value += upgradeAmount;
                }
                Debug.Log($"Stamina Recovery Speed upgraded to {playerStats.StaminaRecoverySpeed.Value}");
                break;

            case StatType.MaxLight:
                if (upgrade.isPercentage)
                {
                    float increase = playerStats.MaxLight.Value * (upgradeAmount / 100f);
                    playerStats.MaxLight.Value += increase;
                    playerStats.CurrentLight.Value += increase;
                }
                else
                {
                    playerStats.MaxLight.Value += upgradeAmount;
                    playerStats.CurrentLight.Value += upgradeAmount;
                }
                Debug.Log($"Max Light upgraded to {playerStats.MaxLight.Value}");
                break;

            case StatType.LightRecoverySpeed:
                if (upgrade.isPercentage)
                {
                    float increase = playerStats.LightRecoverySpeed.Value * (upgradeAmount / 100f);
                    playerStats.LightRecoverySpeed.Value += increase;
                }
                else
                {
                    playerStats.LightRecoverySpeed.Value += upgradeAmount;
                }
                Debug.Log($"Light Recovery Speed upgraded to {playerStats.LightRecoverySpeed.Value}");
                break;

            case StatType.LightCostReduction:
                if (upgrade.isPercentage)
                {
                    float reduction = playerStats.LightCostPerShot.Value * (upgradeAmount / 100f);
                    playerStats.LightCostPerShot.Value -= reduction;
                }
                else
                {
                    playerStats.LightCostPerShot.Value -= upgradeAmount;
                }
                // Ensure light cost doesn't go below a minimum value
                playerStats.LightCostPerShot.Value = Mathf.Max(playerStats.LightCostPerShot.Value, 1f);
                Debug.Log($"Light Cost Per Shot reduced to {playerStats.LightCostPerShot.Value}");
                break;
        }
    }

    /// <summary>
    /// Gets the number of times an upgrade has been purchased
    /// </summary>
    private int GetPurchaseCount(string upgradeId)
    {
        return purchaseCount.ContainsKey(upgradeId) ? purchaseCount[upgradeId] : 0;
    }

    /// <summary>
    /// Increments the purchase count for an upgrade
    /// </summary>
    private void IncrementPurchaseCount(string upgradeId)
    {
        if (purchaseCount.ContainsKey(upgradeId))
        {
            purchaseCount[upgradeId]++;
        }
        else
        {
            purchaseCount[upgradeId] = 1;
        }
    }

    /// <summary>
    /// Resets all purchase counts (useful for testing or new game)
    /// </summary>
    public void ResetPurchaseCounts()
    {
        purchaseCount.Clear();
        RefreshShopItems();
    }
}

public class ShopItem
{
    public string itemId;
    public int price;
    public int stock;
    public StatUpgradeData upgradeData;
}
