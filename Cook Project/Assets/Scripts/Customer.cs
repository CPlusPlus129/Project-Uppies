using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class Customer : MonoBehaviour, IInteractable
{
    public string customerName;
    public TMPro.TextMeshPro nameText;

    public enum CustomerState
    {
        WaitingForOrder,
        WaitingForMeal
    }
    private CustomerState state = CustomerState.WaitingForOrder;
    private IOrderManager orderManager;
    private IInventorySystem inventorySystem;
    public string specifiedNextOrderName { get; set; }

    private async void Awake()
    {
        if (nameText == null)
        {
            nameText = GetComponentInChildren<TMPro.TextMeshPro>();
        }

        if (nameText != null)
        {
            nameText.text = customerName;
        }
        else
        {
            Debug.LogWarning($"Customer {customerName} has no nameText assigned or found in children.");
        }
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        orderManager.OnOrdersCleared.Subscribe(_ => state = CustomerState.WaitingForOrder).AddTo(this);
    }

    public void Interact()
    {
        switch (state)
        {
            case CustomerState.WaitingForOrder:
                PlaceOrder();
                break;
            case CustomerState.WaitingForMeal:
                Debug.Log($"{customerName} is waiting for meal delivery");
                break;
        }
    }

    private void PlaceOrder()
    {
        Recipe recipe = null;
        if (!string.IsNullOrEmpty(specifiedNextOrderName))
        {
            var mealName = specifiedNextOrderName;
            specifiedNextOrderName = null;
            recipe = Database.Instance.recipeData.GetRecipeByName(mealName);
        }
        else
        {
            recipe = Database.Instance.recipeData.GetRandomRecipe();
        }
        if (recipe == null)
        {
            Debug.LogError("Failed to find recipe to place order.");
            return;
        }
        var order = new Order
        {
            CustomerName = customerName,
            MealName = recipe.mealName,
            Recipe = recipe,
            PlacedAtTime = Time.time
        };
        orderManager.PlaceOrder(order);
        state = CustomerState.WaitingForMeal;
        Debug.Log($"{customerName} placed order: {order.MealName}");
    }

    public bool CanReceiveMeal(ItemBase item)
    {
        if (state != CustomerState.WaitingForMeal) return false;
        if (item is not Meal meal) return false;
        var pendingOrder = orderManager.GetPendingOrderForCustomer(customerName);
        return pendingOrder != null && pendingOrder.MealName.Equals(meal.ItemName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    public void ReceiveMeal(ItemBase meal)
    {
        if (meal is not Meal cookedMeal)
        {
            Debug.LogError("ReceiveMeal called with non-meal item despite CanReceiveMeal check.");
            return;
        }

        var order = new Order { CustomerName = customerName, MealName = meal.ItemName };
        if (orderManager.ServeOrder(order, cookedMeal))
        {
            inventorySystem.RemoveSelectedItem();
            state = CustomerState.WaitingForOrder;
            Debug.Log($"{customerName} received meal: {meal.ItemName}");
        }
    }
}
