using R3;
using UnityEngine;
using UnityEngine.UI;

public class RecipeItem : MonoBehaviour
{
    public Toggle tgl;
    public TMPro.TextMeshProUGUI mealName;
    public ICookingSystem cookingSystem { get; set; }
    public Recipe recipe { get; set; }

    private void Awake()
    {
        tgl.OnValueChangedAsObservable().Subscribe(isOn =>
        {
            if (isOn)
                cookingSystem.currentSelectedRecipe.Value = recipe.mealName;
        }).AddTo(this);
    }

    public void Setup(Recipe recipe)
    {
        this.recipe = recipe;
        mealName.text = recipe.mealName;
    }

    public void ResetUI()
    {
        recipe = null;
    }
}