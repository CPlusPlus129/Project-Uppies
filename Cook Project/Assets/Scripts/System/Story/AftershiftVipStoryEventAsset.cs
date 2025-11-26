using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AftershiftVipStoryEvent", menuName = "Game Flow/VIP Events/Aftershift VIP")]
public sealed class AftershiftVipStoryEventAsset : StoryEventAsset
{
    [Header("VIP Setup")]
    [SerializeField]
    private GameObject vipCustomerPrefab;

    [SerializeField]
    private string vipDisplayName = "BabyBoss";

    [SerializeField]
    [Tooltip("Disable locomotion/AI components so the VIP remains stationary after spawning.")]
    private bool freezeVipOnSpawn = true;

    [SerializeField]
    [Tooltip("Disable all Animator components on the VIP so the model stays in its idle pose.")]
    private bool disableVipAnimatorOnSpawn = true;

    [SerializeField]
    [Tooltip("Override the VIP's world rotation after it snaps to the anchor.")]
    private bool overrideVipRotation;

    [SerializeField]
    private Vector3 vipRotationEuler;

    [SerializeField]
    [Tooltip("Override the VIP's local scale after it is parented to the anchor.")]
    private bool overrideVipScale;

    [SerializeField]
    private Vector3 vipLocalScale = Vector3.one;

    [Header("Spawn Anchors")]
    [SerializeField]
    [Tooltip("Anchor identifiers the VIP can use. Defaults to the three base customer slots.")]
    private List<string> preferredAnchorIds = new List<string> { "Customer1", "Customer2", "Customer3" };

    [SerializeField]
    [Tooltip("If none of the preferred anchors are found, allow any registered anchor to be used.")]
    private bool fallbackToAnyAnchor = true;

    [SerializeField]
    [Tooltip("Parent the spawned VIP under the anchor to inherit baked animations / movement.")]
    private bool parentVipToAnchor = true;

    [Header("Signals & Dialogue & Task")]
    [SerializeField]
    private string signalOnSpawn;

    [SerializeField]
    private string signalOnInteract;

    [SerializeField]
    private DialogueEventAsset spawnDialogue;

    [SerializeField]
    private DialogueEventAsset interactDialogue;

    [SerializeField]
    private string interactCompleteTaskId;

    [Header("HUD Broadcasts")]
    [SerializeField]
    [Tooltip("Optional HUD broadcast shown when the VIP spawns.")]
    private string spawnBroadcastMessage = "After-shift VIP arrived.";

    [SerializeField]
    [Tooltip("Optional HUD broadcast shown after the VIP interaction succeeds.")]
    private string interactBroadcastMessage = "After-shift VIP satisfied.";

    [SerializeField]
    [Tooltip("World broadcast duration in seconds.")]
    private float broadcastDurationSeconds = 4f;

    [Header("Interaction")]
    [SerializeField]
    [Tooltip("Require the player to interact before the story event completes.")]
    private bool waitForPlayerInteraction = true;

    [SerializeField]
    [Tooltip("Allow repeated interactions to trigger the UnityEvents.")]
    private bool allowMultipleInteractions = false;

    [SerializeField]
    private bool disableInteractableAfterUse = true;

    [SerializeField]
    [Tooltip("Disable the base Customer interact behaviour so only this story event handles interaction logic.")]
    private bool disableCustomerInteraction = true;

    [SerializeField]
    private UnityEvent onFirstInteract = new UnityEvent();

    [SerializeField]
    private UnityEvent onInteract = new UnityEvent();

    [Header("Fade Integration")]
    [SerializeField]
    private bool triggerFadeOnInteract = true;

    [SerializeField]
    private bool addFadeComponentIfMissing = true;

    [SerializeField]
    private bool waitForFadeCompletion = true;

    [SerializeField]
    private float fadeWaitTimeoutSeconds = 5f;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (vipCustomerPrefab == null)
        {
            return StoryEventResult.Failed("VIP customer prefab is not set.");
        }

        var dialogueService = context.IsServiceReady<IDialogueService>()
            ? await context.GetServiceAsync<IDialogueService>()
            : null;

        var anchor = ResolveAnchor();
        if (anchor == null)
        {
            Debug.LogError($"[{nameof(AftershiftVipStoryEventAsset)}] No VipCustomerSpawnAnchor found in the scene.");
            return StoryEventResult.Failed("Missing VIP spawn anchor.");
        }

        var vipInstance = SpawnVip(anchor);
        if (vipInstance == null)
        {
            return StoryEventResult.Failed("VIP instantiation failed.");
        }

        var vipRootObject = vipInstance.gameObject;

        if (!string.IsNullOrWhiteSpace(signalOnSpawn))
        {
            context.SendSignal(signalOnSpawn);
        }

        if (!string.IsNullOrWhiteSpace(spawnBroadcastMessage))
        {
            WorldBroadcastSystem.Instance?.Broadcast(spawnBroadcastMessage, broadcastDurationSeconds);
        }

        if (spawnDialogue != null && dialogueService != null)
        {
            await dialogueService.PlayDialogueAsync(spawnDialogue).AttachExternalCancellation(cancellationToken);
        }

        var interactable = PrepareInteractable(vipInstance);
        if (interactable == null)
        {
            Debug.LogError($"[{nameof(AftershiftVipStoryEventAsset)}] VIP prefab is missing a {nameof(UnityInteractable)}.");
            return StoryEventResult.Failed("VIP interactable missing.");
        }

        if (disableCustomerInteraction)
        {
            DisableCustomerInteraction(vipInstance, interactable);
        }

        var interactionTcs = waitForPlayerInteraction ? new UniTaskCompletionSource<bool>() : null;
        UniTaskCompletionSource<bool> fadeTcs = null;
        if (triggerFadeOnInteract && waitForFadeCompletion)
        {
            fadeTcs = new UniTaskCompletionSource<bool>();
        }

        bool hasInteracted = false;

        UnityAction handler = null;
        handler = () =>
        {
            if (!allowMultipleInteractions && hasInteracted)
            {
                return;
            }

            if (!hasInteracted)
            {
                hasInteracted = true;
                onFirstInteract?.Invoke();
            }

            onInteract?.Invoke();

            if (!string.IsNullOrWhiteSpace(signalOnInteract))
            {
                context.SendSignal(signalOnInteract);
            }

            if (!string.IsNullOrEmpty(interactCompleteTaskId))
            {
                TaskManager.Instance?.CompleteTask(interactCompleteTaskId);
            }

            if (!string.IsNullOrWhiteSpace(interactBroadcastMessage))
            {
                WorldBroadcastSystem.Instance?.Broadcast(interactBroadcastMessage, broadcastDurationSeconds);
            }

            if (interactDialogue != null && dialogueService != null)
            {
                dialogueService.PlayDialogueAsync(interactDialogue).Forget();
            }

            if (triggerFadeOnInteract)
            {
                TriggerFade(vipRootObject, fadeTcs, cancellationToken);
            }

            interactionTcs?.TrySetResult(true);

            if (!allowMultipleInteractions && disableInteractableAfterUse)
            {
                interactable.enabled = false;
            }
        };

        interactable.AddOnInteractListener(handler);

        if (waitForPlayerInteraction)
        {
            await interactionTcs.Task.AttachExternalCancellation(cancellationToken);
        }

        if (fadeTcs != null)
        {
            await fadeTcs.Task.AttachExternalCancellation(cancellationToken);
        }

        return StoryEventResult.Completed(hasInteracted
            ? "Aftershift VIP interaction completed."
            : "Aftershift VIP spawned (no interaction required).");
    }

    private Customer SpawnVip(VipCustomerSpawnAnchor anchor)
    {
        try
        {
            var pivot = anchor != null ? anchor.SpawnPoint : null;
            var position = pivot != null ? pivot.position : Vector3.zero;
            var rotation = pivot != null ? pivot.rotation : Quaternion.identity;
            var parent = parentVipToAnchor && pivot != null ? pivot : null;
            var instance = UnityEngine.Object.Instantiate(vipCustomerPrefab, position, rotation, parent);
            StabilizeVipPresentation(instance);
            anchor?.SnapTransform(instance.transform);
            ApplyVipTransformOverrides(instance.transform);
            ReapplyTransformsNextFrame(instance.transform).Forget();

            var customer = instance.GetComponent<Customer>() ?? instance.GetComponentInChildren<Customer>();
            if (customer == null)
            {
                Debug.LogError($"[{nameof(AftershiftVipStoryEventAsset)}] Spawned prefab '{instance.name}' is missing a Customer component.");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(vipDisplayName))
            {
                customer.customerName = vipDisplayName;
                if (customer.nameText != null)
                {
                    customer.nameText.text = vipDisplayName;
                }
            }

            PrepareInteractable(customer);
            customer.gameObject.name = $"AftershiftVIP_{customer.customerName}";
            return customer;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(AftershiftVipStoryEventAsset)}] Failed to spawn VIP: {ex}");
            return null;
        }
    }

    private void StabilizeVipPresentation(GameObject instance)
    {
        if (instance == null || (!freezeVipOnSpawn && !disableVipAnimatorOnSpawn))
        {
            return;
        }

        if (freezeVipOnSpawn)
        {
            foreach (var agent in instance.GetComponentsInChildren<NavMeshAgent>(true))
            {
                if (agent == null) continue;
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }
                agent.enabled = false;
            }

            foreach (var mob in instance.GetComponentsInChildren<Mob>(true))
            {
                if (mob == null) continue;
                mob.enabled = false;
            }

            foreach (var bossChase in instance.GetComponentsInChildren<BossChaseController>(true))
            {
                if (bossChase == null) continue;
                bossChase.enabled = false;
            }

            foreach (var movementAudio in instance.GetComponentsInChildren<MobMovementAudio>(true))
            {
                if (movementAudio == null) continue;
                movementAudio.enabled = false;
            }

            foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rigidbody == null) continue;
                if (!rigidbody.isKinematic)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            foreach (var audioSource in instance.GetComponentsInChildren<AudioSource>(true))
            {
                if (audioSource == null) continue;
                audioSource.Stop();
                audioSource.playOnAwake = false;
                audioSource.enabled = false;
            }
        }

        if (disableVipAnimatorOnSpawn)
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                if (animator == null) continue;
                animator.enabled = false;
            }
        }
    }

    private void ApplyVipTransformOverrides(Transform vipTransform)
    {
        if (vipTransform == null)
        {
            return;
        }

        if (overrideVipRotation)
        {
            var desiredRotation = Quaternion.Euler(vipRotationEuler);
            vipTransform.localRotation = desiredRotation;
        }

        if (overrideVipScale)
        {
            vipTransform.localScale = vipLocalScale;
        }
    }

    private async UniTaskVoid ReapplyTransformsNextFrame(Transform vipTransform)
    {
        if (vipTransform == null) return;

        // Force apply for 5 frames to fight any initialization logic (animators, navmesh, mob state machines)
        for (int i = 0; i < 5; i++)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (vipTransform == null) return;
            ApplyVipTransformOverrides(vipTransform);
        }
    }

    private UnityInteractable PrepareInteractable(Customer customer)
    {
        if (customer == null)
        {
            return null;
        }

        var interactable = customer.GetComponent<UnityInteractable>();
        if (interactable == null)
        {
            interactable = customer.gameObject.AddComponent<UnityInteractable>();
        }

        interactable.enabled = true;
        return interactable;
    }

    private void DisableCustomerInteraction(Customer customer, UnityInteractable interactable)
    {
        if (customer == null)
        {
            return;
        }

        if (interactable != null)
        {
            interactable.RemoveOnInteractListener(customer.Interact);
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(customer);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(customer);
        }
    }

    private void TriggerFade(GameObject target, UniTaskCompletionSource<bool> fadeTcs, CancellationToken token)
    {
        if (target == null)
        {
            fadeTcs?.TrySetResult(true);
            return;
        }

        var fade = target.GetComponent<SceneObjectFadeAway>();
        if (fade == null && addFadeComponentIfMissing)
        {
            fade = target.AddComponent<SceneObjectFadeAway>();
        }

        if (fade == null)
        {
            fadeTcs?.TrySetResult(true);
            return;
        }

        fade.FadeAway();

        if (fadeTcs != null)
        {
            WaitForFadeCompletionAsync(fade, fadeTcs, token).Forget();
        }
    }

    private async UniTaskVoid WaitForFadeCompletionAsync(SceneObjectFadeAway fade, UniTaskCompletionSource<bool> fadeTcs, CancellationToken token)
    {
        try
        {
            var timeout = Mathf.Max(0.1f, fadeWaitTimeoutSeconds);
            var deadline = Time.realtimeSinceStartup + timeout;
            while (fade != null && fade.gameObject != null && Time.realtimeSinceStartup < deadline)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            fadeTcs.TrySetResult(true);
        }
    }

    private VipCustomerSpawnAnchor ResolveAnchor()
    {
        var anchors = UnityEngine.Object.FindObjectsByType<VipCustomerSpawnAnchor>(FindObjectsSortMode.InstanceID);
        if (anchors == null || anchors.Length == 0)
        {
            return null;
        }

        var filtered = FilterAnchors(anchors, preferredAnchorIds);
        if (filtered.Count == 0 && fallbackToAnyAnchor)
        {
            filtered = anchors.ToList();
        }

        if (filtered.Count == 0)
        {
            return null;
        }

        var index = UnityEngine.Random.Range(0, filtered.Count);
        return filtered[index];
    }

    private static List<VipCustomerSpawnAnchor> FilterAnchors(IEnumerable<VipCustomerSpawnAnchor> anchors, List<string> filter)
    {
        if (anchors == null)
        {
            return new List<VipCustomerSpawnAnchor>();
        }

        if (filter == null || filter.Count == 0)
        {
            return anchors.ToList();
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var allowed = new HashSet<string>(filter.Where(id => !string.IsNullOrWhiteSpace(id)), comparer);
        if (allowed.Count == 0)
        {
            return anchors.ToList();
        }

        return anchors.Where(anchor => allowed.Contains(anchor.AnchorId)).ToList();
    }
}
