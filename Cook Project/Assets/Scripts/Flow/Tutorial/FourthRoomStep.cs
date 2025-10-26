using System.Diagnostics;
using Cysharp.Threading.Tasks;
using R3;

class FourthRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string fourthRoomEnterDialogueName;
    private readonly string fourthRoomGatherDialogueName;
    private readonly IDialogueService dialogueService;
    private readonly TriggerZone triggerZone;

    public FourthRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door, string fourthRoomEnterDialogueName, string fourthRoomGatherDialogueName, IDialogueService dialogueService, TriggerZone triggerZone)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
        this.fourthRoomEnterDialogueName = fourthRoomEnterDialogueName;
        this.fourthRoomGatherDialogueName = fourthRoomGatherDialogueName;
        this.dialogueService = dialogueService;
        this.triggerZone = triggerZone;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(fourthRoomEnterDialogueName);
        await WaitUntilPlayerGetsGun();
        UnityEngine.Debug.Log("Door Open!");
        door.Open();
        await WaitUntilPlayerGetsFoodFromSource();

        await dialogueService.StartDialogueAsync(fourthRoomGatherDialogueName);
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