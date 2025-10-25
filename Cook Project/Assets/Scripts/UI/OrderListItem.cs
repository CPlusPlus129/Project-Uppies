using UnityEngine;

public class OrderListItem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI customerName;
    public TMPro.TextMeshProUGUI mealName;
    public TMPro.TextMeshProUGUI ingredientText;
    public Order order { get; private set; }

    public void SetupUI(Order order)
    {
        this.order = order;
        this.customerName.text = order.CustomerName;
        this.mealName.text = order.MealName;
        this.ingredientText.text = string.Join("\n", order.Recipe.ingredients);
    }
}