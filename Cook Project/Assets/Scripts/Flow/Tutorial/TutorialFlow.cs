using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using NvJ.Rendering;
using UnityEngine;

public class TutorialFlow : MonoBehaviour
{
    [SerializeField] private TriggerZone zeroRoomTriggerZone;
    [SerializeField] private TriggerZone secondRoomTriggerZone;
    [SerializeField] private TriggerZone thirdRoomTriggerZone;
    [SerializeField] private TriggerZone fourthRoomTriggerZone;
    [SerializeField] private TriggerZone cookingRoomTriggerZone;
    [SerializeField] private Customer customer;
    [SerializeField] private SimpleDoor[] doors;
    [SerializeField] private FoodSource[] foods;
    [SerializeField] private EmissionIndicator[] doorArrows;
    [SerializeField] private GameObject backToFirstRoomArrow;

    [SerializeField] private FlamePillarEffect satanTeleportEffect;
    [SerializeField] private FlamePillarEffect stanTeleportEffect;

    [SerializeField] private GameObject satanLight;

    private string orderName = "SoulShake";
    
    // Room 0 Dialogue
    private string startDialogueName = "tutorial_start";
    private string zeroRoomSecondDialogueName = "meeting_satan";

    // Room 1 Dialogue
    private string firstRoomEnterDialogueName = "tutorial_first_room_entering";
    private string firstRoomStanDialogueName = "tutorial_first_room_orders";

    // Room 2 Dialogue
    private string secondRoomEnterDialogueName = "tutorial_second_room";

    // Room 3 Dialogue
    private string thirdRoomEnterDialogueName = "tutorial_third_room";
    private string thirdRoomDarknessDialogueName = "tutorial_third_room_damage";

    // Room 4 Dialogue
    private string fourthRoomEnterDialogueName = "tutorial_fourth_room";

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
        foreach (var doorArrow in doorArrows)
        {
            doorArrow.SetIsOn(false);
        }
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        var orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        var steps = new List<ITutorialStep>
        {
            new ZeroRoomStep(dialogueService, zeroRoomTriggerZone, startDialogueName, zeroRoomSecondDialogueName, satanLight, satanTeleportEffect),
            new FirstRoomStep(dialogueService, orderManager, customer, doors[0], doorArrows[0], firstRoomEnterDialogueName, firstRoomStanDialogueName, orderName, stanTeleportEffect),
            new SecondRoomStep(inventorySystem, foods[0], doors[1], doorArrows[1], doorArrows[0], secondRoomEnterDialogueName, dialogueService, secondRoomTriggerZone),
            new ThirdRoomStep(inventorySystem, foods[1], doors[2], doorArrows[2], doorArrows[1], thirdRoomEnterDialogueName, thirdRoomDarknessDialogueName, dialogueService, thirdRoomTriggerZone),
            new FourthRoomStep(inventorySystem, foods[2], doors[3], fourthRoomEnterDialogueName, dialogueService, fourthRoomTriggerZone),
            new CookingStep(inventorySystem, backToFirstRoomArrow, doorArrows[2], orderName, cookingRoomTriggerZone),
            new ServeMealStep(orderManager, orderName)
        };

        foreach (var step in steps)
        {
            Debug.Log($"Tutorial Step {step}");
            await step.ExecuteAsync();
        }

        Debug.Log("Tutorial Finished!");
        var sceneManagementService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
        // for current game, this is sufficient
        var nextSceneName = sceneManagementService.GetNextSceneName();
        await sceneManagementService.LoadSceneAsync(nextSceneName, null, SceneTransitionType.Fade);
    }

}