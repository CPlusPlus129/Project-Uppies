using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class RecipeSelectionPanel : MonoBehaviour, IUIInitializable
{
    public RecipeItem recipeItemPrefab;
    public Button cookButton;
    private ICookingSystem cookingSystem;
    private ObjectPool<RecipeItem> recipeItemPool;
    private List<RecipeItem> recipeItemList = new List<RecipeItem>();
    
    public async UniTask Init()
    {
        recipeItemPrefab.gameObject.SetActive(false);
        recipeItemPool = new ObjectPool<RecipeItem>(() =>
        {
            var item = Instantiate(recipeItemPrefab, recipeItemPrefab.transform.parent);
            item.cookingSystem = cookingSystem;
            return item;
        });
        cookingSystem = await ServiceLocator.Instance.GetAsync<ICookingSystem>();
        cookingSystem.currentSelectedRecipe
            .Subscribe(_ => UpdateCookButton())
            .AddTo(this);
        cookButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                cookingSystem.Cook();
                UpdateUI();
            }).AddTo(this);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        UpdateRecipeList();
        UpdateCookButton();
    }

    private void UpdateRecipeList()
    {
        ClearAllItems();
        foreach (var recipe in Database.Instance.recipeData.datas)
        {
            var item = recipeItemPool.Get();
            item.gameObject.SetActive(true);
            item.Setup(recipe);
            var canCook = cookingSystem.CheckPlayerHasIngredients(recipe);
            item.btn.interactable = canCook;
            recipeItemList.Add(item);
        }        
    }

    private void UpdateCookButton()
    {
        var currentRecipeName = cookingSystem.currentSelectedRecipe.Value;
        if(string.IsNullOrEmpty(currentRecipeName))
        {
            cookButton.interactable = false;
            return;
        }
        var recipe = Database.Instance.recipeData.GetRecipeByName(currentRecipeName);
        cookButton.interactable = cookingSystem.CheckPlayerHasIngredients(recipe);
    }

    private void ClearAllItems()
    {
        foreach (var item in recipeItemList)
        {
            item.ResetUI();
            recipeItemPool.Release(item);
            item.gameObject.SetActive(false);
        }
        recipeItemList.Clear();
    }

    
}