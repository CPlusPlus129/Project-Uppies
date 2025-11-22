using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Events/Add Task")]
public class AddTaskEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("The task description to add to the list.")]
    private string taskDescription;
    [SerializeField]
    [Tooltip("The task needs to be completed before player starts next shift.")]
    private bool dueBeforeShiftStarts;
    [SerializeField]
    [Tooltip("The task needs to be completed to end current shift.")]
    private bool dueBeforeShiftEnds;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(taskDescription))
        {
            // Use this event's ID as the task ID
            var taskData = new TaskManager.TaskData()
            {
                Id = EventId,
                Description = taskDescription,
                dueBeforeShiftStarts = dueBeforeShiftStarts,
                dueBeforeShiftEnds = dueBeforeShiftEnds
            };
            TaskManager.Instance.AddTask(taskData);
        }

        return UniTask.FromResult(StoryEventResult.Completed());
    }
}
