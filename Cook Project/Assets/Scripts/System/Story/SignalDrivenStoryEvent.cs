using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SignalDrivenStoryEvent", menuName = "Game Flow/Story Events/Signal Driven")]
public class SignalDrivenStoryEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Optional override for the signal id to wait for. Defaults to the event id.")]
    private string signalIdOverride;

    [SerializeField]
    [Tooltip("UnityEvents invoked once the required signal is received.")]
    private UnityEvent onSignalReceived;

    [SerializeField]
    [Tooltip("Optional timeout in seconds. Leave at 0 to wait indefinitely.")]
    private float timeoutSeconds = 0f;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var signalId = string.IsNullOrWhiteSpace(signalIdOverride) ? EventId : signalIdOverride;
        if (string.IsNullOrWhiteSpace(signalId))
        {
            Debug.LogWarning($"[{nameof(SignalDrivenStoryEvent)}] {name} requires a signal id or valid EventId to wait on.");
            onSignalReceived?.Invoke();
            return StoryEventResult.Completed();
        }

        try
        {
            if (timeoutSeconds > 0f)
            {
                var timeout = TimeSpan.FromSeconds(timeoutSeconds);
                var waitTask = context.WaitForSignalAsync(signalId, cancellationToken);
                var timeoutTask = UniTask.Delay(timeout, DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);

                var completed = await UniTask.WhenAny(waitTask, timeoutTask);
                if (completed == 1)
                {
                    Debug.LogWarning($"[{nameof(SignalDrivenStoryEvent)}] Timed out waiting for signal '{signalId}' on {name}. Completing anyway.");
                }
            }
            else
            {
                await context.WaitForSignalAsync(signalId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        onSignalReceived?.Invoke();
        return StoryEventResult.Completed();
    }
}
