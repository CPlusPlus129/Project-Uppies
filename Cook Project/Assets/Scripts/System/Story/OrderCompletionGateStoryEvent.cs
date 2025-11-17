using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

[CreateAssetMenu(fileName = "OrderCompletionGate", menuName = "Game Flow/Shift Events/Order Completion Gate")]
public class OrderCompletionGateStoryEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Number of orders the player must complete after this event starts.")]
    private int requiredOrderCompletions = 1;

    [SerializeField]
    [Tooltip("Optional message logged while waiting. Leave blank to skip logging.")]
    private string debugMessage;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (requiredOrderCompletions <= 0)
        {
            return StoryEventResult.Completed();
        }

        var orderManager = await context.GetServiceAsync<IOrderManager>();
        if (orderManager == null)
        {
            Debug.LogWarning($"[{nameof(OrderCompletionGateStoryEvent)}] Could not resolve IOrderManager for {name}. Completing immediately.");
            return StoryEventResult.Completed();
        }

        if (!string.IsNullOrWhiteSpace(debugMessage))
        {
            Debug.Log($"[{nameof(OrderCompletionGateStoryEvent)}] {debugMessage} (target {requiredOrderCompletions})");
        }

        var tcs = new UniTaskCompletionSource<bool>();
        int progress = 0;
        var subscription = orderManager.OnOrderServed
            .Subscribe(_ =>
            {
                progress++;
                if (progress >= requiredOrderCompletions)
                {
                    tcs.TrySetResult(true);
                }
            });

        try
        {
            await tcs.Task.AttachExternalCancellation(cancellationToken);
        }
        finally
        {
            subscription.Dispose();
        }

        return StoryEventResult.Completed();
    }
}
