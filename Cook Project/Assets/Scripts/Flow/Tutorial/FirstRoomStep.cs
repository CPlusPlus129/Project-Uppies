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

    private readonly string enterDilaogueName;
    private readonly string stanDialogueName;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    private FlamePillarEffect stanTeleportEffect;

    public FirstRoomStep(IDialogueService dialogueService, IOrderManager orderManager, Customer customer, SimpleDoor door, EmissionIndicator doorArrow, string enterDialogueName, string stanDialogueName, string orderName, FlamePillarEffect stanTeleportEffect)
    {
        this.dialogueService = dialogueService;
        this.orderManager = orderManager;
        this.customer = customer;
        this.door = door;
        this.doorArrow = doorArrow;
        this.enterDilaogueName = enterDialogueName;
        this.stanDialogueName = stanDialogueName;
        this.orderName = orderName;
        this.stanTeleportEffect = stanTeleportEffect;
    }

    public async UniTask ExecuteAsync()
    {
        Debug.Log("HERE!");
        await UniTask.WaitForSeconds(2);
        await dialogueService.StartDialogueAsync(enterDilaogueName);

        customer.specifiedNextOrderName = orderName;
        UnityEngine.Debug.Log("Waiting for player to take order...");
        await WaitUntilPlayerGetsOrder();
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