using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeInfoBlock : MonoBehaviour
{
    [SerializeField] private Image mealImage;
    [SerializeField] private Image ingredientPrefab;
    [SerializeField] private GameObject plusObjectPrefab;
    [SerializeField] private TextMeshProUGUI mealNameText;

    private IAssetLoader _assetLoader;
    private ICookingSystem _cookingSystem;
    private readonly List<GameObject> generatedObjects = new();
    private CancellationTokenSource loadCts;
    private CancellationTokenSource ingredientLoadCts;

    public async UniTask Init(IAssetLoader assetLoader, ICookingSystem cookingSystem)
    {
        ingredientPrefab.gameObject.SetActive(false);
        plusObjectPrefab.gameObject.SetActive(false);
        _assetLoader = assetLoader;
        _cookingSystem = cookingSystem;
        cookingSystem.currentSelectedRecipe.Subscribe(OnSelectedRecipeChanged).AddTo(this);
        await UniTask.CompletedTask;
    }

    private void OnSelectedRecipeChanged(string recipeName)
    {
        if(string.IsNullOrEmpty(recipeName))
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Recipe r = null;
        foreach(var recipe in Database.Instance.recipeData.datas)
        {
            if(recipe.mealName == recipeName)
            {
                r = recipe;
                break;
            }
        }
        if(r == null)
        {
            Debug.LogError($"Failed to find recipe {recipeName}!");
            return;
        }

        mealNameText.text = r.mealDisplayName;

        //meal
        loadCts?.Cancel();
        loadCts = new CancellationTokenSource();
        var token = loadCts.Token;
        LoadIconSprite(mealImage, recipeName, token).Forget();

        //ingredients
        foreach (var go in generatedObjects)
            Destroy(go);
        generatedObjects.Clear();

        ingredientLoadCts?.Cancel();
        ingredientLoadCts = new CancellationTokenSource();
        var ingToken = ingredientLoadCts.Token;

        Transform parent = ingredientPrefab.transform.parent;
        var ingredients = r.ingredients;
        for (int i = 0; i < ingredients.Length; i++)
        {
            string ingName = ingredients[i];

            // ingredient prefab
            var ingUI = Instantiate(ingredientPrefab, parent);
            ingUI.gameObject.SetActive(true);
            generatedObjects.Add(ingUI.gameObject);

            LoadIconSprite(ingUI, ingName, ingToken).Forget();

            // plus prefab (skip last)
            if (i < ingredients.Length - 1)
            {
                var plus = Instantiate(plusObjectPrefab, parent);
                plus.gameObject.SetActive(true);
                generatedObjects.Add(plus.gameObject);
            }
        }
    }

    private async UniTask LoadIconSprite(Image img, string ingredientName, CancellationToken token)
    {
        try
        {
            img.enabled = false;
            var sprite = await _assetLoader.LoadAsync<Sprite>($"Res:/Images/ItemIcons/{ingredientName}.png");

            if (token.IsCancellationRequested) return;

            img.sprite = sprite;
            img.enabled = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogError($"icon load failed: {e}");
        }
    }
}