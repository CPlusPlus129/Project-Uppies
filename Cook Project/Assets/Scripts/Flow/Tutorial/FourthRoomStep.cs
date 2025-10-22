using Cysharp.Threading.Tasks;
using R3;

class FourthRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private CompositeDisposable disposables = new CompositeDisposable();

    public FourthRoomStep(IInventorySystem inventorySystem, FoodSource foodSource)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitUntilPlayerGetsFoodFromSource();
        //TODO arrow
    }

    private async UniTask WaitUntilPlayerGetsFoodFromSource()
    {
        var tcs = new UniTaskCompletionSource();
        inventorySystem.OnInventoryChanged
            .Subscribe(list =>
            {
                foreach (var item in list)
                {
                    if (item != null && item.ItemName == foodSource.ItemName)
                    {
                        tcs.TrySetResult();
                        disposables.Clear();
                    }
                }
            }).AddTo(disposables);

        await tcs.Task;
    }
}