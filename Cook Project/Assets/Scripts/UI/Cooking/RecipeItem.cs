using R3;
using UnityEngine;
using UnityEngine.UI;

public class RecipeItem : MonoBehaviour
{
    public Button btn;
    public TMPro.TextMeshProUGUI mealName;
    public TMPro.TextMeshProUGUI ingredientText; // use text just for demo
    public Recipe recipe { get; set; }

    private void Awake()
    {
        btn.OnClickAsObservable().Subscribe(_ =>
        {
            CookingSystem.Instance.currentSelectedRecipe.Value = recipe.mealName;
        }).AddTo(this);
    }

    public void Setup(Recipe recipe)
    {
        this.recipe = recipe;
        mealName.text = recipe.mealName;
        ingredientText.text = string.Join(" + ", recipe.ingredients);
    }

    public void ResetUI()
    {
        recipe = null;
        mealName.text = ingredientText.text = "";
    }
}