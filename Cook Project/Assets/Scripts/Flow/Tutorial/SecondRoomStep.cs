using Cysharp.Threading.Tasks;
using R3;

class SecondRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private CompositeDisposable disposables = new CompositeDisposable();

    public SecondRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitUntilPlayerGetsFoodFromSource();
        door.Open();
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