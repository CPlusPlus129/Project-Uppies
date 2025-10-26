using Cysharp.Threading.Tasks;
using R3;

class ThirdRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string thirdRoomDialogue;
    private readonly string thirdRoomDarknessDialogueName;
    private readonly IDialogueService dialogueService;
    private readonly TriggerZone triggerZone;

    public ThirdRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door, EmissionIndicator doorArrow, EmissionIndicator prevDoorArrow, string thirdRoomDialogue, string thirdRoomDarknessDialogueName, IDialogueService dialogueService, TriggerZone triggerZone)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
        this.doorArrow = doorArrow;
        this.prevDoorArrow = prevDoorArrow;
        this.thirdRoomDialogue = thirdRoomDialogue;
        this.thirdRoomDarknessDialogueName = thirdRoomDarknessDialogueName;
        this.dialogueService = dialogueService;
        this.triggerZone = triggerZone;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(thirdRoomDialogue);
        
        var playerTookDamage = new UniTaskCompletionSource();
        PlayerStatSystem.Instance.CurrentHP.Subscribe(hp =>
        {
            playerTookDamage.TrySetResult();
        }).AddTo(disposables);

        await playerTookDamage.Task;
        await dialogueService.StartDialogueAsync(thirdRoomDarknessDialogueName);

        await WaitUntilPlayerGetsFoodFromSource();
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