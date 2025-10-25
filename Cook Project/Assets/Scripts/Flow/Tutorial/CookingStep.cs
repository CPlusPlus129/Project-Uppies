using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class CookingStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly GameObject backArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public CookingStep(IInventorySystem inventorySystem, GameObject backArrow, EmissionIndicator prevDoorArrow, string orderName)
    {
        this.inventorySystem = inventorySystem;
        this.backArrow = backArrow;
        this.orderName = orderName;
        this.prevDoorArrow = prevDoorArrow;
    }

    public async UniTask ExecuteAsync()
    {
        backArrow?.SetActive(true);
        prevDoorArrow?.gameObject.SetActive(false);
        await WaitUntilPlayerCookedMeal();
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