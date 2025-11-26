using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using R3;
using R3.Triggers;
using System;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Events/Wait Until Task Complete")]
public class WaitUntilTaskCompleteEvent : StoryEventAsset
{
    [SerializeField]
    [Tooltip("The task ID to wait.")]
    private string targetTaskId;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(targetTaskId))
        {
            return StoryEventResult.Completed();
        }

        var taskManager = TaskManager.Instance;
        if (taskManager == null)
        {
            return StoryEventResult.Failed("TaskManager not available");
        }

        if (taskManager.IsTaskCompleted(targetTaskId))
        {
            return StoryEventResult.Completed();
        }

        try
        {
            var destroyedAsObservable = taskManager.gameObject
                .OnDestroyAsObservable();

            await taskManager
                .OnTaskCompleted
                .Where(taskId => taskId == targetTaskId)
                .TakeUntil(destroyedAsObservable)
                .FirstAsync(cancellationToken);

            return StoryEventResult.Completed();
        }
        catch (OperationCanceledException)
        {
            return StoryEventResult.Failed("Operation cancelled");
        }
        catch (InvalidOperationException)
        {
            return StoryEventResult.Failed("TaskManager was destroyed before task completed");
        }
    }
}
