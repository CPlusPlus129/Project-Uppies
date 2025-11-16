using UnityEngine;

public class OrderListItem : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private TMPro.TextMeshProUGUI customerName;
    [SerializeField] private TMPro.TextMeshProUGUI mealName;
    [SerializeField] private TMPro.TextMeshProUGUI ingredientText;
    public Order order { get; private set; }

    public void SetupUI(Order order)
    {
        this.order = order;
        this.customerName.text = order.CustomerName;
        this.mealName.text = order.MealName;
        this.ingredientText.text = string.Join("\n", order.Recipe.ingredients);
    }

    public void Enter()
    {
        anim.SetTrigger("enter");
    }

    public void Exit() 
    {
        anim.SetTrigger("exit");
    }
}