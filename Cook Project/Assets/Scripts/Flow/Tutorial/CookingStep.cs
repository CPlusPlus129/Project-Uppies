using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

class CookingStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly GameObject backArrow;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public CookingStep(IInventorySystem inventorySystem, GameObject backArrow, string orderName)
    {
        this.inventorySystem = inventorySystem;
        this.backArrow = backArrow;
        this.orderName = orderName;
    }

    public async UniTask ExecuteAsync()
    {
        backArrow?.SetActive(true);
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