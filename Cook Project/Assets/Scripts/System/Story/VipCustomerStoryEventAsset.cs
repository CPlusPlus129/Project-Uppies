using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "VipCustomerStoryEvent", menuName = "Game Flow/VIP Events/VIP Customer")]
public sealed class VipCustomerStoryEventAsset : StoryEventAsset, IBackgroundStoryEvent
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    [Header("Execution Flow")]
    [SerializeField]
    [Tooltip("If true, this event runs in the background and doesn't block the main story queue.")]
    private bool runInBackground = true;

    [SerializeField]
    [Tooltip("If running in background, should we block subsequent events in THIS specific sequence?")]
    private bool blockSourceSequence = true;

    public bool RunInBackground => runInBackground;
    public bool BlockSourceSequence => blockSourceSequence;

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
    [Tooltip("Override the VIP's local rotation relative to the anchor.")]
    private bool overrideVipRotation;

    [SerializeField]
    private Vector3 vipRotationEuler;

    [SerializeField]
    [Tooltip("Override the VIP's local scale after it is parented to the anchor.")]
    private bool overrideVipScale;

    [SerializeField]
    private Vector3 vipLocalScale = Vector3.one;

    [SerializeField]
    [Tooltip("Optional meal name to force as the VIP's next order.")]
    private string forcedMealName;

    [SerializeField]
    [Tooltip("If disabled, the VIP spawns as a passive prop and never places an order.")]
    private bool vipHasOrder = true;

    [SerializeField]
    [Tooltip("Destroy the spawned VIP once their order is fulfilled or the event fails.")]
    private bool destroyVipOnExit = true;

    [Header("Shift Scheduling")]
    [SerializeField]
    [Tooltip("Only run this event when the active shift index matches the configured target.")]
    private bool requireSpecificShift = true;

    [SerializeField]
    [Tooltip("Shift index that should receive this VIP (0-based). Ignored when Require Specific Shift is false.")]
    private int targetShiftIndex = 0;

    [SerializeField]
    [Tooltip("Randomly pick a spawn hour within this window (uses in-game clock hours).")]
    private Vector2 spawnHourWindow = new Vector2(10f, 14f);

    [SerializeField]
    [Tooltip("Clamp the spawn window to the configured work day start/end hours.")]
    private bool clampSpawnWindowToWorkday = true;

    [SerializeField]
    [Tooltip("Spawn immediately if the window has already elapsed once the event runs.")]
    private bool spawnIfWindowAlreadyPassed = true;

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

    [Header("Dialogue Hooks")]
    [SerializeField]
    private DialogueEventAsset spawnDialogue;

    [SerializeField]
    private DialogueEventAsset orderTakenDialogue;

    [SerializeField]
    private DialogueEventAsset orderCompletedDialogue;

    [Header("Signals & Broadcasts")]
    [SerializeField]
    private string signalOnSpawn;

    [SerializeField]
    private string signalOnOrderTaken;

    [SerializeField]
    private string signalOnOrderCompleted;

    [SerializeField]
    [Tooltip("Optional signal that immediately destroys the spawned VIP when received.")]
    private string destroyVipSignalId;

    [SerializeField]
    [Tooltip("Optional HUD broadcast shown when the VIP spawns.")]
    private string spawnBroadcastMessage = "VIP arrival detected.";

    [SerializeField]
    [Tooltip("Optional HUD broadcast shown when the VIP order completes.")]
    private string completionBroadcastMessage = "VIP satisfied. Nice work.";

    [SerializeField]
    [Tooltip("World broadcast duration in seconds.")]
    private float broadcastDurationSeconds = 5f;

    [Header("Task Hooks")]
    [SerializeField]
    [Tooltip("Task id to mark complete once the VIP's order is served.")]
    private string taskIdToComplete;

    [SerializeField]
    [Tooltip("Task description to add when the VIP spawns.")]
    private string taskDescription = "Serve the VIP customer";

    [SerializeField]
    [Tooltip("Automatically add the task when the VIP spawns.")]
    private bool autoAddTaskOnSpawn = true;

    public override async UniTask<bool> CanExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (vipCustomerPrefab == null)
        {
            Debug.LogError($"[{nameof(VipCustomerStoryEventAsset)}] No VIP prefab assigned on {name}.");
            return false;
        }

        return await base.CanExecuteAsync(context, cancellationToken);
    }

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (vipCustomerPrefab == null)
        {
            return StoryEventResult.Failed("VIP customer prefab is not set.");
        }

        var shiftSystem = await context.GetServiceAsync<IShiftSystem>();
        var orderManager = await context.GetServiceAsync<IOrderManager>();
        var dialogueService = context.IsServiceReady<IDialogueService>() ? await context.GetServiceAsync<IDialogueService>() : null;

        var isShiftValid = await EnsureTargetShiftAsync(shiftSystem, cancellationToken);
        if (!isShiftValid)
        {
            Debug.LogWarning($"[{nameof(VipCustomerStoryEventAsset)}] Skipping VIP spawn because shift {targetShiftIndex} is not active.");
            return StoryEventResult.Skipped("Shift mismatch for VIP spawn.");
        }

        await WaitForShiftActiveAsync(shiftSystem, cancellationToken);

        var spawnHour = ResolveSpawnHour();
        var spawnWindowHit = await WaitForSpawnWindowAsync(shiftSystem, spawnHour, cancellationToken);
        if (!spawnWindowHit)
        {
            return StoryEventResult.Skipped("Shift ended before VIP spawn window elapsed.");
        }

        var anchor = ResolveAnchor();
        if (anchor == null)
        {
            Debug.LogError($"[{nameof(VipCustomerStoryEventAsset)}] No VipCustomerSpawnAnchor found in the scene.");
            return StoryEventResult.Failed("Missing VIP spawn anchor.");
        }

        var vipInstance = SpawnVip(anchor);
        if (vipInstance == null)
        {
            return StoryEventResult.Failed("VIP instantiation failed.");
        }

        ConfigureVipOrder(vipInstance);
        var vipName = vipInstance.customerName;

        ListenForDestroySignal(context, vipInstance, cancellationToken);

        if (vipHasOrder && autoAddTaskOnSpawn && !string.IsNullOrWhiteSpace(taskIdToComplete))
        {
            TaskManager.Instance.AddTask(taskIdToComplete, taskDescription);
        }

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

        StoryEventResult result;
        if (vipHasOrder)
        {
            result = await WaitForVipOrderAsync(
                context,
                vipName,
                orderManager,
                shiftSystem,
                dialogueService,
                cancellationToken);
        }
        else
        {
            result = StoryEventResult.Completed($"VIP '{vipName}' spawned without placing an order.");
        }

        if (destroyVipOnExit && vipInstance != null)
        {
            UnityEngine.Object.Destroy(vipInstance.gameObject);
        }

        return result;
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
            // Force apply again next frame to override any Start/OnEnable resets (e.g. NavMeshAgent, BossChaseController)
            ReapplyTransformsNextFrame(instance.transform).Forget();

            var customer = instance.GetComponent<Customer>() ?? instance.GetComponentInChildren<Customer>();
            if (customer == null)
            {
                Debug.LogError($"[{nameof(VipCustomerStoryEventAsset)}] Spawned prefab '{instance.name}' is missing a Customer component.");
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

            EnsureVipIsInteractable(customer, vipHasOrder);
            customer.gameObject.name = $"VIP_{customer.customerName}";
            return customer;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(VipCustomerStoryEventAsset)}] Failed to spawn VIP: {ex}");
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
                Debug.Log($"[VIP Debug] Disabled BossChaseController on {bossChase.name}");
            }

            foreach (var movementAudio in instance.GetComponentsInChildren<MobMovementAudio>(true))
            {
                if (movementAudio == null) continue;
                movementAudio.enabled = false;
            }

            foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rigidbody == null) continue;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
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
                // Also disable root motion explicitly to prevent override
                animator.applyRootMotion = false;
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
            Debug.Log($"[VIP Override] Applied Local Rotation: {vipRotationEuler} -> {vipTransform.localRotation.eulerAngles}");
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

    private void ConfigureVipOrder(Customer vip)
    {
        if (vip == null)
        {
            return;
        }

        if (!vipHasOrder)
        {
            vip.specifiedNextOrderName = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(forcedMealName))
        {
            vip.specifiedNextOrderName = forcedMealName;
        }
    }

    private void ListenForDestroySignal(GameFlowContext context, Customer vip, CancellationToken token)
    {
        if (vip == null || string.IsNullOrWhiteSpace(destroyVipSignalId))
        {
            return;
        }

        WaitForDestroySignalAsync(context, vip, token).Forget();
    }

    private async UniTaskVoid WaitForDestroySignalAsync(GameFlowContext context, Customer vip, CancellationToken token)
    {
        try
        {
            await context.WaitForSignalAsync(destroyVipSignalId, token);
            if (vip != null)
            {
                UnityEngine.Object.Destroy(vip.gameObject);
            }
        }
        catch (OperationCanceledException)
        {
            // Story flow cancelled before the destroy signal fired.
        }
    }

    private static void EnsureVipIsInteractable(Customer customer, bool enableInteraction)
    {
        if (customer == null)
        {
            return;
        }

        var interactable = customer.GetComponent<UnityInteractable>();
        if (!enableInteraction)
        {
            if (interactable != null)
            {
                interactable.enabled = false;
            }
            return;
        }

        if (interactable == null)
        {
            interactable = customer.gameObject.AddComponent<UnityInteractable>();
        }

        if (interactable == null)
        {
            Debug.LogWarning($"[{nameof(VipCustomerStoryEventAsset)}] Failed to attach UnityInteractable to VIP {customer.customerName}.", customer);
            return;
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

    private float ResolveSpawnHour()
    {
        var windowStart = spawnHourWindow.x;
        var windowEnd = spawnHourWindow.y;
        if (windowStart > windowEnd)
        {
            (windowStart, windowEnd) = (windowEnd, windowStart);
        }

        if (clampSpawnWindowToWorkday && Database.Instance?.shiftData != null)
        {
            var shiftData = Database.Instance.shiftData;
            var minHour = Mathf.Min(shiftData.workDayStartHour, shiftData.workDayEndHour);
            var maxHour = Mathf.Max(shiftData.workDayStartHour, shiftData.workDayEndHour);
            windowStart = Mathf.Clamp(windowStart, minHour, maxHour);
            windowEnd = Mathf.Clamp(windowEnd, minHour, maxHour);
        }

        if (Mathf.Approximately(windowStart, windowEnd))
        {
            return windowStart;
        }

        return UnityEngine.Random.Range(windowStart, windowEnd);
    }

    private async UniTask<bool> EnsureTargetShiftAsync(IShiftSystem shiftSystem, CancellationToken token)
    {
        if (!requireSpecificShift || targetShiftIndex < 0)
        {
            return true;
        }

        if (shiftSystem.shiftNumber.Value == targetShiftIndex)
        {
            return true;
        }

        var tcs = new UniTaskCompletionSource<bool>();
        IDisposable subscription = null;
        subscription = shiftSystem.shiftNumber.Subscribe(value =>
        {
            if (value == targetShiftIndex)
            {
                tcs.TrySetResult(true);
            }
            else if (value > targetShiftIndex)
            {
                tcs.TrySetResult(false);
            }
        });

        try
        {
            return await tcs.Task.AttachExternalCancellation(token);
        }
        finally
        {
            subscription?.Dispose();
        }
    }

    private static async UniTask WaitForShiftActiveAsync(IShiftSystem shiftSystem, CancellationToken token)
    {
        if (IsShiftActive(shiftSystem.currentState.Value))
        {
            return;
        }

        var tcs = new UniTaskCompletionSource<bool>();
        IDisposable subscription = null;
        subscription = shiftSystem.currentState.Subscribe(state =>
        {
            if (IsShiftActive(state))
            {
                tcs.TrySetResult(true);
            }
        });

        try
        {
            await tcs.Task.AttachExternalCancellation(token);
        }
        finally
        {
            subscription?.Dispose();
        }
    }

    private async UniTask<bool> WaitForSpawnWindowAsync(IShiftSystem shiftSystem, float spawnHour, CancellationToken token)
    {
        if (spawnIfWindowAlreadyPassed && shiftSystem.currentClockHour.Value >= spawnHour)
        {
            return true;
        }

        var reachedHourTcs = new UniTaskCompletionSource<bool>();
        var shiftEndedTcs = new UniTaskCompletionSource<bool>();

        IDisposable clockSubscription = null;
        clockSubscription = shiftSystem.currentClockHour.Subscribe(hour =>
        {
            if (hour >= spawnHour)
            {
                reachedHourTcs.TrySetResult(true);
            }
        });

        IDisposable stateSubscription = null;
        stateSubscription = shiftSystem.currentState.Subscribe(state =>
        {
            if (!IsShiftActive(state))
            {
                shiftEndedTcs.TrySetResult(true);
            }
        });

        try
        {
            var completed = await UniTask.WhenAny(reachedHourTcs.Task, shiftEndedTcs.Task).AttachExternalCancellation(token);
            return completed.winArgumentIndex == 0;
        }
        finally
        {
            clockSubscription?.Dispose();
            stateSubscription?.Dispose();
        }
    }

    private async UniTask<StoryEventResult> WaitForVipOrderAsync(
        GameFlowContext context,
        string vipName,
        IOrderManager orderManager,
        IShiftSystem shiftSystem,
        IDialogueService dialogueService,
        CancellationToken token)
    {
        var vipServedTcs = new UniTaskCompletionSource<bool>();
        var shiftEndedTcs = new UniTaskCompletionSource<bool>();
        var disposables = new CompositeDisposable();

        orderManager.OnNewOrder
            .Subscribe(order =>
            {
                if (IsVipOrder(order, vipName))
                {
                    if (!string.IsNullOrWhiteSpace(signalOnOrderTaken))
                    {
                        context.SendSignal(signalOnOrderTaken);
                    }

                    if (orderTakenDialogue != null && dialogueService != null)
                    {
                        dialogueService.PlayDialogueAsync(orderTakenDialogue).Forget();
                    }
                }
            })
            .AddTo(disposables);

        orderManager.OnOrderServed
            .Subscribe(order =>
            {
                if (IsVipOrder(order, vipName))
                {
                    vipServedTcs.TrySetResult(true);
                }
            })
            .AddTo(disposables);

        shiftSystem.currentState
            .Subscribe(state =>
            {
                if (!IsShiftActive(state))
                {
                    shiftEndedTcs.TrySetResult(true);
                }
            })
            .AddTo(disposables);

        try
        {
            var completed = await UniTask.WhenAny(vipServedTcs.Task, shiftEndedTcs.Task).AttachExternalCancellation(token);
            var vipServed = completed.winArgumentIndex == 0;

            if (vipServed)
            {
                if (!string.IsNullOrWhiteSpace(signalOnOrderCompleted))
                {
                    context.SendSignal(signalOnOrderCompleted);
                }

                if (!string.IsNullOrWhiteSpace(completionBroadcastMessage))
                {
                    WorldBroadcastSystem.Instance?.Broadcast(completionBroadcastMessage, broadcastDurationSeconds);
                }

                if (orderCompletedDialogue != null && dialogueService != null)
                {
                    dialogueService.PlayDialogueAsync(orderCompletedDialogue).Forget();
                }

                if (!string.IsNullOrWhiteSpace(taskIdToComplete))
                {
                    TaskManager.Instance.CompleteTask(taskIdToComplete);
                }

                return StoryEventResult.Completed($"VIP order '{vipName}' served.");
            }

            return StoryEventResult.Failed($"Shift ended before VIP '{vipName}' was satisfied.");
        }
        finally
        {
            disposables.Dispose();
        }
    }

    private static bool IsShiftActive(ShiftSystem.ShiftState state)
    {
        return state == ShiftSystem.ShiftState.InShift || state == ShiftSystem.ShiftState.Overtime;
    }

    private static bool IsVipOrder(Order order, string vipName)
    {
        if (order == null || string.IsNullOrWhiteSpace(order.CustomerName) || string.IsNullOrWhiteSpace(vipName))
        {
            return false;
        }

        return NameComparer.Equals(order.CustomerName, vipName);
    }
}
