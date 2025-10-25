using Cysharp.Threading.Tasks;
using R3;

class FourthRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private CompositeDisposable disposables = new CompositeDisposable();

    public FourthRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitUntilPlayerGetsGun();
        door.Open();
        await WaitUntilPlayerGetsFoodFromSource();
    }

    private async UniTask WaitUntilPlayerGetsGun()
    {
        if (PlayerStatSystem.Instance.CanUseWeapon.Value)
            return;
        var tcs = new UniTaskCompletionSource();
        PlayerStatSystem.Instance.CanUseWeapon
            .Subscribe(can =>
            {
                if (can)
                {
                    disposables.Clear();
                    tcs.TrySetResult();
                }
            }).AddTo(disposables);
        await tcs.Task;
    }

    private async UniTask WaitUntilPlayerGetsFoodFromSource()
    {
        var tcs = new UniTaskCompletionSource();
        inventorySystem.OnInventoryChanged
            .Subscribe(list =>
            {
                foreach (var item in list)
                {
                    if (item != null && item.EqualsItemName(foodSource.ItemName))
                    {
                        disposables.Clear();
                        tcs.TrySetResult();
                    }
                }
            }).AddTo(disposables);

        await tcs.Task;
    }
}