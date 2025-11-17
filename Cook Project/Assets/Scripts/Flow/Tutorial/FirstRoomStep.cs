using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using NvJ.Rendering;

class FirstRoomStep : ITutorialStep
{
    private readonly IDialogueService dialogueService;
    private readonly IOrderManager orderManager;
    private readonly Customer customer;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly TriggerZone triggerZone;
    private readonly GameObject firstRoomWallDoor;
    private readonly GameObject firstRoomBlockingWall;

    //private readonly string enterDialogueName = "tutorial_first_room_entering";
    private readonly string stanDialogueName = "tutorial_first_room_orders";
    private readonly string orderName;
    private readonly string hintText;
    private CompositeDisposable disposables = new CompositeDisposable();

    private FlamePillarEffect stanTeleportEffect;

    public FirstRoomStep(TutorialContext context)
    {
        this.dialogueService = context.DialogueService;
        this.orderManager = context.OrderManager;
        this.customer = context.Customer;
        this.door = context.Doors.Dequeue();
        this.doorArrow = context.DoorArrows.Dequeue();
        this.orderName = context.OrderName;
        this.hintText = context.TutorialHints.Dequeue();
        this.stanTeleportEffect = context.stanTeleportEffect;
        this.triggerZone = context.TriggerZones.Dequeue();
        this.firstRoomWallDoor = context.firstRoomWallDoor;
        this.firstRoomBlockingWall = context.firstRoomBlockingWall;
    }

    public async UniTask ExecuteAsync()
    {
        triggerZone.gameObject.SetActive(true);
        triggerZone.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                Debug.Log("[OneRoomStep] Trigger zone activated!");
                firstRoomWallDoor.SetActive(false);
                firstRoomBlockingWall.SetActive(true);
                disposables.Clear();
            })
            .AddTo(disposables);

        customer.specifiedNextOrderName = orderName;
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);
        UnityEngine.Debug.Log("Waiting for player to take order...");
        await WaitUntilPlayerGetsOrder();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");
        UnityEngine.Debug.Log("Taking order...");
        await dialogueService.StartDialogueAsync(stanDialogueName);

        doorArrow?.SetIsOn(true);
        door.Open();
    }

    private async UniTask WaitUntilPlayerGetsOrder()
    {
        var tcs = new UniTaskCompletionSource();
        orderManager.OnNewOrder
            .Where(order => order.CustomerName == customer.customerName && order.MealName == orderName)
            .Take(1)
            .Subscribe(_ => tcs.TrySetResult())
            .AddTo(disposables);

        await tcs.Task;
    }
}