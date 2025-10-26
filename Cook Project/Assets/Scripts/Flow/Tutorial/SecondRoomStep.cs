using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class SecondRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string secondRoomDialogue;
    private readonly IDialogueService dialogueService;
    private readonly TriggerZone triggerZone;

    public SecondRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door, EmissionIndicator doorArrow, EmissionIndicator prevDoorArrow, string secondRoomDialogue, IDialogueService dialogueService, TriggerZone secondRoomTriggerZone)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
        this.doorArrow = doorArrow;
        this.prevDoorArrow = prevDoorArrow;
        this.secondRoomDialogue = secondRoomDialogue;
        this.dialogueService = dialogueService;
        this.triggerZone = secondRoomTriggerZone;
    }

    public async UniTask ExecuteAsync()
    {
        // Wait for player to walk into room
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(secondRoomDialogue);

        // Wit until player grabs food item
        await WaitUntilPlayerGetsFoodFromSource();

        // Enable arrow & open door
        doorArrow?.SetIsOn(true);
        prevDoorArrow?.gameObject.SetActive(false);
        door.Open();
    }

    private async UniTask WaitForPlayerToEnterZone()
    {
        var tcs = new UniTaskCompletionSource();
        
        triggerZone.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                Debug.Log("[ZeroRoomStep] Trigger zone activated!");
                disposables.Clear();
                tcs.TrySetResult();
            })
            .AddTo(disposables);

        await tcs.Task;
    }

    private async UniTask WaitUntilPlayerGetsFoodFromSource()
    {
        var tcs = new UniTaskCompletionSource();
        inventorySystem.OnInventoryChanged
            .Subscribe(list =>
            {
                foreach (var item in list)
                {
                    if (item != null && item.EqualsItemName(foodSource.ItemName))
                    {
                        disposables.Clear();
                        tcs.TrySetResult();
                    }
                }
            }).AddTo(disposables);

        await tcs.Task;
    }
}