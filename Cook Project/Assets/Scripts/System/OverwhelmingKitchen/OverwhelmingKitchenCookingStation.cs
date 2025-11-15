using System.Linq;
using UnityEngine;

/// <summary>
/// Overwhelming Kitchen dedicated cooking station.
/// Uses the minigame's fake inventory to cook dishes and complete orders.
/// </summary>
public class OverwhelmingKitchenCookingStation : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OverwhelmingKitchenSystem kitchenSystem;

    [Header("Settings")]
    [SerializeField] private string currentSelectedRecipe;

    /// <summary>
    /// Set the currently selected recipe (called by UI or other systems)
    /// </summary>
    public void SetSelectedRecipe(string recipeName)
    {
        currentSelectedRecipe = recipeName;
        Debug.Log($"[OverwhelmingKitchenCookingStation] Selected recipe: {recipeName}");
    }

    public void Interact()
    {
        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenCookingStation] Kitchen system reference is null!");
            return;
        }

        if (kitchenSystem.CurrentState.Value != OverwhelmingKitchenState.Running)
        {
            Debug.LogWarning("[OverwhelmingKitchenCookingStation] Game is not running");
            return;
        }

        if (string.IsNullOrEmpty(currentSelectedRecipe))
        {
            Debug.LogWarning("[OverwhelmingKitchenCookingStation] No recipe selected");
            return;
        }

        // Try to cook
        TryCook(currentSelectedRecipe);
    }

    /// <summary>
    /// Try to cook a dish
    /// </summary>
    private void TryCook(string recipeName)
    {
        // Get recipe
        var recipe = Database.Instance.recipeData.GetRecipeByName(recipeName);
        if (recipe == null)
        {
            Debug.LogError($"[OverwhelmingKitchenCookingStation] Recipe not found: {recipeName}");
            return;
        }

        // Check if we have all ingredients
        if (!CheckHasAllIngredients(recipe))
        {
            Debug.LogWarning($"[OverwhelmingKitchenCookingStation] Missing ingredients for {recipeName}");
            WorldBroadcastSystem.Instance?.Broadcast($"Missing ingredients for {recipeName}!", 2f);
            return;
        }

        // Remove ingredients from fake inventory
        foreach (var ingredient in recipe.ingredients)
        {
            kitchenSystem.RemoveIngredient(ingredient);
        }

        Debug.Log($"[OverwhelmingKitchenCookingStation] Successfully cooked: {recipeName}");

        // Try to match with an active order
        MatchAndCompleteOrder(recipe);
    }

    /// <summary>
    /// Check if all ingredients are available
    /// </summary>
    private bool CheckHasAllIngredients(Recipe recipe)
    {
        if (recipe.ingredients == null || recipe.ingredients.Length == 0)
            return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (!kitchenSystem.HasIngredient(ingredient))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Match and complete an order
    /// </summary>
    private void MatchAndCompleteOrder(Recipe cookedRecipe)
    {
        var activeOrders = kitchenSystem.ActiveOrders.CurrentValue;
        if (activeOrders == null || activeOrders.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenCookingStation] No active orders to match");
            return;
        }

        // Find matching order
        var matchingOrder = activeOrders.FirstOrDefault(order => order.MealName == cookedRecipe.mealName);
        if (matchingOrder != null)
        {
            kitchenSystem.CompleteOrder(matchingOrder);
            WorldBroadcastSystem.Instance?.Broadcast($"Completed: {matchingOrder.MealName}!", 2f);
            Debug.Log($"[OverwhelmingKitchenCookingStation] Completed order: {matchingOrder.MealName}");
        }
        else
        {
            Debug.LogWarning($"[OverwhelmingKitchenCookingStation] No matching order for {cookedRecipe.mealName}");
            WorldBroadcastSystem.Instance?.Broadcast($"No one ordered {cookedRecipe.mealName}!", 2f);
        }
    }

    /// <summary>
    /// Quick cook - automatically select the first cookable order
    /// </summary>
    public void QuickCook()
    {
        if (kitchenSystem == null || kitchenSystem.CurrentState.Value != OverwhelmingKitchenState.Running)
            return;

        var activeOrders = kitchenSystem.ActiveOrders.CurrentValue;
        if (activeOrders == null || activeOrders.Count == 0)
            return;

        // Try to find an order we can complete
        foreach (var order in activeOrders)
        {
            if (order.Recipe != null && CheckHasAllIngredients(order.Recipe))
            {
                currentSelectedRecipe = order.Recipe.mealName;
                TryCook(currentSelectedRecipe);
                return;
            }
        }

        Debug.LogWarning("[OverwhelmingKitchenCookingStation] No orders can be completed with current ingredients");
    }
}
