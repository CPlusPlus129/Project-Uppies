using System;
using System.Collections.Generic;
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

    private readonly Queue<DialogueEventAsset> queuedEvents = new();
    private readonly HashSet<DialogueEventAsset> playedAssets = new();

    private IDialogueService cachedService;
    private bool isPlaying;

    /// <summary>
    /// Plays the default event immediately if possible.
    /// </summary>
    public void Play()
    {
        DebugLog($"Play() requested on {name} (default event: {defaultEvent?.name ?? "<null>"})");
        Play(defaultEvent).Forget();
    }

    /// <summary>
    /// Queues the default event if another dialogue is already running; otherwise plays immediately.
    /// </summary>
    public void PlayAndQueue()
    {
        PlayAndQueue(defaultEvent).Forget();
    }

    /// <summary>
    /// Attempts to play the default event if conditions allow it.
    /// </summary>
    public bool TryPlay()
    {
        return TryPlay(defaultEvent);
    }

    /// <summary>
    /// Plays the provided dialogue event immediately if possible.
    /// </summary>
    public UniTask Play(DialogueEventAsset dialogueEventAsset)
    {
        return PlayInternalAsync(dialogueEventAsset, false);
    }

    /// <summary>
    /// Queues the provided dialogue event if another dialogue is already running; otherwise plays immediately.
    /// </summary>
    public UniTask PlayAndQueue(DialogueEventAsset dialogueEventAsset)
    {
        return PlayInternalAsync(dialogueEventAsset, true);
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

        Play(dialogueEventAsset).Forget();
        return true;
    }

    private async UniTask PlayInternalAsync(DialogueEventAsset dialogueEventAsset, bool allowQueue)
    {
        if (dialogueEventAsset == null)
        {
            Debug.LogWarning($"[{nameof(DialogueEventPlayer)}] Missing DialogueEventAsset on {name}.");
            onDialogueDenied?.Invoke();
            return;
        }

        if (dialogueEventAsset.PlayOnce && playedAssets.Contains(dialogueEventAsset))
        {
            DebugLog($"PlayInternalAsync abort: {dialogueEventAsset.name} already recorded as played once.");
            onDialogueDenied?.Invoke();
            return;
        }

        if (isPlaying)
        {
            if (allowQueue)
            {
                DebugLog($"Queueing dialogue {dialogueEventAsset.name}.");
                queuedEvents.Enqueue(dialogueEventAsset);
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
        DebugLog($"Starting dialogue {dialogueEventAsset.name}.");

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
            DebugLog($"Processing queued dialogue {next.name}.");
            await PlayInternalAsync(next, false);
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
}
