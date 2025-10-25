using Cysharp.Threading.Tasks;
using R3;

class ThirdRoomStep : ITutorialStep
{
    private readonly IInventorySystem inventorySystem;
    private readonly FoodSource foodSource;
    private readonly SimpleDoor door;
    private readonly EmissionIndicator doorArrow;
    private readonly EmissionIndicator prevDoorArrow;
    private CompositeDisposable disposables = new CompositeDisposable();

    public ThirdRoomStep(IInventorySystem inventorySystem, FoodSource foodSource, SimpleDoor door, EmissionIndicator doorArrow, EmissionIndicator prevDoorArrow)
    {
        this.inventorySystem = inventorySystem;
        this.foodSource = foodSource;
        this.door = door;
        this.doorArrow = doorArrow;
        this.prevDoorArrow = prevDoorArrow;
    }

    public async UniTask ExecuteAsync()
    {
        await WaitUntilPlayerGetsFoodFromSource();
        doorArrow?.SetIsOn(true);
        prevDoorArrow?.gameObject.SetActive(false);
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