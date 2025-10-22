using Cysharp.Threading.Tasks;
using R3;

class CookingStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public CookingStep(IInventorySystem inventorySystem, string orderName)
    {
        this.inventorySystem = inventorySystem;
        this.orderName = orderName;
    }

    public async UniTask ExecuteAsync()
    {
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
                    if (item != null && item.ItemName == orderName)
                    {
                        tcs.TrySetResult();
                        disposables.Clear();
                    }
                }
            }).AddTo(disposables);

        await tcs.Task;
    }
}