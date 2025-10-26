using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class SecondRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly IDialogueService dialogueService;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string secondRoomEnterDialogue = "tutorial_second_room";
    private readonly string secondRoomFoodDialogue = "tutorial_second_room_gathered";
    private readonly string hintText;
    private readonly TriggerZone triggerZone;

    public SecondRoomStep(TutorialContext context)
    {
        this.inventorySystem = context.InventorySystem;
        this.dialogueService = context.DialogueService;
        this.foodSource = context.Foods.Dequeue();
        this.door = context.Doors.Dequeue();
        this.doorArrow = context.DoorArrows.Dequeue();
        this.prevDoorArrow = context.PrevDoorArrows.Dequeue();
        this.triggerZone = context.TriggerZones.Dequeue();
        this.hintText = context.TutorialHints.Dequeue();
    }

    public async UniTask ExecuteAsync()
    {
        // Wait for player to walk into room
        triggerZone.gameObject.SetActive(true);
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(secondRoomEnterDialogue);

        // Wit until player grabs food item
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);
        await WaitUntilPlayerGetsFoodFromSource();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");
        await dialogueService.StartDialogueAsync(secondRoomFoodDialogue);

        // Enable arrow & open door
        doorArrow?.SetIsOn(true);
        prevDoorArrow?.gameObject.SetActive(false);
        door.Open();
        triggerZone.gameObject.SetActive(false);
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