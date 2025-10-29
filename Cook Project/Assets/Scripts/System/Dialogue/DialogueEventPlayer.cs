using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine;
using UnityEngine.Events;
using R3;

[DisallowMultipleComponent]
public class DialogueEventPlayer : MonoBehaviour
{
    [SerializeField]
    private DialogueEventAsset defaultEvent;

    [SerializeField]
    [Tooltip("Optional collection that will be used when calling Play()/PlayAndQueue without specifying an event directly.")]
    private DialogueEventCollection defaultCollection;

    [SerializeField]
    [Tooltip("Override the collection's default playback mode when this player consumes it.")]
    private bool overrideCollectionMode = false;

    [SerializeField]
    private DialogueEventCollection.PlaybackMode collectionPlaybackOverride = DialogueEventCollection.PlaybackMode.Sequential;

    [SerializeField]
    [Tooltip("Reset sequential/random state whenever this component is enabled.")]
    private bool resetCollectionStateOnEnable = false;

    [SerializeField]
    [Tooltip("Reset sequential/random state whenever this component is disabled.")]
    private bool resetCollectionStateOnDisable = true;

    [SerializeField]
    [Tooltip("If enabled, the component will request the dialogue service from the ServiceLocator when needed.")]
    private bool resolveServiceFromLocator = true;

    [SerializeField]
    [Tooltip("Optional manual reference to a dialogue service. Overrides ServiceLocator resolution when assigned.")]
    private MonoBehaviour dialogueServiceBehaviour;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueCompleted;
    [SerializeField] private UnityEvent onDialogueDenied;
    [SerializeField] private bool enableDebugLogging = false;

    private readonly Queue<PlaybackRequest> queuedEvents = new();
    private readonly HashSet<DialogueEventAsset> playedAssets = new();
    private readonly Dictionary<DialogueEventCollection, CollectionState> collectionStates = new();

    private IDialogueService cachedService;
    private bool isPlaying;
    private DialogueEventAsset lastPlayedAsset;
    private DialogueEventCollection lastPlayedCollection;
    private DialogueEventCollection activeCollection;

    private void Awake()
    {
        if (activeCollection == null && defaultCollection != null)
        {
            activeCollection = defaultCollection;
        }
    }

    private void OnEnable()
    {
        if (activeCollection == null && defaultCollection != null)
        {
            activeCollection = defaultCollection;
        }

        if (resetCollectionStateOnEnable)
        {
            ResetCollectionState(activeCollection);
        }
    }

    private void OnDisable()
    {
        if (resetCollectionStateOnDisable)
        {
            ResetCollectionState(activeCollection);
        }
    }

    /// <summary>
    /// Plays the default event immediately if possible.
    /// </summary>
    public void Play()
    {
        DebugLog($"Play() requested on {name} (default event: {defaultEvent?.name ?? "<null>"}, default collection: {activeCollection?.name ?? "<none>"})");
        PlayDefaultAsync(false).Forget();
    }

    /// <summary>
    /// Queues the default event if another dialogue is already running; otherwise plays immediately.
    /// </summary>
    public void PlayAndQueue()
    {
        PlayDefaultAsync(true).Forget();
    }

    /// <summary>
    /// Attempts to play the default event if conditions allow it.
    /// </summary>
    public bool TryPlay()
    {
        if (activeCollection != null && !activeCollection.IsEmpty)
        {
            return TryPlayCollection(activeCollection, overrideCollectionMode ? collectionPlaybackOverride : (DialogueEventCollection.PlaybackMode?)null);
        }

        return TryPlay(defaultEvent);
    }

    /// <summary>
    /// Plays the provided dialogue event immediately if possible.
    /// </summary>
    public UniTask Play(DialogueEventAsset dialogueEventAsset)
    {
        return PlayInternalAsync(dialogueEventAsset, false, null, false);
    }

    /// <summary>
    /// Queues the provided dialogue event if another dialogue is already running; otherwise plays immediately.
    /// </summary>
    public UniTask PlayAndQueue(DialogueEventAsset dialogueEventAsset)
    {
        return PlayInternalAsync(dialogueEventAsset, true, null, false);
    }

    /// <summary>
    /// Attempts to play the dialogue event if it is valid and not blocked.
    /// </summary>
    public bool TryPlay(DialogueEventAsset dialogueEventAsset)
    {
        DebugLog($"TryPlay() requested on {name} (event: {dialogueEventAsset?.name ?? "<null>"})");
        if (dialogueEventAsset == null)
        {
            Debug.LogWarning($"[{nameof(DialogueEventPlayer)}] Missing DialogueEventAsset on {name}.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (dialogueEventAsset.PlayOnce && playedAssets.Contains(dialogueEventAsset))
        {
            DebugLog($"Denied: {dialogueEventAsset.name} marked PlayOnce and already played.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (isPlaying)
        {
            DebugLog("Denied: dialogue currently playing.");
            onDialogueDenied?.Invoke();
            return false;
        }

        PlayInternalAsync(dialogueEventAsset, false, null, false).Forget();
        return true;
    }

    public UniTask PlayCollection(DialogueEventCollection collection)
    {
        return PlayCollectionInternal(collection, false, null);
    }

    public UniTask PlayCollection(DialogueEventCollection collection, DialogueEventCollection.PlaybackMode modeOverride)
    {
        return PlayCollectionInternal(collection, false, modeOverride);
    }

    public UniTask PlayCollectionAndQueue(DialogueEventCollection collection)
    {
        return PlayCollectionInternal(collection, true, null);
    }

    public UniTask PlayCollectionAndQueue(DialogueEventCollection collection, DialogueEventCollection.PlaybackMode modeOverride)
    {
        return PlayCollectionInternal(collection, true, modeOverride);
    }

    public bool TryPlayCollection(DialogueEventCollection collection, DialogueEventCollection.PlaybackMode? modeOverride = null)
    {
        if (collection == null)
        {
            DebugLog("TryPlayCollection denied: collection is null.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (collection.IsEmpty)
        {
            DebugLog($"TryPlayCollection denied: collection {collection.name} has no entries.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (isPlaying)
        {
            DebugLog("TryPlayCollection denied: dialogue currently playing.");
            onDialogueDenied?.Invoke();
            return false;
        }

        var mode = modeOverride ?? (overrideCollectionMode ? collectionPlaybackOverride : collection.DefaultPlayback);
        var asset = SelectNextDialogue(collection, mode);
        if (asset == null)
        {
            DebugLog($"TryPlayCollection denied: unable to resolve next entry from {collection.name}.");
            onDialogueDenied?.Invoke();
            return false;
        }

        PlayInternalAsync(asset, false, collection, false).Forget();
        return true;
    }

    public bool RepeatLastDialogue(bool queueIfBusy = false, bool ignorePlayOnce = false)
    {
        if (lastPlayedAsset == null)
        {
            DebugLog("RepeatLastDialogue denied: no previous dialogue.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (!ignorePlayOnce && lastPlayedAsset.PlayOnce && playedAssets.Contains(lastPlayedAsset))
        {
            DebugLog($"RepeatLastDialogue denied: {lastPlayedAsset.name} marked PlayOnce and already played.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (isPlaying)
        {
            if (!queueIfBusy)
            {
                DebugLog("RepeatLastDialogue denied: dialogue currently playing and queueIfBusy=false.");
                onDialogueDenied?.Invoke();
                return false;
            }

            queuedEvents.Enqueue(new PlaybackRequest
            {
                Asset = lastPlayedAsset,
                SourceCollection = lastPlayedCollection,
                IgnorePlayOnce = ignorePlayOnce
            });
            DebugLog($"RepeatLastDialogue queued: {lastPlayedAsset.name}.");
            return true;
        }

        PlayInternalAsync(lastPlayedAsset, false, lastPlayedCollection, ignorePlayOnce).Forget();
        return true;
    }

    private async UniTask PlayInternalAsync(DialogueEventAsset dialogueEventAsset, bool allowQueue, DialogueEventCollection sourceCollection, bool ignorePlayOnce)
    {
        if (dialogueEventAsset == null)
        {
            Debug.LogWarning($"[{nameof(DialogueEventPlayer)}] Missing DialogueEventAsset on {name}.");
            onDialogueDenied?.Invoke();
            return;
        }

        if (!ignorePlayOnce && dialogueEventAsset.PlayOnce && playedAssets.Contains(dialogueEventAsset))
        {
            DebugLog($"PlayInternalAsync abort: {dialogueEventAsset.name} already recorded as played once.");
            onDialogueDenied?.Invoke();
            return;
        }

        if (isPlaying)
        {
            if (allowQueue)
            {
                DebugLog($"Queueing dialogue {dialogueEventAsset.name} (collection: {sourceCollection?.name ?? "<none>"}).");
                queuedEvents.Enqueue(new PlaybackRequest
                {
                    Asset = dialogueEventAsset,
                    SourceCollection = sourceCollection,
                    IgnorePlayOnce = ignorePlayOnce
                });
            }
            else
            {
                DebugLog($"Play request denied: already playing {dialogueEventAsset.name}.");
                onDialogueDenied?.Invoke();
            }
            return;
        }

        var service = await ResolveServiceAsync();
        if (service == null)
        {
            Debug.LogError($"[{nameof(DialogueEventPlayer)}] Failed to resolve IDialogueService.");
            onDialogueDenied?.Invoke();
            return;
        }

        isPlaying = true;
        onDialogueStarted?.Invoke();
        DebugLog($"Starting dialogue {dialogueEventAsset.name} (collection: {sourceCollection?.name ?? "<none>"}).");

        bool completed = false;

        void MarkCompleted()
        {
            if (completed)
            {
                return;
            }

            completed = true;

            if (dialogueEventAsset.PlayOnce)
            {
                playedAssets.Add(dialogueEventAsset);
            }

            lastPlayedAsset = dialogueEventAsset;
            lastPlayedCollection = sourceCollection;

            if (sourceCollection != null)
            {
                var state = GetOrCreateState(sourceCollection);
                state.LastAsset = dialogueEventAsset;
            }
        }

        try
        {
            if (service is DialogueEngine_Gaslight gaslight)
            {
                gaslight.onEndScenario
                    .Take(1)
                    .Subscribe(_ => MarkCompleted())
                    .AddTo(this);
            }

            await service.PlayDialogueAsync(dialogueEventAsset);
            MarkCompleted();
            DebugLog($"Dialogue {dialogueEventAsset.name} completed (PlayOnce={dialogueEventAsset.PlayOnce}).");
            onDialogueCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            onDialogueDenied?.Invoke();
        }
        finally
        {
            isPlaying = false;
        }

        if (queuedEvents.Count > 0)
        {
            var next = queuedEvents.Dequeue();
            DebugLog($"Processing queued dialogue {next.Asset?.name ?? "<null>"} (collection: {next.SourceCollection?.name ?? "<none>"}).");
            await PlayInternalAsync(next.Asset, false, next.SourceCollection, next.IgnorePlayOnce);
        }
    }

    private UniTask PlayDefaultAsync(bool allowQueue)
    {
        if (activeCollection != null && !activeCollection.IsEmpty)
        {
            var modeOverride = overrideCollectionMode ? collectionPlaybackOverride : (DialogueEventCollection.PlaybackMode?)null;
            return PlayCollectionInternal(activeCollection, allowQueue, modeOverride);
        }

        if (defaultEvent != null)
        {
            return PlayInternalAsync(defaultEvent, allowQueue, null, false);
        }

        DebugLog("PlayDefaultAsync aborted: no default event or collection configured.");
        onDialogueDenied?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask PlayCollectionInternal(DialogueEventCollection collection, bool allowQueue, DialogueEventCollection.PlaybackMode? modeOverride)
    {
        if (collection == null)
        {
            DebugLog("PlayCollectionInternal aborted: collection is null.");
            onDialogueDenied?.Invoke();
            return UniTask.CompletedTask;
        }

        if (collection.IsEmpty)
        {
            DebugLog($"PlayCollectionInternal aborted: collection {collection.name} has no entries.");
            onDialogueDenied?.Invoke();
            return UniTask.CompletedTask;
        }

        var mode = modeOverride ?? (overrideCollectionMode && collection == activeCollection
            ? collectionPlaybackOverride
            : collection.DefaultPlayback);

        var asset = SelectNextDialogue(collection, mode);
        if (asset == null)
        {
            DebugLog($"PlayCollectionInternal aborted: unable to resolve next entry from {collection.name}.");
            onDialogueDenied?.Invoke();
            return UniTask.CompletedTask;
        }

        return PlayInternalAsync(asset, allowQueue, collection, false);
    }

    private DialogueEventAsset SelectNextDialogue(DialogueEventCollection collection, DialogueEventCollection.PlaybackMode mode)
    {
        if (collection == null || collection.IsEmpty)
        {
            return null;
        }

        var entries = collection.DialogueEvents;
        var state = GetOrCreateState(collection);

        return mode switch
        {
            DialogueEventCollection.PlaybackMode.Random => SelectRandomEntry(collection, entries, state),
            _ => SelectSequentialEntry(collection, entries, state)
        };
    }

    private DialogueEventAsset SelectSequentialEntry(DialogueEventCollection collection, IReadOnlyList<DialogueEventAsset> entries, CollectionState state)
    {
        int count = entries.Count;
        if (count == 0)
        {
            return null;
        }

        if (!collection.LoopSequential && state.NextIndex >= count)
        {
            DebugLog($"Sequential playback reached end of {collection.name} and looping is disabled.");
            return null;
        }

        int index = state.NextIndex % count;
        var asset = entries[index];

        if (collection.LoopSequential)
        {
            state.NextIndex = (index + 1) % count;
        }
        else
        {
            state.NextIndex = Math.Min(state.NextIndex + 1, count);
        }

        return asset;
    }

    private DialogueEventAsset SelectRandomEntry(DialogueEventCollection collection, IReadOnlyList<DialogueEventAsset> entries, CollectionState state)
    {
        int count = entries.Count;
        if (count == 0)
        {
            return null;
        }

        if (count == 1)
        {
            state.RemainingIndices.Clear();
            return entries[0];
        }

        DialogueEventAsset candidate = null;

        if (collection.ExhaustBeforeRepeat)
        {
            EnsureRandomPool(collection, state, count);

            if (state.RemainingIndices.Count == 0)
            {
                return null;
            }

            int attempts = 0;
            while (state.RemainingIndices.Count > 0)
            {
                int pick = UnityEngine.Random.Range(0, state.RemainingIndices.Count);
                int entryIndex = state.RemainingIndices[pick];
                candidate = entries[entryIndex];

                bool wouldRepeat = collection.AvoidImmediateRepeat && state.LastAsset != null && candidate == state.LastAsset && state.RemainingIndices.Count > 1;
                if (!wouldRepeat || attempts > state.RemainingIndices.Count)
                {
                    state.RemainingIndices.RemoveAt(pick);
                    break;
                }

                attempts++;
            }

            if (candidate == null && entries.Count > 0)
            {
                candidate = entries[UnityEngine.Random.Range(0, count)];
            }
        }
        else
        {
            int attempts = 0;
            do
            {
                candidate = entries[UnityEngine.Random.Range(0, count)];
                attempts++;
            }
            while (collection.AvoidImmediateRepeat && state.LastAsset != null && candidate == state.LastAsset && attempts < 6);
        }

        return candidate;
    }

    private void EnsureRandomPool(DialogueEventCollection collection, CollectionState state, int entryCount)
    {
        if (!collection.ExhaustBeforeRepeat)
        {
            return;
        }

        if (state.RemainingIndices.Count == 0)
        {
            state.RemainingIndices.Clear();
            for (int i = 0; i < entryCount; i++)
            {
                state.RemainingIndices.Add(i);
            }
        }
    }

    private CollectionState GetOrCreateState(DialogueEventCollection collection)
    {
        if (collection == null)
        {
            return null;
        }

        if (!collectionStates.TryGetValue(collection, out var state) || state == null)
        {
            state = new CollectionState();
            collectionStates[collection] = state;
        }

        return state;
    }

    public void SetActiveCollection(DialogueEventCollection collection, bool resetState = true)
    {
        activeCollection = collection;
        if (resetState)
        {
            ResetCollectionState(collection);
        }
    }

    public DialogueEventCollection GetActiveCollection() => activeCollection;

    public DialogueEventAsset GetLastDialogue() => lastPlayedAsset;

    public void ResetCollectionState(DialogueEventCollection collection)
    {
        if (collection == null)
        {
            return;
        }

        var state = GetOrCreateState(collection);
        state.NextIndex = 0;
        state.LastAsset = null;
        state.RemainingIndices.Clear();
        if (collection.ExhaustBeforeRepeat)
        {
            for (int i = 0; i < collection.DialogueEvents.Count; i++)
            {
                state.RemainingIndices.Add(i);
            }
        }
    }

    public void ResetAllCollectionStates()
    {
        foreach (var key in collectionStates.Keys.ToArray())
        {
            ResetCollectionState(key);
        }
    }

    private async UniTask<IDialogueService> ResolveServiceAsync()
    {
        if (cachedService != null)
        {
            return cachedService;
        }

        if (dialogueServiceBehaviour is IDialogueService directService)
        {
            cachedService = directService;
            return cachedService;
        }

        if (!resolveServiceFromLocator)
        {
            return null;
        }

        try
        {
            cachedService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }

        return cachedService;
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogging)
        {
            return;
        }

        Debug.Log($"[{nameof(DialogueEventPlayer)}] {message}", this);
    }

    private class CollectionState
    {
        public int NextIndex;
        public DialogueEventAsset LastAsset;
        public List<int> RemainingIndices = new();
    }

    private struct PlaybackRequest
    {
        public DialogueEventAsset Asset;
        public DialogueEventCollection SourceCollection;
        public bool IgnorePlayOnce;
    }

}
