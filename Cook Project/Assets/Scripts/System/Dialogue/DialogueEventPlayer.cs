using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine;
using UnityEngine.Events;

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

    private readonly Queue<DialogueEventAsset> queuedEvents = new();
    private readonly HashSet<DialogueEventAsset> playedAssets = new();

    private IDialogueService cachedService;
    private bool isPlaying;

    /// <summary>
    /// Plays the default event immediately if possible.
    /// </summary>
    public void Play()
    {
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
        if (dialogueEventAsset == null)
        {
            Debug.LogWarning($"[{nameof(DialogueEventPlayer)}] Missing DialogueEventAsset on {name}.");
            onDialogueDenied?.Invoke();
            return false;
        }

        if (dialogueEventAsset.PlayOnce && playedAssets.Contains(dialogueEventAsset))
        {
            onDialogueDenied?.Invoke();
            return false;
        }

        if (isPlaying)
        {
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
            onDialogueDenied?.Invoke();
            return;
        }

        if (isPlaying)
        {
            if (allowQueue)
            {
                queuedEvents.Enqueue(dialogueEventAsset);
            }
            else
            {
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

        try
        {
            await service.PlayDialogueAsync(dialogueEventAsset);
            playedAssets.Add(dialogueEventAsset);
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
}
