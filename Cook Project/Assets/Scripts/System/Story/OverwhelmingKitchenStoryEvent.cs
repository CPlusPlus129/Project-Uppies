using Cysharp.Threading.Tasks;
using DialogueModule;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// StoryEvent for the Overwhelming Kitchen minigame.
/// Starts the minigame, optional intro/outro dialogues, and waits for game completion.
/// </summary>
[CreateAssetMenu(fileName = "OverwhelmingKitchenStoryEvent", menuName = "Game Flow/Story Events/Overwhelming Kitchen")]
public class OverwhelmingKitchenStoryEvent : StoryEventAsset
{
    [Header("Game Reference")]
    [SerializeField]
    [Tooltip("Reference to the OverwhelmingKitchenSystem in the scene. Will be found automatically if not set.")]
    private OverwhelmingKitchenSystem kitchenSystemOverride;

    [Header("Dialogue Integration")]
    [SerializeField]
    [Tooltip("Optional dialogue to play before starting the game")]
    private DialogueEventAsset introDialogue;

    [SerializeField]
    [Tooltip("Optional dialogue to play after completing the game")]
    private DialogueEventAsset outroDialogue;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("Signal ID to wait for game completion. Should match OverwhelmingKitchenVictory's completionSignalId")]
    private string gameCompleteSignalId = "overwhelming_kitchen_complete";

    [SerializeField]
    [Tooltip("If true, waits for the completion signal. If false, completes immediately after starting.")]
    private bool waitForCompletion = true;

    [SerializeField]
    [Tooltip("If true, automatically starts the game when this event executes.")]
    private bool autoStartGame = true;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        Debug.Log($"[OverwhelmingKitchenStoryEvent] Starting overwhelming kitchen event: {EventId}");

        // Find kitchen system if not set
        var kitchenSystem = kitchenSystemOverride;
        if (kitchenSystem == null)
        {
            kitchenSystem = UnityEngine.Object.FindFirstObjectByType<OverwhelmingKitchenSystem>();
            if (kitchenSystem == null)
            {
                Debug.LogError("[OverwhelmingKitchenStoryEvent] OverwhelmingKitchenSystem not found in scene!");
                return StoryEventResult.Failed("OverwhelmingKitchenSystem not found in scene");
            }
        }

        // Play intro dialogue if set
        if (introDialogue != null)
        {
            Debug.Log("[OverwhelmingKitchenStoryEvent] Playing intro dialogue...");
            var dialogueService = await context.GetServiceAsync<IDialogueService>();
            if (dialogueService != null)
            {
                await dialogueService.StartDialogueAsync(introDialogue.Label);
            }
        }

        // Start the game
        if (autoStartGame)
        {
            Debug.Log("[OverwhelmingKitchenStoryEvent] Starting kitchen game...");
            UIRoot.Instance.GetUIComponent<HUD>()?.Close();
            UIRoot.Instance.GetUIComponent<OverwhelmingKitchenUI>()?.Open();
            kitchenSystem.StartGame();
        }

        // Wait for completion if enabled
        if (waitForCompletion && !string.IsNullOrEmpty(gameCompleteSignalId))
        {
            Debug.Log($"[OverwhelmingKitchenStoryEvent] Waiting for completion signal: {gameCompleteSignalId}");

            try
            {
                await context.WaitForSignalAsync(gameCompleteSignalId, cancellationToken);
                Debug.Log("[OverwhelmingKitchenStoryEvent] Completion signal received!");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[OverwhelmingKitchenStoryEvent] Wait for completion was cancelled");
                return StoryEventResult.Cancelled("Event was cancelled while waiting for game completion");
            }
        }

        // Play outro dialogue if set
        if (outroDialogue != null)
        {
            Debug.Log("[OverwhelmingKitchenStoryEvent] Playing outro dialogue...");
            var dialogueService = await context.GetServiceAsync<IDialogueService>();
            if (dialogueService != null)
            {
                await dialogueService.StartDialogueAsync(outroDialogue.Label);
            }
        }

        UIRoot.Instance.GetUIComponent<HUD>()?.Open();
        UIRoot.Instance.GetUIComponent<OverwhelmingKitchenUI>()?.Close();

        Debug.Log("[OverwhelmingKitchenStoryEvent] Overwhelming kitchen event completed!");
        return StoryEventResult.Completed("Overwhelming kitchen game completed successfully");
    }

    public override async UniTask<bool> CanExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        // Always can execute unless the game is already running
        var kitchenSystem = kitchenSystemOverride;
        if (kitchenSystem == null)
        {
            kitchenSystem = UnityEngine.Object.FindFirstObjectByType<OverwhelmingKitchenSystem>();
        }

        if (kitchenSystem != null && kitchenSystem.CurrentState.Value == OverwhelmingKitchenState.Running)
        {
            Debug.LogWarning("[OverwhelmingKitchenStoryEvent] Game is already running!");
            return false;
        }

        return await base.CanExecuteAsync(context, cancellationToken);
    }

#if UNITY_EDITOR
    [ContextMenu("Find Kitchen System in Scene")]
    private void FindKitchenSystem()
    {
        if (Application.isPlaying)
        {
            var system = UnityEngine.Object.FindFirstObjectByType<OverwhelmingKitchenSystem>();
            if (system != null)
            {
                Debug.Log($"Found OverwhelmingKitchenSystem: {system.name}");
                kitchenSystemOverride = system;
            }
            else
            {
                Debug.LogWarning("OverwhelmingKitchenSystem not found in scene!");
            }
        }
        else
        {
            Debug.LogWarning("This can only be run in play mode!");
        }
    }
#endif
}
