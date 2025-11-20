using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Events/Remove Task")]
public class RemoveTaskEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("The Event ID of the task to remove.")]
    private string targetEventId;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(targetEventId))
        {
            TaskManager.Instance.CompleteTask(targetEventId);
        }

        return UniTask.FromResult(StoryEventResult.Completed());
    }
}
