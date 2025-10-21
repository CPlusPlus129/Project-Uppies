using Cysharp.Threading.Tasks;
using UnityEngine;

public class Customer : MonoBehaviour, IInteractable
{
    public string customerName;
    public TMPro.TextMeshPro nameText;

    public enum CustomerState
    {
        WaitingForOrder,
        OrderPlaced, WaitingForMeal
    }
    private CustomerState state = CustomerState.WaitingForOrder;
    private IOrderManager orderManager;
    private IInventorySystem inventorySystem;
    public string specifiedNextOrderName { get; set; }

    private async void Awake()
    {
        nameText.text = customerName;
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
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
        string mealName = null;
        if (!string.IsNullOrEmpty(specifiedNextOrderName))
        {
            mealName = specifiedNextOrderName;
            specifiedNextOrderName = null;
        }
        else
        {
            var randRecipe = Database.Instance.recipeData.GetRandomRecipe();
            mealName = randRecipe.mealName;
        }
        var order = new Order
        {
            CustomerName = customerName,
            MealName = mealName
        };
        orderManager.PlaceOrder(order);
        state = CustomerState.WaitingForMeal;
        Debug.Log($"{customerName} placed order: {order.MealName}");
        //WorldBroadcastSystem.Instance.Broadcast($"Check recipes in the pot. Find ingredients on the map to cook them.", 60f);
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
        var order = new Order { CustomerName = customerName, MealName = meal.ItemName };
        if (orderManager.ServeOrder(order))
        {
            inventorySystem.RemoveSelectedItem();
            state = CustomerState.WaitingForOrder;
            Debug.Log($"{customerName} received meal: {meal.ItemName}");
        }
    }
}