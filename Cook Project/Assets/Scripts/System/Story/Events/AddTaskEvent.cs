using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Events/Add Task")]
public class AddTaskEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("The task description to add to the list.")]
    private string taskDescription;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(taskDescription))
        {
            // Use this event's ID as the task ID
            TaskManager.Instance.AddTask(this.EventId, taskDescription);
        }

        return UniTask.FromResult(StoryEventResult.Completed());
    }
}
