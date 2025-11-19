using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Events/Remove Task")]
public class RemoveTaskEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("The Event ID of the task to remove.")]
    private string targetEventId;

    [SerializeField]
    [Tooltip("Time in seconds to show the task as completed before removing it.")]
    private float completionDelay = 2.0f;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(targetEventId))
        {
            TaskManager.Instance.CompleteTask(targetEventId, completionDelay);
        }

        return UniTask.FromResult(StoryEventResult.Completed());
    }
}
