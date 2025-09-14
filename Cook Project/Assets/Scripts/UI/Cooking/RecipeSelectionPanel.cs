using R3;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSelectionPanel : MonoBehaviour
{
    public RecipeItem recipeItemPrefab;
    public Button cookButton;

    private void Awake()
    {
        recipeItemPrefab.gameObject.SetActive(false);
        CookingSystem.Instance.currentSelectedRecipe
            .Subscribe(s => cookButton.interactable = !string.IsNullOrEmpty(s))
            .AddTo(this);

        cookButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                CookingSystem.Instance.Cook();
            }).AddTo(this);
    }
}