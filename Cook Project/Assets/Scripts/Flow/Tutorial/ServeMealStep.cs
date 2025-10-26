using Cysharp.Threading.Tasks;
using R3;

class ServeMealStep : ITutorialStep
{
    private readonly IOrderManager orderManager;
    private readonly string orderName;
    private CompositeDisposable disposables = new CompositeDisposable();

    public ServeMealStep(IOrderManager orderManager, string orderName)
    {
        this.orderManager = orderManager;
        this.orderName = orderName;
    }

    public async UniTask ExecuteAsync()
    {
        UnityEngine.Debug.Log("ServeMealStep: Waiting for player to serve order " + orderName);
        await WaitUntilPlayerServedOrder();
    }

    private async UniTask WaitUntilPlayerServedOrder()
    {
        var tcs = new UniTaskCompletionSource();
        orderManager.OnOrderServed
            .Where(order => string.Equals(order.MealName, orderName, System.StringComparison.InvariantCultureIgnoreCase))
            .Take(1)
            .Subscribe(_ => tcs.TrySetResult())
            .AddTo(disposables);

        await tcs.Task;
    }
}