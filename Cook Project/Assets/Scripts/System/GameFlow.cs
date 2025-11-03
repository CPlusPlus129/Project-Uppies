using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class GameFlow : MonoSingleton<GameFlow>
{
    [Header("Story Flow")]
    [SerializeField]
    [Tooltip("Sequence automatically enqueued once core services finish initialization.")]
    private StorySequenceAsset startingSequence;

    [SerializeField]
    [Tooltip("If enabled, the starting sequence is automatically enqueued on boot.")]
    private bool autoPlayStartingSequence = true;

    [SerializeField]
    [Tooltip("Logs detailed story flow information to the Unity console.")]
    private bool logStoryFlow = false;

    public bool IsInitialized { get; private set; } = false;
    public bool IsStoryPaused => isStoryPaused;
    public StoryEventRuntime CurrentStoryEvent => currentStoryEvent;

    public Subject<StoryEventRuntime> OnStoryEventQueued { get; } = new Subject<StoryEventRuntime>();
    public Subject<StoryEventRuntime> OnStoryEventStarted { get; } = new Subject<StoryEventRuntime>();
    public Subject<(StoryEventRuntime runtime, StoryEventResult result)> OnStoryEventFinished { get; } = new Subject<(StoryEventRuntime runtime, StoryEventResult result)>();
    public Subject<(StoryEventRuntime runtime, Exception exception)> OnStoryEventFailed { get; } = new Subject<(StoryEventRuntime runtime, Exception exception)>();
    public Subject<StorySequenceAsset> OnSequenceQueued { get; } = new Subject<StorySequenceAsset>();

    private readonly LinkedList<StoryEventRuntime> storyQueue = new LinkedList<StoryEventRuntime>();
    private readonly Dictionary<string, StoryEventResult> lastResultByEventId = new Dictionary<string, StoryEventResult>();
    private readonly Dictionary<string, UniTaskCompletionSource<bool>> signalWaiters = new Dictionary<string, UniTaskCompletionSource<bool>>();

    private CancellationTokenSource gameLoopCts;
    private StoryEventRuntime currentStoryEvent;
    private bool isStoryPaused;
    private UniTaskCompletionSource<bool> resumeFlowTcs;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        if (!IsInitialized)
        {
            StartGame().Forget();
        }
    }

    private async UniTask StartGame()
    {
        IsInitialized = false;
        CancelGameLoop();

        gameLoopCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        await InitServices();
        await LoadTables();
        await SetupGame();

        if (autoPlayStartingSequence && startingSequence != null)
        {
            EnqueueSequence(startingSequence);
        }

        IsInitialized = true;
        await WaitForDialogueServiceReady();
        RunStoryLoopAsync(gameLoopCts.Token).Forget();
    }

    private async UniTask InitServices()
    {
        await ServiceLocator.Instance.Init();
    }

    private async UniTask LoadTables()
    {
        var tableManager = await ServiceLocator.Instance.GetAsync<ITableManager>();
        await tableManager.LoadAllTables();
    }

    private async UniTask SetupGame()
    {
        await UniTask.CompletedTask;
    }

    private async UniTask RunStoryLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (isStoryPaused)
            {
                resumeFlowTcs ??= new UniTaskCompletionSource<bool>();
                try
                {
                    await resumeFlowTcs.Task.AttachExternalCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                finally
                {
                    resumeFlowTcs = null;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            if (!TryDequeueNextEvent(out var runtime))
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                continue;
            }

            if (runtime.Asset == null)
            {
                LogWarning("Encountered a null StoryEventAsset while processing the queue. Skipping.");
                continue;
            }

            currentStoryEvent = runtime;
            runtime.SetState(StoryEventState.Running);
            OnStoryEventStarted.OnNext(runtime);

            var context = new GameFlowContext(this, runtime, cancellationToken);
            StoryEventResult result;
            Exception failureException = null;

            try
            {
                var canExecute = await ShouldRunEventAsync(runtime, context, cancellationToken);
                if (!canExecute)
                {
                    result = StoryEventResult.Skipped($"Preconditions failed or event is not replayable: {runtime.Asset.EventId}");
                }
                else
                {
                    result = await runtime.Asset.ExecuteAsync(context, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                result = StoryEventResult.Cancelled($"Story event cancelled: {runtime.Asset.EventId}");
            }
            catch (Exception ex)
            {
                failureException = ex;
                result = StoryEventResult.Failed(ex.Message);
            }

            if (failureException != null)
            {
                runtime.SetState(StoryEventState.Failed);
                OnStoryEventFailed.OnNext((runtime, failureException));
                LogError($"Story event '{runtime.Asset.EventId}' threw an exception: {failureException}");
            }

            if (result.FinalState == StoryEventState.Pending)
            {
                result = StoryEventResult.Completed(result.Message, result.NextSequence);
            }

            runtime.SetState(result.FinalState, result);

            var eventId = runtime.Asset.EventId;
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                lastResultByEventId[eventId] = result;
            }

            if (result.NextSequence != null)
            {
                EnqueueSequence(result.NextSequence);
            }

            if (result.PauseFlow)
            {
                PauseStoryFlow($"Event requested pause: {eventId}");
            }

            OnStoryEventFinished.OnNext((runtime, result));
            currentStoryEvent = null;
        }
    }

    private async UniTask<bool> ShouldRunEventAsync(StoryEventRuntime runtime, GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!runtime.Asset.IsReplayable &&
            !string.IsNullOrWhiteSpace(runtime.Asset.EventId) &&
            lastResultByEventId.TryGetValue(runtime.Asset.EventId, out var lastResult) &&
            lastResult.FinalState == StoryEventState.Completed)
        {
            return false;
        }

        return await runtime.Asset.CanExecuteAsync(context, cancellationToken);
    }

    private bool TryDequeueNextEvent(out StoryEventRuntime runtime)
    {
        if (storyQueue.First != null)
        {
            runtime = storyQueue.First.Value;
            storyQueue.RemoveFirst();
            return true;
        }

        runtime = null;
        return false;
    }

    public StoryEventRuntime[] GetPendingEventsSnapshot()
    {
        var array = new StoryEventRuntime[storyQueue.Count];
        storyQueue.CopyTo(array, 0);
        return array;
    }

    public IReadOnlyDictionary<string, StoryEventResult> StoryHistory => lastResultByEventId;

    public IReadOnlyList<StoryEventRuntime> EnqueueSequence(StorySequenceAsset sequence, bool insertAtFront = false)
    {
        if (sequence == null)
        {
            return Array.Empty<StoryEventRuntime>();
        }

        var created = new List<StoryEventRuntime>();
        int index = 0;
        foreach (var storyEvent in sequence.EnumerateEvents())
        {
            if (storyEvent == null)
            {
                continue;
            }

            var runtime = new StoryEventRuntime(storyEvent, sequence, index++);
            created.Add(runtime);
        }

        if (created.Count == 0)
        {
            Log($"Sequence '{sequence.SequenceId}' has no valid events to enqueue.");
            return created;
        }

        if (insertAtFront)
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                storyQueue.AddFirst(created[i]);
                OnStoryEventQueued.OnNext(created[i]);
            }
        }
        else
        {
            foreach (var runtime in created)
            {
                storyQueue.AddLast(runtime);
                OnStoryEventQueued.OnNext(runtime);
            }
        }

        OnSequenceQueued.OnNext(sequence);
        Log($"Enqueued sequence '{sequence.SequenceId}' with {created.Count} events" + (insertAtFront ? " at the front of the queue." : "."));

        return created;
    }

    public StoryEventRuntime EnqueueEvent(StoryEventAsset asset, bool insertAtFront = false, StorySequenceAsset sourceSequence = null)
    {
        if (asset == null)
        {
            return null;
        }

        var runtime = new StoryEventRuntime(asset, sourceSequence, 0);

        if (insertAtFront)
        {
            storyQueue.AddFirst(runtime);
        }
        else
        {
            storyQueue.AddLast(runtime);
        }

        OnStoryEventQueued.OnNext(runtime);
        Log($"Enqueued story event '{asset.EventId}'" + (insertAtFront ? " at the front of the queue." : "."));

        return runtime;
    }

    public void ClearStoryQueue()
    {
        storyQueue.Clear();
        Log("Cleared all pending story events.");
    }

    public bool TryGetStoryEventResult(string eventId, out StoryEventResult result)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            result = default;
            return false;
        }

        return lastResultByEventId.TryGetValue(eventId, out result);
    }

    public bool HasEventCompleted(string eventId)
    {
        return TryGetStoryEventResult(eventId, out var result) && result.FinalState == StoryEventState.Completed;
    }

    public void PauseStoryFlow(string reason = null)
    {
        if (isStoryPaused)
        {
            return;
        }

        isStoryPaused = true;
        Log("Story flow paused." + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" {reason}"));
    }

    public void ResumeStoryFlow()
    {
        if (!isStoryPaused)
        {
            return;
        }

        isStoryPaused = false;
        resumeFlowTcs?.TrySetResult(true);
        resumeFlowTcs = null;
        Log("Story flow resumed.");
    }

    public async UniTask WaitForSignalAsync(string signalId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(signalId))
        {
            throw new ArgumentException("Signal id cannot be null or whitespace.", nameof(signalId));
        }

        UniTaskCompletionSource<bool> waiter;

        lock (signalWaiters)
        {
            if (!signalWaiters.TryGetValue(signalId, out waiter) || waiter.Task.Status != UniTaskStatus.Pending)
            {
                waiter = new UniTaskCompletionSource<bool>();
                signalWaiters[signalId] = waiter;
            }
        }

        try
        {
            await waiter.Task.AttachExternalCancellation(cancellationToken);
        }
        finally
        {
            lock (signalWaiters)
            {
                if (signalWaiters.TryGetValue(signalId, out var existing) && existing == waiter && waiter.Task.Status != UniTaskStatus.Pending)
                {
                    signalWaiters.Remove(signalId);
                }
            }
        }
    }

    public void Signal(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId))
        {
            return;
        }

        UniTaskCompletionSource<bool> waiter = null;

        lock (signalWaiters)
        {
            if (signalWaiters.TryGetValue(signalId, out waiter))
            {
                signalWaiters.Remove(signalId);
            }
        }

        waiter?.TrySetResult(true);
        Log($"Signal '{signalId}' received.");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CancelGameLoop();

        OnStoryEventQueued.OnCompleted();
        OnStoryEventStarted.OnCompleted();
        OnStoryEventFinished.OnCompleted();
        OnStoryEventFailed.OnCompleted();
        OnSequenceQueued.OnCompleted();
    }

    private void CancelGameLoop()
    {
        if (gameLoopCts != null)
        {
            gameLoopCts.Cancel();
            gameLoopCts.Dispose();
            gameLoopCts = null;
        }

        resumeFlowTcs?.TrySetCanceled();
        resumeFlowTcs = null;
        isStoryPaused = false;
        currentStoryEvent = null;
        storyQueue.Clear();
        lastResultByEventId.Clear();
    }

    private void Log(string message)
    {
        if (logStoryFlow)
        {
            Debug.Log($"[GameFlow] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (logStoryFlow)
        {
            Debug.LogWarning($"[GameFlow] {message}");
        }
    }

    private void LogError(string message)
    {
        if (logStoryFlow)
        {
            Debug.LogError($"[GameFlow] {message}");
        }
    }

    private async UniTask WaitForDialogueServiceReady()
    {
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        if (dialogueService == null)
        {
            return;
        }

        if (dialogueService is DialogueEngine_Gaslight gaslight && !gaslight.IsRuntimeReady)
        {
            await gaslight.WaitUntilReadyAsync().AttachExternalCancellation(gameLoopCts.Token);
        }
    }
}
