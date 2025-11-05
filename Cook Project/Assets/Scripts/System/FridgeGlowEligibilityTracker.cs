using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

/// <summary>
/// Tracks which fridges are currently eligible to glow according to the FridgeGlowManager
/// and exposes helper methods to toggle glow on all eligible fridges simultaneously.
/// </summary>
public class FridgeGlowEligibilityTracker : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Player Guidance")]
    [Tooltip("Optional reference to PlayerFridgeGuidance used when glow triggering should also spin up the path effect.")]
    [SerializeField] private PlayerFridgeGuidance fridgeGuidance;

    private readonly List<FoodSource> eligibleFridgeList = new List<FoodSource>();
    private readonly HashSet<FoodSource> eligibleFridgeSet = new HashSet<FoodSource>();
    private readonly List<FoodSource> previouslyEligibleFridges = new List<FoodSource>();
    private readonly HashSet<FoodSource> previouslyEligibleSet = new HashSet<FoodSource>();
    private readonly HashSet<string> inventoryItems = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

    private IFridgeGlowManager fridgeGlowManager;
    private IInventorySystem inventorySystem;

    private enum GlowCommand
    {
        None,
        Enable,
        Disable,
        EnableForDuration
    }

    private GlowCommand lastGlowCommand = GlowCommand.None;
    private float lastGlowDurationSeconds;
    private bool lastGlowTriggeredGuidance;
    private bool hasLoggedMissingGuidance;

    public IReadOnlyList<FoodSource> EligibleFridges => eligibleFridgeList;

    private async void Awake()
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await WaitForServicesAsync(destroyToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (destroyToken.IsCancellationRequested)
        {
            return;
        }

        await BindManagerAsync();
    }

    private async UniTask BindManagerAsync()
    {
        fridgeGlowManager = await ServiceLocator.Instance.GetAsync<IFridgeGlowManager>();
        inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();

        if (fridgeGlowManager == null)
        {
            Debug.LogError("FridgeGlowEligibilityTracker: Failed to resolve IFridgeGlowManager");
            return;
        }

        if (inventorySystem == null)
        {
            Debug.LogError("FridgeGlowEligibilityTracker: Failed to resolve IInventorySystem");
        }
        else
        {
            InitializeInventoryTracking();
        }

        fridgeGlowManager.EligibleFridgesChanged += HandleEligibleFridgesChanged;
        SyncEligibleFridges(fridgeGlowManager.GetEligibleFridgesSnapshot());

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: Listening for eligible fridge updates. Initial count: {eligibleFridgeList.Count}");
        }
    }

    private async UniTask WaitForServicesAsync(CancellationToken cancellationToken)
    {
        await UniTask.WhenAll(
            UniTask.WaitUntil(() => ServiceLocator.Instance.IsServiceReady<IFridgeGlowManager>(), cancellationToken: cancellationToken),
            UniTask.WaitUntil(() => ServiceLocator.Instance.IsServiceReady<IInventorySystem>(), cancellationToken: cancellationToken)
        );
    }

    private void OnDestroy()
    {
        if (fridgeGlowManager != null)
        {
            fridgeGlowManager.EligibleFridgesChanged -= HandleEligibleFridgesChanged;
        }
    }

    private void HandleEligibleFridgesChanged(IReadOnlyCollection<FoodSource> fridges)
    {
        SyncEligibleFridges(fridges);

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: Eligible fridge count updated to {eligibleFridgeList.Count} [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }
    }

    private void SyncEligibleFridges(IReadOnlyCollection<FoodSource> fridges)
    {
        if (fridges == null)
        {
            return;
        }

        previouslyEligibleFridges.Clear();
        previouslyEligibleFridges.AddRange(eligibleFridgeList);
        previouslyEligibleSet.Clear();
        foreach (var fridge in previouslyEligibleFridges)
        {
            if (fridge != null)
            {
                previouslyEligibleSet.Add(fridge);
            }
        }

        eligibleFridgeSet.Clear();
        eligibleFridgeList.Clear();

        foreach (var fridge in fridges)
        {
            if (fridge == null)
            {
                continue;
            }

            if (ShouldSuppress(fridge))
            {
                continue;
            }

            if (eligibleFridgeSet.Add(fridge))
            {
                eligibleFridgeList.Add(fridge);
            }
        }

        ApplyLastGlowCommand();

        foreach (var fridge in previouslyEligibleFridges)
        {
            if (fridge == null)
            {
                continue;
            }

            if (!eligibleFridgeSet.Contains(fridge))
            {
                fridge.DisableGlow();
            }
        }
    }

    public void EnableGlowOnEligible()
    {
        EnableGlowOnEligible(false);
    }

    public void EnableGlowOnEligible(bool activateGuidance)
    {
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: EnableGlowOnEligible({activateGuidance}) → {eligibleFridgeList.Count} fridges [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        lastGlowCommand = GlowCommand.Enable;
        lastGlowDurationSeconds = 0f;
        lastGlowTriggeredGuidance = activateGuidance;

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.EnableGlow();
        }

        if (activateGuidance)
        {
            TriggerGuidance(false, 0f);
        }
    }

    public void DisableGlowOnEligible()
    {
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: DisableGlowOnEligible [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        lastGlowCommand = GlowCommand.Disable;
        lastGlowDurationSeconds = 0f;
        lastGlowTriggeredGuidance = false;

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.DisableGlow();
        }
    }

    public void EnableGlowOnEligibleForDuration(float seconds)
    {
        EnableGlowOnEligibleForDuration(seconds, false);
    }

    public void EnableGlowOnEligibleForDuration(float seconds, bool activateGuidance)
    {
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: EnableGlowOnEligibleForDuration({seconds}, {activateGuidance}) → {eligibleFridgeList.Count} fridges [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        lastGlowCommand = GlowCommand.EnableForDuration;
        lastGlowDurationSeconds = Mathf.Max(0f, seconds);
        lastGlowTriggeredGuidance = activateGuidance;

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.EnableGlowForDuration(seconds);
        }

        if (activateGuidance)
        {
            if (lastGlowDurationSeconds > 0f)
            {
                TriggerGuidance(true, lastGlowDurationSeconds);
            }
            else
            {
                TriggerGuidance(false, 0f);
            }
        }
    }

    private void RequestLatestSnapshot()
    {
        if (fridgeGlowManager == null)
        {
            return;
        }

        var snapshot = fridgeGlowManager.GetEligibleFridgesSnapshot();
        SyncEligibleFridges(snapshot);
    }

    private void InitializeInventoryTracking()
    {
        UpdateInventorySnapshot(inventorySystem.GetAllItems());

        inventorySystem.OnInventoryChanged
            .Subscribe(items =>
            {
                if (UpdateInventorySnapshot(items))
                {
                    RequestLatestSnapshot();
                }
            })
            .AddTo(this);
    }

    private bool UpdateInventorySnapshot(IReadOnlyList<ItemBase> items)
    {
        if (items == null)
        {
            return false;
        }

        var newItems = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
            {
                continue;
            }

            newItems.Add(item.ItemName);
        }

        if (inventoryItems.SetEquals(newItems))
        {
            return false;
        }

        inventoryItems.Clear();

        foreach (var itemName in newItems)
        {
            inventoryItems.Add(itemName);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: Inventory snapshot updated ({inventoryItems.Count} items)");
        }

        return true;
    }

    private bool ShouldSuppress(FoodSource fridge)
    {
        if (fridge == null || string.IsNullOrWhiteSpace(fridge.ItemName))
        {
            return false;
        }

        return inventoryItems.Contains(fridge.ItemName);
    }

    private void ApplyLastGlowCommand()
    {
        if (lastGlowCommand == GlowCommand.None || eligibleFridgeList.Count == 0)
        {
            return;
        }

        bool triggeredGuidanceThisFrame = false;

        foreach (var fridge in eligibleFridgeList)
        {
            if (fridge == null || previouslyEligibleSet.Contains(fridge))
            {
                continue;
            }

            switch (lastGlowCommand)
            {
                case GlowCommand.Enable:
                    fridge.EnableGlow();
                    triggeredGuidanceThisFrame = true;
                    break;
                case GlowCommand.Disable:
                    fridge.DisableGlow();
                    break;
                case GlowCommand.EnableForDuration:
                    fridge.EnableGlowForDuration(lastGlowDurationSeconds);
                    triggeredGuidanceThisFrame = true;
                    break;
            }
        }

        if (triggeredGuidanceThisFrame && lastGlowTriggeredGuidance)
        {
            if (lastGlowCommand == GlowCommand.EnableForDuration && lastGlowDurationSeconds > 0f)
            {
                TriggerGuidance(true, lastGlowDurationSeconds);
            }
            else if (lastGlowCommand == GlowCommand.Enable)
            {
                TriggerGuidance(false, 0f);
            }
        }
    }

    private PlayerFridgeGuidance ResolveFridgeGuidance()
    {
        if (fridgeGuidance != null)
        {
            return fridgeGuidance;
        }

#if UNITY_2023_1_OR_NEWER
        fridgeGuidance = FindFirstObjectByType<PlayerFridgeGuidance>(FindObjectsInactive.Exclude);
#else
        fridgeGuidance = FindObjectOfType<PlayerFridgeGuidance>();
#endif

        if (fridgeGuidance == null && !hasLoggedMissingGuidance)
        {
            hasLoggedMissingGuidance = true;
            if (enableDebugLogs)
            {
                Debug.LogWarning("FridgeGlowEligibilityTracker: Guidance activation requested but no PlayerFridgeGuidance was found in the scene.", this);
            }
        }

        return fridgeGuidance;
    }

    private void TriggerGuidance(bool useDuration, float durationSeconds)
    {
        PlayerFridgeGuidance guidance = ResolveFridgeGuidance();
        if (guidance == null)
        {
            return;
        }

        if (useDuration && durationSeconds > 0f)
        {
            guidance.ActivateGuidanceForSeconds(durationSeconds);
        }
        else
        {
            guidance.ActivateGuidance();
        }
    }
}
