using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

[CreateAssetMenu(fileName = "WaitForTaskStoryEvent", menuName = "Game Flow/Flow Control/Wait For Task")]
public sealed class WaitForTaskStoryEventAsset : StoryEventAsset, IBackgroundStoryEvent
{
    [Header("Task Configuration")]
    [SerializeField]
    [Tooltip("The Task ID to wait for.")]
    private string taskId;

    [SerializeField]
    [Tooltip("If true, this event completes immediately if the task is already in the completion history.")]
    private bool checkHistory = true;

    [SerializeField]
    [Tooltip("If true, this event runs in the background (not blocking the global queue).")]
    private bool runInBackground = false;

    [SerializeField]
    [Tooltip("If true, and RunInBackground is true, this event will block the REST of its own source sequence.")]
    private bool blockSourceSequence = false;

    // IBackgroundStoryEvent implementation
    public bool RunInBackground => runInBackground;
    public bool BlockSourceSequence => blockSourceSequence;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return StoryEventResult.Failed("WaitForTaskStoryEventAsset: Task ID is empty.");
        }

        var taskManager = TaskManager.Instance;
        if (taskManager == null)
        {
            return StoryEventResult.Failed("WaitForTaskStoryEventAsset: TaskManager instance not found.");
        }

        // 1. Check immediate completion
        if (checkHistory && taskManager.IsTaskCompleted(taskId))
        {
            return StoryEventResult.Completed($"Task '{taskId}' was already complete.");
        }

        // blocking check removed as we now use IBackgroundStoryEvent for flow control

        // 2. Wait for completion
        var tcs = new UniTaskCompletionSource<bool>();
        
        // Subscribe using R3
        using var subscription = taskManager.OnTaskCompleted
            .Where(completedId => completedId == taskId)
            .Subscribe(_ => tcs.TrySetResult(true));

        // Double-check in case it completed while we were subscribing (unlikely on main thread, but safe)
        if (checkHistory && taskManager.IsTaskCompleted(taskId))
        {
            return StoryEventResult.Completed($"Task '{taskId}' completed during initialization.");
        }

        try
        {
            await tcs.Task.AttachExternalCancellation(cancellationToken);
            return StoryEventResult.Completed($"Task '{taskId}' completed.");
        }
        catch (System.OperationCanceledException)
        {
            return StoryEventResult.Cancelled($"Wait for task '{taskId}' was cancelled.");
        }
    }
}
