using System.Diagnostics;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class FirstRoomStep : ITutorialStep
{
    private readonly IDialogueService dialogueService;
    private readonly IOrderManager orderManager;
    private readonly Customer customer;
    private readonly SimpleDoor door;
    private readonly string dialogueName;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public FirstRoomStep(IDialogueService dialogueService, IOrderManager orderManager, Customer customer, SimpleDoor door, string dialogueName, string orderName)
    {
        this.dialogueService = dialogueService;
        this.orderManager = orderManager;
        this.customer = customer;
        this.door = door;
        this.dialogueName = dialogueName;
        this.orderName = orderName;
    }

    public async UniTask ExecuteAsync()
    {
        customer.specifiedNextOrderName = orderName;
        UnityEngine.Debug.Log("Waiting for player to take order...");
        await WaitUntilPlayerGetsOrder();
        UnityEngine.Debug.Log("Taking order...");
        await dialogueService.StartDialogueAsync(dialogueName);


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