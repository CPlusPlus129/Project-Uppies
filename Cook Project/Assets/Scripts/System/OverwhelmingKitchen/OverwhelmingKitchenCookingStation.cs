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

        QuickCook();
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

        SpawnItem(recipe.mealName);
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

    private void SpawnItem(string itemName) {
        var itemPrefab = Database.Instance.itemPrefabData.GetItemByName(itemName);
        if (itemPrefab == null)
        {
            Debug.LogError($"[OverwhelmingKitchenFridge] Ingredient prefab not found: {itemName}");
            return;
        }
        
        Vector3 spawnPosition = transform.position;

        // Instantiate
        var itemObject = Instantiate(itemPrefab.gameObject, spawnPosition, Quaternion.identity);
        var item = itemObject.GetComponent<ItemBase>();

        if (item == null)
        {
            Debug.LogError($"[OverwhelmingKitchenFridge] Spawned object does not have ItemBase component: {itemName}");
            Destroy(itemObject);
            return;
        }

        // Add to kitchen system's fake inventory
        kitchenSystem.AddItemToInventory(item, itemObject);

        Debug.Log($"[OverwhelmingKitchenFridge] Spawned ingredient: {itemName} at {spawnPosition}");
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
                TryCook(order.Recipe.mealName);
                return;
            }
        }

        Debug.LogWarning("[OverwhelmingKitchenCookingStation] No orders can be completed with current ingredients");
    }
}
