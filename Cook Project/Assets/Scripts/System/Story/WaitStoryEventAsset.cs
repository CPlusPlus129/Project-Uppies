using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "WaitStoryEvent", menuName = "Game Flow/Utility/Wait")]
public sealed class WaitStoryEventAsset : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Duration, in seconds, to wait before the event completes.")]
    [Min(0f)]
    private float waitSeconds = 1f;

    [SerializeField]
    [Tooltip("If true, waits using unscaled realtime so pause/slowmo won't shorten the delay.")]
    private bool useUnscaledTime = true;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var delay = Mathf.Max(0f, waitSeconds);
        if (delay > 0f)
        {
            var duration = TimeSpan.FromSeconds(delay);
            var delayType = useUnscaledTime ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            await UniTask.Delay(duration, delayType: delayType, cancellationToken: cancellationToken);
        }

        return StoryEventResult.Completed(delay <= 0f ? "Wait skipped (<= 0s)." : $"Waited {delay:0.##} seconds.");
    }
}
