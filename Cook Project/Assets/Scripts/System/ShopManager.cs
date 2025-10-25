using UnityEngine;

/// <summary>
/// Manager component that initializes the ShopSystem with the StatUpgradeConfig
/// This should be placed in the scene to provide the shop configuration
/// </summary>
public class ShopManager : MonoBehaviour
{
    [SerializeField] private StatUpgradeConfig upgradeConfig;
    
    [Header("Settings")]
    [SerializeField] private bool refreshOnStart = true;

    private void Awake()
    {
        if (upgradeConfig == null)
        {
            Debug.LogError("StatUpgradeConfig is not assigned in ShopManager! Shop will not function properly.");
            return;
        }

        // Initialize the shop system with the upgrade configuration
        ShopSystem.Instance.Initialize(upgradeConfig);
    }

    private void Start()
    {
        if (refreshOnStart && upgradeConfig != null)
        {
            ShopSystem.Instance.RefreshShopItems();
        }
    }

    /// <summary>
    /// Manually refresh the shop items (can be called from Unity Events or other scripts)
    /// </summary>
    public void RefreshShop()
    {
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.RefreshShopItems();
        }
    }

    /// <summary>
    /// Reset all purchase counts and refresh the shop (useful for testing or new game)
    /// </summary>
    public void ResetShop()
    {
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.ResetPurchaseCounts();
        }
    }
}
