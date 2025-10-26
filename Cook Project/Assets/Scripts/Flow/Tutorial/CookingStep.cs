using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class CookingStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly IDialogueService dialogueService;
    private readonly GameObject backArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private readonly string orderName;
    private readonly TriggerZone triggerZone;
    private readonly string startCookingDialogueName = "tutorial_cooking_manual";
    private readonly string endCookingDialogueName = "tutorial_cooking_complete";
    private readonly string hintText;
    private CompositeDisposable disposables = new CompositeDisposable();

    public CookingStep(TutorialContext context)
    {
        this.inventorySystem = context.InventorySystem;
        this.dialogueService = context.DialogueService;
        this.backArrow = context.backArrow;
        this.orderName = context.OrderName;
        this.prevDoorArrow = context.PrevDoorArrows.Dequeue();
        this.triggerZone = context.TriggerZones.Dequeue();
        this.hintText = context.TutorialHints.Dequeue();
    }

    public async UniTask ExecuteAsync()
    {
        backArrow?.SetActive(true);
        prevDoorArrow?.gameObject.SetActive(false);
        triggerZone.gameObject.SetActive(true);
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);
        await WaitForPlayerToEnterZone();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");
        await dialogueService.StartDialogueAsync(startCookingDialogueName);
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