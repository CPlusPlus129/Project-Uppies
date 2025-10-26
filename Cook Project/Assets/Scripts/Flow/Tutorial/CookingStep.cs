using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class CookingStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly GameObject backArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private readonly string orderName;
    private readonly TriggerZone triggerZone;
    private readonly IDialogueService dialogueService;
    private readonly string startCookingDialogueName;
    private readonly string endCookingDialogueName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public CookingStep(IInventorySystem inventorySystem, GameObject backArrow, EmissionIndicator prevDoorArrow, string orderName, TriggerZone triggerZone, IDialogueService dialogueService, string startCookingDialogueName, string endCookingDialogueName)
    {
        this.inventorySystem = inventorySystem;
        this.backArrow = backArrow;
        this.orderName = orderName;
        this.prevDoorArrow = prevDoorArrow;
        this.triggerZone = triggerZone;
        this.dialogueService = dialogueService;
        this.startCookingDialogueName = startCookingDialogueName;
        this.endCookingDialogueName = endCookingDialogueName;
    }

    public async UniTask ExecuteAsync()
    {
        triggerZone.gameObject.SetActive(true);
        await WaitForPlayerToEnterZone();
        await dialogueService.StartDialogueAsync(startCookingDialogueName);
        backArrow?.SetActive(true);
        prevDoorArrow?.gameObject.SetActive(false);
        await WaitUntilPlayerCookedMeal();

        await UniTask.WaitForSeconds(1);
        await dialogueService.StartDialogueAsync(endCookingDialogueName);
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

    private async UniTask WaitUntilPlayerCookedMeal()
    {
        var tcs = new UniTaskCompletionSource();
        inventorySystem.OnInventoryChanged
            .Subscribe(list =>
            {
                foreach (var item in list)
                {
                    if (item != null && item.EqualsItemName(orderName))
                    {
                        disposables.Clear();
                        tcs.TrySetResult();
                    }
                }
            }).AddTo(disposables);

        await tcs.Task;
    }
}