using R3;
using UnityEngine;

/// <summary>
/// Overwhelming Kitchen victory object.
/// Enabled when target order count is reached, triggers fire effects and ends game upon player interaction.
/// </summary>
public class OverwhelmingKitchenVictory : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OverwhelmingKitchenSystem kitchenSystem;

    [Header("Settings")]
    [SerializeField] private string completionSignalId = "overwhelming_kitchen_complete";
    [SerializeField] private bool autoResetAfterCompletion = true;
    [SerializeField] private float resetDelaySeconds = 3f;
    [SerializeField] private Transform playerTeleportPosition;

    private CompositeDisposable disposables = new CompositeDisposable();
    private bool canInteract = false;

    private void Start()
    {
        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenVictory] Kitchen system reference is null!");
            return;
        }

        // Subscribe to game complete event
        kitchenSystem.OnGameComplete
            .Subscribe(_ => OnGameCompleted())
            .AddTo(disposables);

        // Initially disable interaction
        canInteract = false;
    }

    private void OnGameCompleted()
    {
        Debug.Log("[OverwhelmingKitchenVictory] Game completed! Enabling victory interaction");

        // Enable interaction
        canInteract = true;

        // Make sure the object is visible
        gameObject.SetActive(true);

        // Broadcast message
        WorldBroadcastSystem.Instance?.Broadcast("You've completed all orders! Interact with the object to escape!", 5f);
    }

    public void Interact()
    {
        if (!canInteract)
        {
            Debug.LogWarning("[OverwhelmingKitchenVictory] Cannot interact yet - game not completed");
            return;
        }

        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenVictory] Kitchen system reference is null!");
            return;
        }

        if (kitchenSystem.CurrentState.Value != OverwhelmingKitchenState.Completed)
        {
            Debug.LogWarning("[OverwhelmingKitchenVictory] Game is not in completed state");
            return;
        }

        Debug.Log("[OverwhelmingKitchenVictory] Victory object interacted! Triggering fire...");

        // Trigger fire effects
        kitchenSystem.TriggerFireAndEnd();

        // Teleport player to specified position
        if (playerTeleportPosition != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = playerTeleportPosition.position;
                player.transform.rotation = playerTeleportPosition.rotation;
                Debug.Log($"[OverwhelmingKitchenVictory] Teleported player to {playerTeleportPosition.position}");
            }
            else
            {
                Debug.LogWarning("[OverwhelmingKitchenVictory] Player object not found!");
            }
        }
        else
        {
            Debug.LogWarning("[OverwhelmingKitchenVictory] Player teleport position not set!");
        }

        // Broadcast dramatic message
        WorldBroadcastSystem.Instance?.Broadcast("FIRE!", 3f);

        // Send signal for StoryEvent
        if (!string.IsNullOrEmpty(completionSignalId))
        {
            GameFlow.Instance?.Signal(completionSignalId);
            Debug.Log($"[OverwhelmingKitchenVictory] Sent completion signal: {completionSignalId}");
        }

        // Disable interaction to prevent multiple triggers
        canInteract = false;

        // Auto reset if enabled
        if (autoResetAfterCompletion)
        {
            Invoke(nameof(ResetGameDelayed), resetDelaySeconds);
        }
    }

    private void ResetGameDelayed()
    {
        Debug.Log("[OverwhelmingKitchenVictory] Auto-resetting game...");
        kitchenSystem?.ResetGame();
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
    }

#if UNITY_EDITOR
    [ContextMenu("Test Trigger Victory")]
    private void TestTriggerVictory()
    {
        if (Application.isPlaying && kitchenSystem != null)
        {
            // Force complete the game for testing
            var completedCountField = typeof(OverwhelmingKitchenSystem).GetField("CompletedCount",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (completedCountField != null)
            {
                var completedCount = completedCountField.GetValue(kitchenSystem) as R3.ReactiveProperty<int>;
                if (completedCount != null)
                {
                    completedCount.Value = kitchenSystem.RequiredCompletions;
                }
            }

            OnGameCompleted();
        }
    }
#endif
}
