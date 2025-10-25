using System;
using UnityEngine;

/// <summary>
/// Defines the type of stat that can be upgraded
/// </summary>
public enum StatType
{
    MaxHP,
    MaxStamina,
    StaminaRecoverySpeed,
    MaxLight,
    LightRecoverySpeed,
    LightCostReduction
}

/// <summary>
/// Represents a single stat upgrade that can be purchased from the shop
/// </summary>
[Serializable]
public class StatUpgradeData
{
    [Header("Upgrade Info")]
    public string upgradeId;
    public string upgradeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("Stat Modification")]
    public StatType statType;
    public float upgradeValue;
    public bool isPercentage;

    [Header("Shop Settings")]
    public int price;
    public int maxPurchases = 1;

    /// <summary>
    /// Returns a formatted string describing the stat effect
    /// </summary>
    public string GetStatEffectString()
    {
        string prefix = upgradeValue > 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        string statName = GetStatDisplayName();
        
        return $"{statName}: {prefix}{upgradeValue}{suffix}";
    }

    /// <summary>
    /// Gets a user-friendly display name for the stat type
    /// </summary>
    private string GetStatDisplayName()
    {
        return statType switch
        {
            StatType.MaxHP => "Max HP",
            StatType.MaxStamina => "Max Stamina",
            StatType.StaminaRecoverySpeed => "Stamina Recovery",
            StatType.MaxLight => "Max Light",
            StatType.LightRecoverySpeed => "Light Recovery",
            StatType.LightCostReduction => "Light Cost Reduction",
            _ => statType.ToString()
        };
    }
}
