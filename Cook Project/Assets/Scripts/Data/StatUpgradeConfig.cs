using UnityEngine;

/// <summary>
/// ScriptableObject that holds all available stat upgrades for the shop
/// </summary>
[CreateAssetMenu(fileName = "StatUpgradeConfig", menuName = "ScriptableObjects/StatUpgradeConfig")]
public class StatUpgradeConfig : ScriptableObject
{
    [SerializeField] private StatUpgradeData[] availableUpgrades;

    public StatUpgradeData[] AvailableUpgrades => availableUpgrades;

    /// <summary>
    /// Gets a specific upgrade by its ID
    /// </summary>
    public StatUpgradeData GetUpgradeById(string upgradeId)
    {
        foreach (var upgrade in availableUpgrades)
        {
            if (upgrade.upgradeId == upgradeId)
            {
                return upgrade;
            }
        }
        
        Debug.LogWarning($"Upgrade with ID '{upgradeId}' not found in StatUpgradeConfig");
        return null;
    }

    /// <summary>
    /// Gets a random upgrade from the available upgrades
    /// </summary>
    public StatUpgradeData GetRandomUpgrade()
    {
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogWarning("No upgrades available in StatUpgradeConfig");
            return null;
        }

        int randomIndex = Random.Range(0, availableUpgrades.Length);
        return availableUpgrades[randomIndex];
    }

    /// <summary>
    /// Validates that all upgrades have unique IDs
    /// </summary>
    private void OnValidate()
    {
        if (availableUpgrades == null) return;

        for (int i = 0; i < availableUpgrades.Length; i++)
        {
            if (availableUpgrades[i] == null) continue;
            
            for (int j = i + 1; j < availableUpgrades.Length; j++)
            {
                if (availableUpgrades[j] == null) continue;
                
                if (availableUpgrades[i].upgradeId == availableUpgrades[j].upgradeId)
                {
                    Debug.LogError($"Duplicate upgrade ID found: {availableUpgrades[i].upgradeId}");
                }
            }
        }
    }
}
