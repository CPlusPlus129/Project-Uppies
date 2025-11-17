using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "StartAfterShiftState", menuName = "Game Flow/Shift Events/Start After Shift State")]
public sealed class StartAfterShiftStateStoryEventAsset : StoryEventAsset
{
    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var shiftSystem = await context.GetServiceAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogError("[StartAfterShiftStateStoryEvent] Unable to resolve IShiftSystem.");
            return StoryEventResult.Failed("Shift system unavailable.");
        }

        shiftSystem.EnterAfterShiftState();
        return StoryEventResult.Completed("After shift state started.");
    }
}
