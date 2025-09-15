using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "ScriptableObjects/RecipeDataObject")]
public class RecipeData : ScriptableObject
{
    public Recipe[] datas;

    public Recipe GetRecipeByName(string mealName)
    {
        foreach (var recipe in datas)
        {
            if (recipe.mealName == mealName)
            {
                return recipe;
            }
        }
        return null;
    }

    public Recipe GetRandomRecipe()
    {
        if (datas.Length == 0) return null;
        int index = UnityEngine.Random.Range(0, datas.Length);
        return datas[index];
    }
}

[Serializable]
public class Recipe
{
    public string mealName;
    public string[] ingredients;
}