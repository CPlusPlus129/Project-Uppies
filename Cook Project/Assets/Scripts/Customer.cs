using UnityEngine;

public class Customer : MonoBehaviour, IInteractable
{
    public string customerName;

    public void Interact()
    {
        Debug.Log($"interact with customer {customerName}");
        if (!OrderManager.Instance.CustomerHasPendingOrder(customerName))
        {
            //TEST
            var order = new Order { CustomerName = customerName, MealName = "BloodBurger" };
            OrderManager.Instance.PlaceOrder(order);
            Debug.Log($"Placed order for {customerName}: {order.MealName}");
        }
    }
}