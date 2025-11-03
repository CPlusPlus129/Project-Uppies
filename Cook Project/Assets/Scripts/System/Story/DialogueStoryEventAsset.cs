using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueStoryEvent", menuName = "Game Flow/Story Events/Dialogue Event")]
public class DialogueStoryEventAsset : StoryEventAsset
{
    [Header("Dialogue")]
    [SerializeField]
    [Tooltip("Primary dialogue event to play.")]
    private DialogueEventAsset primaryDialogue;

    [SerializeField]
    [Tooltip("Optional additional dialogue events that will run after the primary one.")]
    private List<DialogueEventAsset> additionalDialogues = new List<DialogueEventAsset>();

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Skip dialogues marked PlayOnce that have already been triggered during this session.")]
    private bool respectDialoguePlayOnce = true;

    [SerializeField]
    [Tooltip("Display DialogueEventAsset.Hint via WorldBroadcastSystem before playing each dialogue.")]
    private bool broadcastHints = false;

    [SerializeField]
    [Tooltip("Duration passed to WorldBroadcastSystem when broadcasting hints.")]
    private float hintDurationSeconds = 3f;

    [SerializeField]
    [Tooltip("Log warnings when a dialogue asset is missing or skipped.")]
    private bool logWarnings = true;

    private static readonly HashSet<int> playedDialogueInstanceIds = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlayedDialogueCache()
    {
        playedDialogueInstanceIds.Clear();
    }

    public override async UniTask<bool> CanExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var baseResult = await base.CanExecuteAsync(context, cancellationToken);
        if (!baseResult)
        {
            return false;
        }

        var dialogues = EnumerateDialogues().Where(asset => asset != null).ToList();
        if (dialogues.Count == 0)
        {
            return false;
        }

        if (!respectDialoguePlayOnce)
        {
            return true;
        }

        return dialogues.Any(asset => !asset.PlayOnce || !playedDialogueInstanceIds.Contains(asset.GetInstanceID()));
    }

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var dialogues = EnumerateDialogues().Where(asset => asset != null).ToList();
        if (dialogues.Count == 0)
        {
            LogWarning("No dialogue assets configured for DialogueStoryEventAsset.");
            return StoryEventResult.Skipped("No dialogue assets configured.");
        }

        var dialogueService = await context.GetServiceAsync<IDialogueService>();
        if (dialogueService == null)
        {
            Debug.LogError("[DialogueStoryEvent] Dialogue service is not available via ServiceLocator.");
            return StoryEventResult.Failed("Dialogue service unavailable.");
        }

        var anyPlayed = false;
        DialogueEventAsset lastPlayed = null;

        foreach (var dialogue in dialogues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (respectDialoguePlayOnce && dialogue.PlayOnce && playedDialogueInstanceIds.Contains(dialogue.GetInstanceID()))
            {
                LogWarning($"Skipping dialogue '{dialogue.name}' because it is marked PlayOnce and already played.");
                continue;
            }

            if (broadcastHints && !string.IsNullOrWhiteSpace(dialogue.Hint))
            {
                WorldBroadcastSystem.Instance?.Broadcast(dialogue.Hint, hintDurationSeconds);
            }

            await dialogueService.PlayDialogueAsync(dialogue).AttachExternalCancellation(cancellationToken);

            anyPlayed = true;
            lastPlayed = dialogue;

            if (respectDialoguePlayOnce && dialogue.PlayOnce)
            {
                playedDialogueInstanceIds.Add(dialogue.GetInstanceID());
            }
        }

        if (!anyPlayed)
        {
            return StoryEventResult.Skipped("All dialogue assets were skipped (PlayOnce).");
        }

        var message = lastPlayed != null ? lastPlayed.Label : null;
        return StoryEventResult.Completed(message);
    }

    private IEnumerable<DialogueEventAsset> EnumerateDialogues()
    {
        if (primaryDialogue != null)
        {
            yield return primaryDialogue;
        }

        if (additionalDialogues != null)
        {
            foreach (var dialogue in additionalDialogues)
            {
                if (dialogue != null)
                {
                    yield return dialogue;
                }
            }
        }
    }

    private void LogWarning(string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[DialogueStoryEvent] {message}");
        }
    }
}
