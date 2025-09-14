using UnityEngine;

public class OrderListItem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI customerName;
    public TMPro.TextMeshProUGUI mealName;
    public Order order { get; private set; }

    public void SetupUI(Order order, string customerName, string mealName)
    {
        this.order = order;
        this.customerName.text = customerName;
        this.mealName.text = mealName;
    }
}