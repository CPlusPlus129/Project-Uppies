using Cysharp.Threading.Tasks;
using R3;

class FourthRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly IDialogueService dialogueService;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string fourthRoomEnterDialogueName = "tutorial_fourth_room";
    private readonly string fourthRoomGatherDialogueName = "tutorial_fourth_room_gathered";
    private readonly string hintText;
    private readonly TriggerZone triggerZone;

    public FourthRoomStep(TutorialContext context)
    {
        this.inventorySystem = context.InventorySystem;
        this.dialogueService = context.DialogueService;
        this.foodSource = context.Foods.Dequeue();
        this.door = context.Doors.Dequeue();
        this.triggerZone = context.TriggerZones.Dequeue();
        this.hintText = context.TutorialHints.Dequeue();
    }

    public async UniTask ExecuteAsync()
    {
        triggerZone.gameObject.SetActive(true);
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(fourthRoomEnterDialogueName);
        await WaitUntilPlayerGetsGun();
        UnityEngine.Debug.Log("Door Open!");
        door.Open();
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);
        await WaitUntilPlayerGetsFoodFromSource();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");

        await dialogueService.StartDialogueAsync(fourthRoomGatherDialogueName);
        triggerZone.gameObject.SetActive(false);
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

    private async UniTask WaitUntilPlayerGetsGun()
    {
        if (PlayerStatSystem.Instance.CanUseWeapon.Value)
            return;
        var tcs = new UniTaskCompletionSource();
        PlayerStatSystem.Instance.CanUseWeapon
            .Subscribe(can =>
            {
                if (can)
                {
                    disposables.Clear();
                    tcs.TrySetResult();
                }
            }).AddTo(disposables);
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