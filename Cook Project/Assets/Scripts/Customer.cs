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

    private void Awake()
    {
        nameText.text = customerName;
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
        var randRecipe = Database.Instance.recipeData.GetRandomRecipe();
        var order = new Order
        {
            CustomerName = customerName,
            MealName = randRecipe.mealName
        };
        OrderManager.Instance.PlaceOrder(order);
        state = CustomerState.WaitingForMeal;
        Debug.Log($"{customerName} placed order: {order.MealName}");
    }

    public bool CanReceiveMeal(ItemBase item)
    {
        if (state != CustomerState.WaitingForMeal) return false;
        if (item is not Meal meal) return false;
        var pendingOrder = OrderManager.Instance.GetPendingOrderForCustomer(customerName);
        return pendingOrder != null && pendingOrder.MealName.Equals(meal.ItemName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    public void ReceiveMeal(ItemBase meal)
    {
        var order = new Order { CustomerName = customerName, MealName = meal.ItemName };
        if (OrderManager.Instance.ServeOrder(order))
        {
            InventorySystem.Instance.RemoveSelectedItem();
            state = CustomerState.WaitingForOrder;
            Debug.Log($"{customerName} received meal: {meal.ItemName}");
        }
    }
}