using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "storageRoomPuzzle1", menuName = "Game Flow/Puzzle Events/Storage Room Puzzle 1")]
public sealed class StorageRoomPuzzle1StoryEventAsset : StoryEventAsset
{
    private const string LogPrefix = "[StorageRoomPuzzle1]";

    private enum EffectMode
    {
        Apply,
        Revert
    }

    [Header("Mode")]
    [SerializeField]
    [Tooltip("Choose whether this event applies the shrink effects or restores the player's defaults.")]
    private EffectMode effectMode = EffectMode.Apply;

    private static bool effectsApplied;
    private static readonly SemaphoreSlim effectSemaphore = new SemaphoreSlim(1, 1);
    private static Vector3? cachedOriginalScale;
    private static Vector3? cachedOriginalSpawnPosition;
    private static float? cachedOriginalSpeed;
    private static float? cachedOriginalSprintSpeed;
    private static float? cachedOriginalJumpHeight;
    private static float? cachedOriginalStepOffset;
    private static float? cachedOriginalInteractDistance;
    private static bool? cachedOriginalCanUseWeapon;
    private static bool weaponStateModifiedByEvent;

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

    [Header("Movement Speed")]
    [SerializeField]
    [Tooltip("When enabled, the player's base speed will be overridden instead of being scaled with the shrink multiplier.")]
    private bool useCustomSpeedOverride = false;

    [SerializeField]
    [Tooltip("Movement speed to apply when the custom override is enabled.")]
    private float customSpeedOverride = 2f;

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

        var playerMotor = playerController.GetComponent<PlayerMotor>() ?? playerController.GetComponentInChildren<PlayerMotor>();
        if (playerMotor == null)
        {
            var message = "PlayerMotor not found on PlayerController.";
            Debug.LogError($"{LogPrefix} {message}");
            return StoryEventResult.Failed(message);
        }

        var characterController = playerMotor.GetComponent<CharacterController>();
        if (characterController == null)
        {
            var message = "CharacterController not found on the player motor.";
            Debug.LogError($"{LogPrefix} {message}");
            return StoryEventResult.Failed(message);
        }

        var playerInteract = playerController.GetComponent<PlayerInteract>() ?? playerController.GetComponentInChildren<PlayerInteract>();
        if (playerInteract == null)
        {
            Debug.LogWarning($"{LogPrefix} PlayerInteract component not found; interaction distance will not be scaled.");
        }

        var playerTransform = playerController.transform;
        if (!cachedOriginalScale.HasValue)
        {
            cachedOriginalScale = playerTransform.localScale;
        }

        await effectSemaphore.WaitAsync(cancellationToken);
        try
        {
            switch (effectMode)
            {
                case EffectMode.Apply:
                    return await ApplyEffectsAsync(playerController, playerMotor, characterController, playerInteract, playerTransform, cancellationToken);

                case EffectMode.Revert:
                    return await RevertEffectsAsync(playerController, playerMotor, characterController, playerInteract, playerTransform, cancellationToken);

                default:
                    var message = $"{LogPrefix} Unsupported effect mode '{effectMode}'.";
                    Debug.LogError(message);
                    return StoryEventResult.Failed(message);
            }
        }
        finally
        {
            effectSemaphore.Release();
        }
    }

    private async UniTask<StoryEventResult> ApplyEffectsAsync(
        PlayerController playerController,
        PlayerMotor playerMotor,
        CharacterController characterController,
        PlayerInteract playerInteract,
        Transform playerTransform,
        CancellationToken cancellationToken)
    {
        if (effectsApplied)
        {
            Debug.Log($"{LogPrefix} Effects already applied; skipping duplicate request.");
            return StoryEventResult.Completed("Storage room puzzle shrink already applied.");
        }

        CaptureOriginalState(playerController, playerMotor, characterController, playerInteract, playerTransform);

        var targetScale = Vector3.Scale(playerTransform.localScale, Vector3.one * Mathf.Clamp(shrinkScaleMultiplier, 0.01f, 1f));

        playerController.SpawnPosition = playerTransform.position;
        await ResizePlayerAsync(playerTransform, targetScale, cancellationToken);

        ApplyMovementScaling(playerMotor, characterController, playerTransform, playerInteract);
        DisablePlayerWeaponIfNeeded();

        effectsApplied = true;
        return StoryEventResult.Completed("Player shrunk for storage room puzzle.");
    }

    private async UniTask<StoryEventResult> RevertEffectsAsync(
        PlayerController playerController,
        PlayerMotor playerMotor,
        CharacterController characterController,
        PlayerInteract playerInteract,
        Transform playerTransform,
        CancellationToken cancellationToken)
    {
        if (!effectsApplied)
        {
            Debug.Log($"{LogPrefix} Effects already reverted or were never applied; skipping.");
            return StoryEventResult.Completed("Storage room puzzle shrink already reverted.");
        }

        if (!cachedOriginalScale.HasValue)
        {
            var message = $"{LogPrefix} Cannot revert because the original scale was not cached.";
            Debug.LogWarning(message);
            return StoryEventResult.Failed(message);
        }

        playerController.SpawnPosition = cachedOriginalSpawnPosition.Value;
        await ResizePlayerAsync(playerTransform, cachedOriginalScale.Value, cancellationToken);

        RestoreMovementSettings(playerMotor, characterController, playerInteract);
        RestorePlayerWeaponState();

        effectsApplied = false;
        CaptureOriginalState(playerController, playerMotor, characterController, playerInteract, playerTransform);

        return StoryEventResult.Completed("Player restored to original size for storage room puzzle.");
    }

    private async UniTask ResizePlayerAsync(Transform playerTransform, Vector3 targetScale, CancellationToken cancellationToken)
    {
        var startScale = playerTransform.localScale;

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

    private void DisablePlayerWeaponIfNeeded()
    {
        if (!disableWeapon)
        {
            return;
        }

        try
        {
            var statSystem = PlayerStatSystem.Instance;
            if (statSystem == null || statSystem.CanUseWeapon == null)
            {
                return;
            }

            if (!cachedOriginalCanUseWeapon.HasValue)
            {
                cachedOriginalCanUseWeapon = statSystem.CanUseWeapon.Value;
            }

            statSystem.CanUseWeapon.Value = false;
            weaponStateModifiedByEvent = true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} Failed to disable weapon: {ex.Message}");
        }
    }

    private void RestorePlayerWeaponState()
    {
        if (!cachedOriginalCanUseWeapon.HasValue || !weaponStateModifiedByEvent)
        {
            return;
        }

        try
        {
            var statSystem = PlayerStatSystem.Instance;
            if (statSystem == null || statSystem.CanUseWeapon == null)
            {
                return;
            }

            statSystem.CanUseWeapon.Value = cachedOriginalCanUseWeapon.Value;
            weaponStateModifiedByEvent = false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} Failed to restore weapon state: {ex.Message}");
        }
    }

    private void CaptureOriginalState(PlayerController playerController, PlayerMotor playerMotor, CharacterController characterController, PlayerInteract playerInteract, Transform playerTransform)
    {
        cachedOriginalScale = playerTransform.localScale;
        cachedOriginalSpawnPosition = playerController.SpawnPosition;
        cachedOriginalSpeed = playerMotor.speed;
        cachedOriginalSprintSpeed = playerMotor.sprintSpeed;
        cachedOriginalJumpHeight = playerMotor.jumpHeight;
        cachedOriginalStepOffset = characterController.stepOffset;
        cachedOriginalInteractDistance = playerInteract != null ? playerInteract.interactDistance : null;

        try
        {
            var statSystem = PlayerStatSystem.Instance;
            if (statSystem != null && statSystem.CanUseWeapon != null)
            {
                cachedOriginalCanUseWeapon = statSystem.CanUseWeapon.Value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} Failed to cache weapon state: {ex.Message}");
        }
    }

    private void RestoreMovementSettings(PlayerMotor playerMotor, CharacterController characterController, PlayerInteract playerInteract)
    {
        if (cachedOriginalSpeed.HasValue)
        {
            playerMotor.speed = cachedOriginalSpeed.Value;
        }

        if (cachedOriginalSprintSpeed.HasValue)
        {
            playerMotor.sprintSpeed = cachedOriginalSprintSpeed.Value;
        }

        if (cachedOriginalJumpHeight.HasValue)
        {
            playerMotor.jumpHeight = cachedOriginalJumpHeight.Value;
        }

        if (cachedOriginalStepOffset.HasValue)
        {
            characterController.stepOffset = cachedOriginalStepOffset.Value;
        }

        if (playerInteract != null && cachedOriginalInteractDistance.HasValue)
        {
            playerInteract.interactDistance = Mathf.Max(cachedOriginalInteractDistance.Value, 0f);
        }
    }

    private void ApplyMovementScaling(PlayerMotor playerMotor, CharacterController characterController, Transform playerTransform, PlayerInteract playerInteract)
    {
        var originalSpeed = cachedOriginalSpeed ?? playerMotor.speed;
        var originalSprintSpeed = cachedOriginalSprintSpeed ?? playerMotor.sprintSpeed;
        var originalJumpHeight = cachedOriginalJumpHeight ?? playerMotor.jumpHeight;
        var originalStepOffset = cachedOriginalStepOffset ?? characterController.stepOffset;
        var originalInteractDistance = cachedOriginalInteractDistance ?? playerInteract?.interactDistance ?? 0f;

        var scaleRatio = Mathf.Max(GetScaleRatio(playerTransform), 0f);

        float targetBaseSpeed;

        if (useCustomSpeedOverride)
        {
            targetBaseSpeed = Mathf.Max(customSpeedOverride, 0f);
        }
        else
        {
            targetBaseSpeed = originalSpeed * scaleRatio;
        }

        var sprintToBaseRatio = Mathf.Approximately(originalSpeed, 0f) ? 1f : originalSprintSpeed / originalSpeed;
        var scaledSprintFromBase = targetBaseSpeed * sprintToBaseRatio;
        var scaledSprintFromScale = originalSprintSpeed * scaleRatio;
        var targetSprintSpeed = Mathf.Max(Mathf.Max(scaledSprintFromBase, scaledSprintFromScale), targetBaseSpeed);
        var targetJumpHeight = originalJumpHeight * scaleRatio;
        var targetStepOffset = Mathf.Clamp(originalStepOffset * scaleRatio, 0f, originalStepOffset);

        playerMotor.speed = targetBaseSpeed;
        playerMotor.sprintSpeed = targetSprintSpeed;
        playerMotor.jumpHeight = targetJumpHeight;
        characterController.stepOffset = targetStepOffset;

        if (playerInteract != null)
        {
            playerInteract.interactDistance = Mathf.Max(originalInteractDistance * scaleRatio, 0f);
        }
    }

    private float GetScaleRatio(Transform playerTransform)
    {
        if (!cachedOriginalScale.HasValue)
        {
            return 1f;
        }

        var originalMagnitude = cachedOriginalScale.Value.magnitude;
        if (Mathf.Approximately(originalMagnitude, 0f))
        {
            return 1f;
        }

        var currentMagnitude = playerTransform.localScale.magnitude;
        return Mathf.Clamp(currentMagnitude / originalMagnitude, 0f, 10f);
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
