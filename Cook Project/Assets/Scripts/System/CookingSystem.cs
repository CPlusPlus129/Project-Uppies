using R3;
using UnityEngine;

public class CookingSystem : SimpleSingleton<CookingSystem>
{
    public ReactiveProperty<string> currentSelectedRecipe = new ReactiveProperty<string>("");
    
    // Store the pending meal item until minigame completion
    private ItemBase pendingMealItem = null;

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
            InventorySystem.Instance.RemoveItem(ingredient);
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
    public void CompleteCooking()
    {
        if (pendingMealItem == null)
        {
            Debug.LogError("CompleteCooking called but no pending meal item exists!");
            return;
        }

        // Add the meal to inventory
        if (InventorySystem.Instance.AddItem(pendingMealItem))
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
        var cache = InventorySystem.Instance.GetItemCache();
        foreach (var ingredient in recipe.ingredients)
        {
            if (!cache.TryGetValue(ingredient, out int itemCount) || itemCount <= 0)
                return false;
        }
        return true;
    }
}
