using Cysharp.Threading.Tasks;
using R3;
using System;
using UnityEngine;

public class CookingSystem : ICookingSystem
{
    public ReactiveProperty<string> currentSelectedRecipe { get; } = new ReactiveProperty<string>("");
    private readonly IInventorySystem inventorySystem;
    // Store the pending meal item until minigame completion
    private ItemBase pendingMealItem = null;
    public MinigamePerformance LastPerformance { get; private set; } = MinigamePerformance.Default;

    public CookingSystem(IInventorySystem inventorySystem)
    {
        this.inventorySystem = inventorySystem;
    }

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    public void Cook()
    {
        if (string.IsNullOrEmpty(currentSelectedRecipe.Value)) return;
        var r = Database.Instance.recipeData.GetRecipeByName(currentSelectedRecipe.Value);
        if (r == null)
        {
            Debug.LogError("Recipe not found: " + currentSelectedRecipe.Value);
            return;
        }
        if (!CheckPlayerHasIngredients(r))
        {
            Debug.LogError("Player does not have all ingredients for: " + currentSelectedRecipe.Value);
            return;
        }

        // Remove ingredients from inventory
        foreach (var ingredient in r.ingredients)
        {
            inventorySystem.RemoveItem(ingredient);
        }

        // Create the meal item but DON'T add it to inventory yet
        var mealPrefab = Database.Instance.itemPrefabData.GetItemByName(r.mealName);
        pendingMealItem = mealPrefab != null ? GameObject.Instantiate(mealPrefab) : null;
        if (pendingMealItem == null)
        {
            Debug.LogError("Meal item prefab not found: " + r.mealName);
            return;
        }
        pendingMealItem.gameObject.SetActive(false);

        Debug.Log($"Started cooking: {r.mealName}. Complete the minigame to finish!");

        // Close cooking UI and open minigame
        // The minigame will call CompleteCooking() when the player successfully completes the pattern
        UIRoot.Instance.GetUIComponent<CookingUI>().Close();
        UIRoot.Instance.GetUIComponent<MinigameUI>().Open();
    }

    /// <summary>
    /// Called when the minigame is successfully completed.
    /// Adds the pending meal to the player's inventory.
    /// This is called by the MinigamePanel when the player completes the pattern.
    /// </summary>
    public void CompleteCooking(MinigamePerformance performance)
    {
        if (pendingMealItem == null)
        {
            Debug.LogError("CompleteCooking called but no pending meal item exists!");
            return;
        }

        LastPerformance = performance;

        if (pendingMealItem.TryGetComponent<Meal>(out var mealComponent))
        {
            mealComponent.SetQuality(performance.QualityScore);
        }

        // Add the meal to inventory
        if (inventorySystem.AddItem(pendingMealItem))
        {
            Debug.Log($"Cooking completed! Added {pendingMealItem.ItemName} to inventory.");
        }
        else
        {
            Debug.LogWarning($"Failed to add {pendingMealItem.ItemName} to inventory - inventory might be full!");
            // If inventory is full, destroy the item or handle it differently
            GameObject.Destroy(pendingMealItem.gameObject);
        }

        // Clear the pending item
        pendingMealItem = null;
    }

    public bool CheckPlayerHasIngredients(Recipe recipe)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (!inventorySystem.HasItem(ingredient))
                return false;
        }
        return true;
    }

    public void Dispose()
    {
        currentSelectedRecipe?.Dispose();

        // Clean up pending meal if any
        if (pendingMealItem != null)
        {
            UnityEngine.Object.Destroy(pendingMealItem.gameObject);
            pendingMealItem = null;
        }
    }
}
