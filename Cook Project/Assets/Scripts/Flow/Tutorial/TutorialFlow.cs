using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class TutorialFlow : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [SerializeField] private SimpleDoor firstDoor;
    [SerializeField] private SimpleDoor secondDoor;
    [SerializeField] private SimpleDoor thirdDoor;
    [SerializeField] private FoodSource food1;
    [SerializeField] private FoodSource food2;
    [SerializeField] private FoodSource food3;

    [Header("Settings")]
    [SerializeField] private string startDialogueName = "story_test1";
    [SerializeField] private string orderName = "SoulShake";

    private void Start()
    {
        StartTutorial().Forget();
    }

    private async UniTaskVoid StartTutorial()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
#if UNITY_WEBGL && !UNITY_EDITOR
        await UniTask.Delay(2000); //wait for webgl to load
#endif
        Debug.Log("Start tutorial!");
        firstDoor.Close();
        secondDoor.Close();
        thirdDoor.Close();
        PlayerStatSystem.Instance.CanUseWeapon.Value = false;
        var recipe = Database.Instance.recipeData.GetRecipeByName(orderName);
        if (recipe == null || recipe.ingredients.Length != 3)
        {
            Debug.LogError($"Failed to find 3 ingredient recipe for order name {orderName}.");
            return;
        }
        food1.SetItemName(recipe.ingredients[0]);
        food2.SetItemName(recipe.ingredients[1]);
        food3.SetItemName(recipe.ingredients[2]);
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        var orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        var steps = new List<ITutorialStep>
        {
            new FirstRoomStep(dialogueService, orderManager, customer, firstDoor, startDialogueName, orderName),
            new SecondRoomStep(inventorySystem, food1, secondDoor),
            new ThirdRoomStep(inventorySystem, food2, thirdDoor),
            new FourthRoomStep(inventorySystem, food3),
        };

        foreach (var step in steps)
        {
            await step.ExecuteAsync();
        }

        Debug.Log("Tutorial Finished!");
    }

}