using Cysharp.Threading.Tasks;
using R3;

class ServeMealStep : ITutorialStep
{
    private readonly IOrderManager orderManager;
    private readonly string orderName;
    private readonly string hintText;
    private CompositeDisposable disposables = new CompositeDisposable();

    public ServeMealStep(TutorialContext context)
    {
        this.orderManager = context.OrderManager;
        this.orderName = context.OrderName;
        this.hintText = context.TutorialHints.Dequeue();
    }

    public async UniTask ExecuteAsync()
    {
        UnityEngine.Debug.Log("ServeMealStep: Waiting for player to serve order " + orderName);
        WorldBroadcastSystem.Instance.TutorialHint(true, hintText);
        await WaitUntilPlayerServedOrder();
        WorldBroadcastSystem.Instance.TutorialHint(false, "");
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