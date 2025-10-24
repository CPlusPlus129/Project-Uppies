using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class TutorialFlow : MonoBehaviour
{
    [SerializeField] private TriggerZone zeroRoomTriggerZone;
    [SerializeField] private Customer customer;
    [SerializeField] private SimpleDoor[] doors;
    [SerializeField] private FoodSource[] foods;
    [SerializeField] private GameObject backToFirstRoomArrow;

    [SerializeField] private GameObject satanLight;

    [Header("Settings")]
    
    [SerializeField] private string orderName = "SoulShake";
    
    private string startDialogueName = "GameStart";
    private string zeroRoomSecondDialogueName = "ZeroRoomReachArea";
    private string firstRoomDialogueName = "FirstRoomIntro";
    private string endDialogueName = "TutorialStart";
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
        foreach (var door in doors)
        {
            door.Close();
        }
        backToFirstRoomArrow.SetActive(false);
        PlayerStatSystem.Instance.CanUseWeapon.Value = false;
        var recipe = Database.Instance.recipeData.GetRecipeByName(orderName);
        if (recipe == null || recipe.ingredients.Length != 3)
        {
            Debug.LogError($"Failed to find 3 ingredient recipe for order name {orderName}.");
            return;
        }
        for (int i = 0; i < 3; i++)
        {
            foods[i].SetItemName(recipe.ingredients[i]);
        }
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        var orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        var steps = new List<ITutorialStep>
        {
            new ZeroRoomStep(dialogueService, zeroRoomTriggerZone, startDialogueName, zeroRoomSecondDialogueName, satanLight),
            new FirstRoomStep(dialogueService, orderManager, customer, doors[0], firstRoomDialogueName, orderName),
            new SecondRoomStep(inventorySystem, foods[0], doors[1]),
            new ThirdRoomStep(inventorySystem, foods[1], doors[2]),
            new FourthRoomStep(inventorySystem, foods[2], doors[3]),
            new CookingStep(inventorySystem, backToFirstRoomArrow, orderName),
            new ServeMealStep(orderManager, orderName)
        };

        foreach (var step in steps)
        {
            Debug.Log($"Tutorial Step {step}");
            await step.ExecuteAsync();
        }

        await dialogueService.StartDialogueAsync(endDialogueName);
        Debug.Log("Tutorial Finished!");
        var sceneManagementService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
        // for current game, this is sufficient
        var nextSceneName = sceneManagementService.GetNextSceneName();
        await sceneManagementService.LoadSceneAsync(nextSceneName, null, SceneTransitionType.Fade);
    }

}