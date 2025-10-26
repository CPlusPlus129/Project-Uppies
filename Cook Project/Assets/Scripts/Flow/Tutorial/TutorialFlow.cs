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
        Setup();
        var context = await GetContext();
        var steps = new List<ITutorialStep>
        {
            new ZeroRoomStep(context),
            new FirstRoomStep(context),
            new SecondRoomStep(context),
            new ThirdRoomStep(context),
            new FourthRoomStep(context),
            new CookingStep(context),
            new ServeMealStep(context)
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

    private void Setup()
    {
        foreach (var door in doors)
        {
            door.Close();
        }
        foreach (var doorArrow in doorArrows)
        {
            doorArrow.SetIsOn(false);
        }
        backToFirstRoomArrow.SetActive(false);
        PlayerStatSystem.Instance.CanUseWeapon.Value = false;
        WorldBroadcastSystem.Instance.TutorialHint(false, "");        

        //setup food sources
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
    }

    private async UniTask<TutorialContext> GetContext()
    {
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        var orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        var zones = new[]
        {
            zeroRoomTriggerZone,
            secondRoomTriggerZone,
            thirdRoomTriggerZone,
            fourthRoomTriggerZone,
            cookingRoomTriggerZone
        };
        var interactKey = InputManager.Instance.GetBindingDisplayString("Interact", "keyboard&mouse");
        var discardKey = InputManager.Instance.GetBindingDisplayString("Discard", "keyboard&mouse");
        var hotbarKey = InputManager.Instance.GetBindingDisplayString("HotbarShortcut", "keyboard&mouse");
        var tutorialHints = new Queue<string>();
        tutorialHints.Enqueue($"Press {interactKey} to talk to {customer.customerName}.");
        tutorialHints.Enqueue($"Press {interactKey} to gather ingredients. Press {discardKey} to discard items");
        tutorialHints.Enqueue("Find a light source to stay safe from the darkness.");
        tutorialHints.Enqueue("Use the shotgun to create light and defeat enemies.");
        tutorialHints.Enqueue("Go back to the first room and cook.");
        tutorialHints.Enqueue($"Use mouse scroll or {hotbarKey} to select the meal. Serve the meal to {customer.customerName}");
        
        var context = new TutorialContext
        {
            DialogueService = dialogueService,
            OrderManager = orderManager,
            InventorySystem = inventorySystem,
            Customer = customer,
            Doors = new Queue<SimpleDoor>(doors),
            DoorArrows = new Queue<EmissionIndicator>(doorArrows),
            PrevDoorArrows = new Queue<EmissionIndicator>(doorArrows),
            Foods = new Queue<FoodSource>(foods),
            TriggerZones = new Queue<TriggerZone>(zones),
            TutorialHints = tutorialHints,

            satanTeleportEffect = satanTeleportEffect,
            stanTeleportEffect = stanTeleportEffect,
            satanLight = satanLight,
            backArrow = backToFirstRoomArrow,

            OrderName = orderName
        };

        return context;
    }

}