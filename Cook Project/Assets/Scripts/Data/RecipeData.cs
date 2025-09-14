using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "ScriptableObjects/RecipeDataObject")]
public class RecipeData : ScriptableObject
{
    public Recipe[] datas;
}

[Serializable]
public class Recipe
{
    public string mealName;
    public string[] ingredients;
}