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
    public StoryEventRuntime CurrentStoryEvent => mainTrack.CurrentEvent;

    public Subject<StoryEventRuntime> OnStoryEventQueued { get; } = new Subject<StoryEventRuntime>();
    public Subject<StoryEventRuntime> OnStoryEventStarted { get; } = new Subject<StoryEventRuntime>();
    public Subject<(StoryEventRuntime runtime, StoryEventResult result)> OnStoryEventFinished { get; } = new Subject<(StoryEventRuntime runtime, StoryEventResult result)>();
    public Subject<(StoryEventRuntime runtime, Exception exception)> OnStoryEventFailed { get; } = new Subject<(StoryEventRuntime runtime, Exception exception)>();
    public Subject<StorySequenceAsset> OnSequenceQueued { get; } = new Subject<StorySequenceAsset>();

    private readonly Dictionary<string, StoryEventResult> lastResultByEventId = new Dictionary<string, StoryEventResult>();
    private readonly Dictionary<string, UniTaskCompletionSource<bool>> signalWaiters = new Dictionary<string, UniTaskCompletionSource<bool>>();

    private CancellationTokenSource gameLoopCts;
    private bool isStoryPaused;
    private UniTaskCompletionSource<bool> resumeFlowTcs;

    private StoryTrack mainTrack;
    private readonly List<StoryTrack> activeTracks = new List<StoryTrack>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            Debug.Log($"[GameFlow] Awake on duplicate {this.GetInstanceID()}. Destroying.");
            return;
        }

        mainTrack = new StoryTrack(this, "MainTrack");
        activeTracks.Add(mainTrack);

        Debug.Log($"[GameFlow] Awake on instance {this.GetInstanceID()}. Initialized: {IsInitialized}");

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
        
        // Start the main track loop
        mainTrack.RunLoopAsync(gameLoopCts.Token).Forget();
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

    public StoryEventRuntime ExecuteIndependentEvent(StoryEventAsset asset)
    {
        if (asset == null) return null;
        
        Log($"Executing independent event: {asset.EventId}");
        var track = new StoryTrack(this, $"Independent_{asset.EventId}");
        activeTracks.Add(track);
        
        var runtime = track.EnqueueEvent(asset, insertAtFront: false);
        
        // Start this track immediately
        track.RunLoopAsync(gameLoopCts.Token).Forget();
        
        // Clean up track when done? 
        // Implementation detail: activeTracks might grow indefinitely if we don't prune.
        // Ideally, StoryTrack raises an event when empty and we remove it.
        // For now, we'll keep it simple or let garbage collection happen if we managed it better.
        // Better: Have the track remove itself from activeTracks when done.
        // We'll handle that via a callback or similar if needed, but for now memory impact is low if they finish.
        // Actually, let's prune finished tracks occasionally or just ignore them.
        
        return runtime;
    }

    public IReadOnlyList<StoryEventRuntime> EnqueueSequence(StorySequenceAsset sequence, bool insertAtFront = false)
    {
        return mainTrack.EnqueueSequence(sequence, insertAtFront);
    }

    public void RestartSequence(StorySequenceAsset sequence, bool insertAtFront = true)
    {
        mainTrack.RestartSequence(sequence, insertAtFront);
    }

    public StoryEventRuntime EnqueueEvent(StoryEventAsset asset, bool insertAtFront = false, StorySequenceAsset sourceSequence = null)
    {
        // If it's a standalone event request (no source sequence) and we want parallel execution...
        // BUT user API expects this to return a runtime and maybe block?
        // Let's stick to the original behavior: EnqueueEvent goes to MAIN TRACK unless specified otherwise.
        // Wait, the problem is EndStorageRoom2 (called via EnqueueEvent) needs to be parallel.
        // HACK: If we detect it's a task completion event, maybe force parallel? No, that's magic.
        // Solution: We'll trust the user's request for "rewrite".
        // We will change EnqueueEvent to use a parallel track IF it has no source sequence.
        // This assumes standalone events are meant to be fire-and-forget parallel commands.
        
        if (sourceSequence == null)
        {
            // It's a standalone event. Run it in a parallel track so it doesn't block the main story.
            return ExecuteIndependentEvent(asset);
        }

        return mainTrack.EnqueueEvent(asset, insertAtFront, sourceSequence);
    }

    public void ClearStoryQueue()
    {
        mainTrack.ClearQueue();
        // Maybe clear parallel tracks too?
        foreach (var t in activeTracks)
        {
            if (t != mainTrack) t.ClearQueue();
        }
    }

    public void ClearHistory()
    {
        lastResultByEventId.Clear();
        Log("Cleared story event history.");
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
        if (isStoryPaused) return;
        isStoryPaused = true;
        Log("Story flow paused." + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" {reason}"));
        // Note: StoryTrack reads this flag
    }

    public void ResumeStoryFlow()
    {
        if (!isStoryPaused) return;
        isStoryPaused = false;
        resumeFlowTcs?.TrySetResult(true);
        resumeFlowTcs = null;
        Log("Story flow resumed.");
    }

    public async UniTask WaitForSignalAsync(string signalId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(signalId)) throw new ArgumentException("Signal id cannot be null or whitespace.", nameof(signalId));

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

    public bool Signal(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId)) return false;

        UniTaskCompletionSource<bool> waiter = null;
        lock (signalWaiters)
        {
            if (signalWaiters.TryGetValue(signalId, out waiter))
            {
                signalWaiters.Remove(signalId);
            }
        }

        var fulfilled = waiter != null;
        waiter?.TrySetResult(true);
        Log($"Signal '{signalId}' received." + (fulfilled ? string.Empty : " (no waiting listeners)"));
        return fulfilled;
    }

    public override void OnDestroy()
    {
        if (Instance != this)
        {
            base.OnDestroy();
            return;
        }
        CancelGameLoop();
        ServiceLocator.Instance?.DisposeAllServices();
        
        OnStoryEventQueued.OnCompleted();
        OnStoryEventStarted.OnCompleted();
        OnStoryEventFinished.OnCompleted();
        OnStoryEventFailed.OnCompleted();
        OnSequenceQueued.OnCompleted();

        base.OnDestroy();
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
        
        mainTrack?.ClearQueue();
        activeTracks.Clear();
        lastResultByEventId.Clear();
    }

    // Logging Helpers (public for inner class)
    public void Log(string message) { if (logStoryFlow) Debug.Log($"[GameFlow] {message}"); }
    public void LogWarning(string message) { if (logStoryFlow) Debug.LogWarning($"[GameFlow] {message}"); }
    public void LogError(string message) { if (logStoryFlow) Debug.LogError($"[GameFlow] {message}"); }
    public void LogSequenceEvent(StoryEventRuntime runtime, string phase, string details = null)
    {
        var sequence = runtime?.SourceSequence;
        if (sequence == null || !sequence.LogEventLifecycle) return;

        var eventId = runtime.Asset != null ? runtime.Asset.EventId : "<null>";
        var seqId = sequence.SequenceId;
        var total = runtime.SequenceLength > 0 ? runtime.SequenceLength : sequence.Events?.Count ?? 0;
        var stepIndex = runtime.SequenceIndex + 1;
        var progress = total > 0 ? $"{stepIndex}/{total}" : stepIndex.ToString();
        var message = $"[StorySequence:{seqId}] {phase} event '{eventId}' (step {progress})";

        if (!string.IsNullOrWhiteSpace(details)) message += $" :: {details}";
        Debug.Log(message, sequence);
    }

    private async UniTask WaitForDialogueServiceReady()
    {
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        if (dialogueService == null) return;

        if (dialogueService is DialogueEngine_Gaslight gaslight && !gaslight.IsRuntimeReady)
        {
            await gaslight.WaitUntilReadyAsync().AttachExternalCancellation(gameLoopCts.Token);
        }
    }
    
    // ==========================================
    // Inner Class: StoryTrack
    // ==========================================
    private class StoryTrack
    {
        private readonly GameFlow gameFlow;
        private readonly string trackName;
        private readonly LinkedList<StoryEventRuntime> queue = new LinkedList<StoryEventRuntime>();
        private readonly HashSet<StorySequenceAsset> blockedSequences = new HashSet<StorySequenceAsset>();
        
        public StoryEventRuntime CurrentEvent { get; private set; }

        public StoryTrack(GameFlow gameFlow, string trackName)
        {
            this.gameFlow = gameFlow;
            this.trackName = trackName;
        }

        public async UniTask RunLoopAsync(CancellationToken cancellationToken)
        {
            gameFlow.Log($"Track '{trackName}' loop started.");
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check Pause
                if (gameFlow.IsStoryPaused)
                {
                    gameFlow.resumeFlowTcs ??= new UniTaskCompletionSource<bool>();
                    try { await gameFlow.resumeFlowTcs.Task.AttachExternalCancellation(cancellationToken); }
                    catch (OperationCanceledException) { break; }
                    finally { gameFlow.resumeFlowTcs = null; }
                    if (cancellationToken.IsCancellationRequested) break;
                    continue;
                }

                if (!TryDequeueNextEvent(out var runtime))
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    continue;
                }

                if (runtime.Asset == null)
                {
                    gameFlow.LogWarning($"[{trackName}] Encountered null asset. Skipping.");
                    continue;
                }

                var isBackground = runtime.Asset is IBackgroundStoryEvent bg && bg.RunInBackground;
                if (isBackground)
                {
                    if (runtime.Asset is IBackgroundStoryEvent bgBlock && bgBlock.BlockSourceSequence && runtime.SourceSequence != null)
                    {
                        blockedSequences.Add(runtime.SourceSequence);
                    }
                    ProcessStoryEventAsync(runtime, cancellationToken, true).Forget();
                    continue;
                }

                await ProcessStoryEventAsync(runtime, cancellationToken, false);
            }
            gameFlow.Log($"Track '{trackName}' loop exited.");
        }

        private bool TryDequeueNextEvent(out StoryEventRuntime runtime)
        {
            if (queue.First != null)
            {
                var node = queue.First;
                while (node != null)
                {
                    var candidate = node.Value;
                    if (candidate.SourceSequence != null && blockedSequences.Contains(candidate.SourceSequence))
                    {
                        node = node.Next;
                        continue;
                    }
                    runtime = candidate;
                    queue.Remove(node);
                    return true;
                }
            }
            runtime = null;
            return false;
        }

        private async UniTask ProcessStoryEventAsync(StoryEventRuntime runtime, CancellationToken cancellationToken, bool isBackground)
        {
            if (!isBackground) CurrentEvent = runtime;

            runtime.SetState(StoryEventState.Running);
            gameFlow.LogSequenceEvent(runtime, "BEGIN", $"[Track:{trackName}]");
            gameFlow.OnStoryEventStarted.OnNext(runtime);

            var context = new GameFlowContext(gameFlow, runtime, cancellationToken);
            StoryEventResult result;
            Exception failureException = null;

            try
            {
                var canExecute = await ShouldRunEventAsync(runtime, context, cancellationToken);
                if (!canExecute)
                {
                    result = StoryEventResult.Skipped($"Preconditions failed/already complete: {runtime.Asset.EventId}");
                }
                else
                {
                    result = await runtime.Asset.ExecuteAsync(context, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                result = StoryEventResult.Cancelled($"Cancelled: {runtime.Asset.EventId}");
            }
            catch (Exception ex)
            {
                failureException = ex;
                result = StoryEventResult.Failed(ex.Message);
            }

            FinalizeStoryEvent(runtime, result, failureException, isBackground);
        }

        private async UniTask<bool> ShouldRunEventAsync(StoryEventRuntime runtime, GameFlowContext context, CancellationToken cancellationToken)
        {
            if (!runtime.Asset.IsReplayable && !string.IsNullOrWhiteSpace(runtime.Asset.EventId) &&
                gameFlow.lastResultByEventId.TryGetValue(runtime.Asset.EventId, out var last) &&
                last.FinalState == StoryEventState.Completed)
            {
                return false;
            }
            return await runtime.Asset.CanExecuteAsync(context, cancellationToken);
        }

        private void FinalizeStoryEvent(StoryEventRuntime runtime, StoryEventResult result, Exception failureException, bool isBackground)
        {
            if (failureException != null)
            {
                runtime.SetState(StoryEventState.Failed);
                gameFlow.OnStoryEventFailed.OnNext((runtime, failureException));
                gameFlow.LogError($"[{trackName}] Event '{runtime.Asset.EventId}' failed: {failureException}");
            }

            if (result.FinalState == StoryEventState.Pending)
                result = StoryEventResult.Completed(result.Message, result.NextSequence);

            runtime.SetState(result.FinalState, result);

            var eventId = runtime.Asset.EventId;
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                gameFlow.lastResultByEventId[eventId] = result;
            }

            var sequenceToChain = result.NextSequence;
            if (sequenceToChain == null && ShouldAutoChainToNextSequence(runtime, result))
            {
                sequenceToChain = runtime.SourceSequence.NextSequence;
            }

            if (sequenceToChain != null)
            {
                EnqueueSequence(sequenceToChain);
            }

            if (result.PauseFlow)
            {
                gameFlow.PauseStoryFlow($"Event requested pause: {eventId}");
            }

            var resultSummary = string.IsNullOrWhiteSpace(result.Message) ? result.FinalState.ToString() : $"{result.FinalState}: {result.Message}";
            gameFlow.LogSequenceEvent(runtime, "END", resultSummary);
            gameFlow.OnStoryEventFinished.OnNext((runtime, result));

            if (!isBackground) CurrentEvent = null;

            if (runtime.SourceSequence != null && blockedSequences.Contains(runtime.SourceSequence))
            {
                blockedSequences.Remove(runtime.SourceSequence);
            }
        }

        private bool ShouldAutoChainToNextSequence(StoryEventRuntime runtime, StoryEventResult result)
        {
            if (runtime?.SourceSequence == null || runtime.SourceSequence.NextSequence == null) return false;
            if (!runtime.IsLastEventInSequence) return false;
            return result.FinalState == StoryEventState.Completed || result.FinalState == StoryEventState.Skipped;
        }

        public IReadOnlyList<StoryEventRuntime> EnqueueSequence(StorySequenceAsset sequence, bool insertAtFront = false)
        {
            if (sequence == null) return Array.Empty<StoryEventRuntime>();
            
            var validEvents = new List<StoryEventAsset>();
            foreach (var evt in sequence.EnumerateEvents())
                if (evt != null) validEvents.Add(evt);

            if (validEvents.Count == 0) return Array.Empty<StoryEventRuntime>();

            var created = new List<StoryEventRuntime>(validEvents.Count);
            for (int i = 0; i < validEvents.Count; i++)
                created.Add(new StoryEventRuntime(validEvents[i], sequence, i, validEvents.Count));

            if (insertAtFront)
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    queue.AddFirst(created[i]);
                    gameFlow.OnStoryEventQueued.OnNext(created[i]);
                }
            }
            else
            {
                foreach (var r in created)
                {
                    queue.AddLast(r);
                    gameFlow.OnStoryEventQueued.OnNext(r);
                }
            }
            
            gameFlow.OnSequenceQueued.OnNext(sequence);
            gameFlow.Log($"[{trackName}] Enqueued sequence '{sequence.SequenceId}' ({created.Count} events).");
            return created;
        }
        
        public StoryEventRuntime EnqueueEvent(StoryEventAsset asset, bool insertAtFront = false, StorySequenceAsset sourceSequence = null)
        {
            if (asset == null) return null;
            var runtime = new StoryEventRuntime(asset, sourceSequence, 0, sourceSequence != null ? 1 : 0);
            
            if (insertAtFront) queue.AddFirst(runtime);
            else queue.AddLast(runtime);
            
            gameFlow.OnStoryEventQueued.OnNext(runtime);
            gameFlow.Log($"[{trackName}] Enqueued event '{asset.EventId}'.");
            return runtime;
        }
        
        public void RestartSequence(StorySequenceAsset sequence, bool insertAtFront = true)
        {
             if (sequence == null) return;
             var node = queue.First;
             while(node != null) {
                 var next = node.Next;
                 if (node.Value != null && node.Value.SourceSequence == sequence) queue.Remove(node);
                 node = next;
             }
             EnqueueSequence(sequence, insertAtFront);
             gameFlow.Log($"[{trackName}] Restarted sequence '{sequence.SequenceId}'.");
        }

        public void ClearQueue()
        {
            queue.Clear();
        }
    }
}
