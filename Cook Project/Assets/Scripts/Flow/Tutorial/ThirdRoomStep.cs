using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class ThirdRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly IDialogueService dialogueService;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private CompositeDisposable disposables = new CompositeDisposable();
    private readonly string thirdRoomDialogue = "tutorial_third_room";
    private readonly string thirdRoomDarknessDialogueName = "tutorial_third_room_damage";
    private readonly string hintText;
    private readonly TriggerZone triggerZone;

    public ThirdRoomStep(TutorialContext context)
    {
        this.inventorySystem =context.InventorySystem;
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
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(thirdRoomDialogue);

        await WaitForPlayerToExitAllSafeZones();

        var player = GameObject.FindWithTag("Player");
        var lightDamage = player?.GetComponent<PlayerLightDamage>();

        await UniTask.WaitForSeconds(1);
        if (lightDamage != null) lightDamage.SetDamageDisabled(true);

        await dialogueService.StartDialogueAsync(thirdRoomDarknessDialogueName);

        if (lightDamage != null) lightDamage.SetDamageDisabled(false);
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);

        await WaitUntilPlayerGetsFoodFromSource();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");
        doorArrow?.SetIsOn(true);
        prevDoorArrow?.gameObject.SetActive(false);
        door.Open();
    }

    private async UniTask WaitForPlayerToExitAllSafeZones()
    {
        var tcs = new UniTaskCompletionSource();

        // Wait until player is in a safe zone, then wait until they are not.
        Observable.EveryUpdate()
            .SkipWhile(_ => !SafeZone.IsPlayerInAnySafeZone) // Wait until we are in a zone
            .SkipWhile(_ => SafeZone.IsPlayerInAnySafeZone)  // Then wait until we are out of the zone
            .Take(1)
            .Subscribe(_ => {
                tcs.TrySetResult();
            }).AddTo(disposables);

        await tcs.Task;
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