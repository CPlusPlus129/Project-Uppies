using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "TeleportPlayerStoryEvent", menuName = "Game Flow/Utility/Teleport Player")]
public sealed class TeleportPlayerStoryEventAsset : StoryEventAsset
{
    [Header("Destination")]
    [SerializeField]
    [Tooltip("Optional transform to copy position (and rotation) from.")]
    private Transform destination;

    [SerializeField]
    [Tooltip("Fallback world position when no destination transform is assigned.")]
    private Vector3 fallbackPosition = Vector3.zero;

    [SerializeField]
    [Tooltip("Offset applied on top of the resolved destination position.")]
    private Vector3 positionOffset = Vector3.zero;

    [Header("Rotation")]
    [SerializeField]
    [Tooltip("Apply destination rotation (or manual override) after teleporting.")]
    private bool overrideRotation = false;

    [SerializeField]
    [Tooltip("Destination rotation when no transform is provided.")]
    private Vector3 fallbackRotationEuler = Vector3.zero;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var playerController = FindPlayerController();
        if (playerController == null)
        {
            return UniTask.FromResult(StoryEventResult.Failed("PlayerController not found."));
        }

        var targetPosition = ResolvePosition();
        var targetRotation = ResolveRotation();

        TeleportPlayer(playerController, targetPosition, targetRotation);

        var message = overrideRotation
            ? $"Player teleported to {targetPosition} with rotation override."
            : $"Player teleported to {targetPosition}.";
        return UniTask.FromResult(StoryEventResult.Completed(message));
    }

    private Vector3 ResolvePosition()
    {
        if (destination != null)
        {
            return destination.position + positionOffset;
        }

        return fallbackPosition + positionOffset;
    }

    private Quaternion? ResolveRotation()
    {
        if (!overrideRotation)
        {
            return null;
        }

        if (destination != null)
        {
            return destination.rotation;
        }

        return Quaternion.Euler(fallbackRotationEuler);
    }

    private static void TeleportPlayer(PlayerController playerController, Vector3 position, Quaternion? rotation)
    {
        var transform = playerController.transform;
        var characterController = playerController.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (rotation.HasValue)
        {
            transform.SetPositionAndRotation(position, rotation.Value);
        }
        else
        {
            transform.position = position;
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private static PlayerController FindPlayerController()
    {
        var controller = Object.FindFirstObjectByType<PlayerController>();
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
