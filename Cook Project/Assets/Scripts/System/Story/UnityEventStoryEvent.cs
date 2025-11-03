using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "UnityEventStoryEvent", menuName = "Game Flow/Story Events/Unity Event")]
public class UnityEventStoryEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("UnityEvents invoked when this story event executes.")]
    private UnityEvent onExecute;

    [SerializeField]
    [Tooltip("If enabled, the event waits for a matching signal before completing.")]
    private bool waitForSignal = false;

    [SerializeField]
    [Tooltip("Optional override for the signal id to wait for. Defaults to the event id.")]
    private string signalIdOverride;

    [SerializeField]
    [Tooltip("Optional delay (in seconds) before the event finishes. Ignored when waiting for a signal.")]
    private float autoAdvanceDelaySeconds = 0f;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        onExecute?.Invoke();

        if (waitForSignal)
        {
            var signalId = string.IsNullOrWhiteSpace(signalIdOverride) ? EventId : signalIdOverride;
            await context.WaitForSignalAsync(signalId, cancellationToken);
        }
        else if (autoAdvanceDelaySeconds > 0f)
        {
            var delay = TimeSpan.FromSeconds(autoAdvanceDelaySeconds);
            await UniTask.Delay(delay, DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
        }

        return StoryEventResult.Completed();
    }
}
