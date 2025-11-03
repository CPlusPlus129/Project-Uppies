using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "storageRoomPuzzle1", menuName = "Game Flow/Story Events/Storage Room Puzzle 1")]
public sealed class StorageRoomPuzzle1StoryEventAsset : StoryEventAsset
{
    private const string LogPrefix = "[StorageRoomPuzzle1]";
    private static Vector3? cachedOriginalScale;

    [Header("Player Shrink Settings")]
    [SerializeField]
    [Tooltip("Target scale multiplier applied uniformly to the player root.")]
    private float shrinkScaleMultiplier = 0.25f;

    [SerializeField]
    [Tooltip("Duration of the shrink animation. Set to 0 for an instant resize.")]
    private float shrinkDurationSeconds = 0.35f;

    [SerializeField]
    [Tooltip("Animation curve for the shrink interpolation.")]
    private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Weapon Handling")]
    [SerializeField]
    [Tooltip("Disable the player's weapon during the puzzle.")]
    private bool disableWeapon = true;

    public static bool TryGetCachedOriginalScale(out Vector3 scale)
    {
        if (cachedOriginalScale.HasValue)
        {
            scale = cachedOriginalScale.Value;
            return true;
        }

        scale = default;
        return false;
    }

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var playerController = FindPlayerController();
        if (playerController == null)
        {
            var message = "PlayerController not found in the active scene";
            Debug.LogError($"{LogPrefix} {message}");
            return StoryEventResult.Failed(message);
        }

        var playerTransform = playerController.transform;
        if (!cachedOriginalScale.HasValue)
        {
            cachedOriginalScale = playerTransform.localScale;
        }

        await ResizePlayerAsync(playerTransform, cancellationToken);

        if (disableWeapon)
        {
            DisablePlayerWeapon();
        }

        return StoryEventResult.Completed("Player shrunk for storage room puzzle.");
    }

    private async UniTask ResizePlayerAsync(Transform playerTransform, CancellationToken cancellationToken)
    {
        var startScale = playerTransform.localScale;
        var targetScale = Vector3.Scale(startScale, Vector3.one * Mathf.Clamp(shrinkScaleMultiplier, 0.01f, 1f));

        var duration = Mathf.Max(0f, shrinkDurationSeconds);
        if (duration <= 0f)
        {
            playerTransform.localScale = targetScale;
            return;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / duration);
            var curvedTime = shrinkCurve != null ? shrinkCurve.Evaluate(normalizedTime) : normalizedTime;
            playerTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curvedTime);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        playerTransform.localScale = targetScale;
    }

    private void DisablePlayerWeapon()
    {
        try
        {
            PlayerStatSystem.Instance.CanUseWeapon.Value = false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} Failed to disable weapon: {ex.Message}");
        }
    }

    private static PlayerController FindPlayerController()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (controller != null)
        {
            return controller;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out PlayerController component))
        {
            return component;
        }

        return null;
    }
}
