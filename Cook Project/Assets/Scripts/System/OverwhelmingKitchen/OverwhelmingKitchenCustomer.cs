using System.Linq;
using UnityEngine;

/// <summary>
/// Overwhelming Kitchen customer object.
/// Player interacts with this to deliver meals from inventory and complete orders.
/// </summary>
public class OverwhelmingKitchenCustomer : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OverwhelmingKitchenSystem kitchenSystem;

    [Header("Customer Order")]
    [SerializeField] private Order assignedOrder;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    /// <summary>
    /// Assign an order to this customer (called by spawning/management system)
    /// </summary>
    public void AssignOrder(Order order)
    {
        assignedOrder = order;
        if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenCustomer] Assigned order: {order?.MealName}");
    }

    public void Interact()
    {
        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenCustomer] Kitchen system reference is null!");
            return;
        }

        if (kitchenSystem.CurrentState.Value != OverwhelmingKitchenState.Running)
        {
            Debug.LogWarning("[OverwhelmingKitchenCustomer] Game is not running");
            return;
        }

        QuickCompleteOrder();
    }

    /// <summary>
    /// Check if player has the required meal in inventory and complete the order
    /// </summary>
    private void DeliverMealAndCompleteOrder()
    {
        var inventory = kitchenSystem.Inventory.CurrentValue;
        if (inventory == null || inventory.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenCustomer] Inventory is empty");
            WorldBroadcastSystem.Instance?.Broadcast("You don't have any meals!", 2f);
            return;
        }

        // Check if the required meal is in inventory
        var requiredMealName = assignedOrder.MealName;
        var mealItem = inventory.FirstOrDefault(item => item.ItemName == requiredMealName);

        if (mealItem == null)
        {
            Debug.LogWarning($"[OverwhelmingKitchenCustomer] Required meal '{requiredMealName}' not found in inventory");
            WorldBroadcastSystem.Instance?.Broadcast($"I ordered {requiredMealName}, not this!", 2f);
            return;
        }

        // Remove meal from inventory
        kitchenSystem.RemoveIngredient(requiredMealName);

        // Complete the order
        kitchenSystem.CompleteOrder(assignedOrder);
        WorldBroadcastSystem.Instance?.Broadcast($"Completed: {assignedOrder.MealName}!", 2f);
        if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenCustomer] Delivered {requiredMealName} and completed order");

        // Destroy this customer object (or disable/pool it)
        Destroy(gameObject);
    }

    /// <summary>
    /// Quick complete order - automatically find and deliver any matching meal from inventory
    /// </summary>
    public void QuickCompleteOrder()
    {
        var inventory = kitchenSystem.Inventory.CurrentValue;
        if (inventory == null || inventory.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenCustomer] Inventory is empty");
            WorldBroadcastSystem.Instance?.Broadcast("You don't have any meals!", 2f);
            return;
        }

        var activeOrders = kitchenSystem.ActiveOrders.CurrentValue;
        if (activeOrders == null || activeOrders.Count == 0)
        {
            Debug.LogWarning("[OverwhelmingKitchenCustomer] No active orders");
            WorldBroadcastSystem.Instance?.Broadcast("No active orders!", 2f);
            return;
        }

        // Try to find any meal in inventory that matches any active order
        foreach (var item in inventory)
        {
            var matchingOrder = activeOrders.FirstOrDefault(order => order.MealName == item.ItemName);
            if (matchingOrder != null)
            {
                // Remove meal from inventory
                kitchenSystem.RemoveIngredient(item.ItemName);

                // Complete the order
                kitchenSystem.CompleteOrder(matchingOrder);
                WorldBroadcastSystem.Instance?.Broadcast($"Completed: {matchingOrder.MealName}!", 2f);
                if (showDebugInfo) Debug.Log($"[OverwhelmingKitchenCustomer] Quick completed order: {matchingOrder.MealName}");
                return;
            }
        }

        // No matching meal found
        Debug.LogWarning("[OverwhelmingKitchenCustomer] No meals in inventory match any active orders");
        WorldBroadcastSystem.Instance?.Broadcast("None of these meals match any orders!", 2f);
    }

    /// <summary>
    /// Get the assigned order (for UI/debugging)
    /// </summary>
    public Order GetAssignedOrder()
    {
        return assignedOrder;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (assignedOrder != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, assignedOrder.MealName);
        }
    }
#endif
}
