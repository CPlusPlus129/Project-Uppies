using R3;
using UnityEngine;

public class CookingSystem : SimpleSingleton<CookingSystem>
{
    public ReactiveProperty<string> currentSelectedRecipe = new ReactiveProperty<string>("");

    public void Cook()
    {
        if (string.IsNullOrEmpty(currentSelectedRecipe.Value)) return;
        var r = Database.Instance.recipeData.GetRecipeByName(currentSelectedRecipe.Value);
        if (r == null)
        {
            Debug.LogError("Recipe not found: " + currentSelectedRecipe.Value);
            return;
        }
        if (!CheckPlayerHasIngredients(r))
        {
            Debug.LogError("Player does not have all ingredients for: " + currentSelectedRecipe.Value);
            return;
        }

        


        foreach (var ingredient in r.ingredients)
        {
            InventorySystem.Instance.RemoveItem(ingredient);
        }


        var mealPrefab = Database.Instance.itemPrefabData.GetItemByName(r.mealName);
        var mealItem = mealPrefab != null ? GameObject.Instantiate(mealPrefab) : null;
        if (mealItem == null)
        {
            Debug.LogError("Meal item prefab not found: " + r.mealName);
            return;
        }
        mealItem.gameObject.SetActive(false);
        InventorySystem.Instance.AddItem(mealItem);
        Debug.Log("Cooked: " + r.mealName);

        // Enable minigame here (Canvas-Minigame)
        UIRoot.Instance.GetUIComponent<CookingUI>().Close();
        UIRoot.Instance.GetUIComponent<MinigameUI>().Open();

    }

    public bool CheckPlayerHasIngredients(Recipe recipe)
    {
        var cache = InventorySystem.Instance.GetItemCache();
        foreach (var ingredient in recipe.ingredients)
        {
            if (!cache.TryGetValue(ingredient, out int itemCount) || itemCount <= 0)
                return false;
        }
        return true;
    }
}