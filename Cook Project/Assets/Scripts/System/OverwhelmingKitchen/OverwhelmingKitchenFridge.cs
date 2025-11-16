using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Overwhelming Kitchen dedicated fridge.
/// Intelligently provides ingredients required by current orders.
/// </summary>
public class OverwhelmingKitchenFridge : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OverwhelmingKitchenSystem kitchenSystem;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private List<string> pendingIngredients = new List<string>();

    public void Interact()
    {
        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenFridge] Kitchen system reference is null!");
            return;
        }

        if (kitchenSystem.CurrentState.Value != OverwhelmingKitchenState.Running)
        {
            Debug.LogWarning("[OverwhelmingKitchenFridge] Game is not running");
            return;
        }

        // Rebuild ingredient pool if empty
        if (pendingIngredients.Count == 0)
        {
            RebuildIngredientPool();
        }

        // If still empty (no orders), do nothing
        if (pendingIngredients.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenFridge] No ingredients to give (no active orders)");
            return;
        }

        // Get first ingredient
        string ingredientName = pendingIngredients[0];
        pendingIngredients.RemoveAt(0);

        // Spawn the ingredient
        SpawnIngredient(ingredientName);

        if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenFridge] Gave ingredient: {ingredientName}. Remaining in pool: {pendingIngredients.Count}");
    }

    /// <summary>
    /// Rebuild ingredient pool - collect all ingredients needed by current orders
    /// </summary>
    private void RebuildIngredientPool()
    {
        pendingIngredients.Clear();

        var activeOrders = kitchenSystem.ActiveOrders.CurrentValue;
        if (activeOrders == null || activeOrders.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenFridge] No active orders to build ingredient pool");
            return;
        }

        // Collect all ingredients from all orders
        var recipeIngredients = new List<string>();
        foreach (var order in activeOrders)
        {
            if (order.Recipe == null || order.Recipe.ingredients == null)
                continue;

            recipeIngredients.Clear();
            recipeIngredients.AddRange(order.Recipe.ingredients);
            ShuffleList(recipeIngredients);
            pendingIngredients.AddRange(recipeIngredients);
        }

        if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenFridge] Built ingredient pool with {pendingIngredients.Count} ingredients");
    }

    /// <summary>
    /// Instantiate ingredient object and add to fake inventory
    /// </summary>
    private void SpawnIngredient(string ingredientName)
    {
        // Get ingredient prefab from database
        var ingredientPrefab = Database.Instance.itemPrefabData.GetItemByName(ingredientName);
        if (ingredientPrefab == null)
        {
            Debug.LogError($"[OverwhelmingKitchenFridge] Ingredient prefab not found: {ingredientName}");
            return;
        }

        Vector3 spawnPosition = transform.position;

        // Instantiate
        var ingredientObject = Instantiate(ingredientPrefab.gameObject, spawnPosition, Quaternion.identity);
        var ingredientItem = ingredientObject.GetComponent<ItemBase>();

        if (ingredientItem == null)
        {
            Debug.LogError($"[OverwhelmingKitchenFridge] Spawned object does not have ItemBase component: {ingredientName}");
            Destroy(ingredientObject);
            return;
        }

        // Add to kitchen system's fake inventory
        kitchenSystem.AddItemToInventory(ingredientItem, ingredientObject);

        if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenFridge] Spawned ingredient: {ingredientName} at {spawnPosition}");
    }

    /// <summary>
    /// Fisher-Yates shuffle
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

}
