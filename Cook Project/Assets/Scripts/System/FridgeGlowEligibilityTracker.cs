using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tracks which fridges are currently eligible to glow according to the FridgeGlowManager
/// and exposes helper methods to toggle glow on all eligible fridges simultaneously.
/// </summary>
public class FridgeGlowEligibilityTracker : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private readonly List<FoodSource> eligibleFridgeList = new List<FoodSource>();
    private readonly HashSet<FoodSource> eligibleFridgeSet = new HashSet<FoodSource>();
    private readonly List<FoodSource> previouslyEligibleFridges = new List<FoodSource>();

    private IFridgeGlowManager fridgeGlowManager;

    public IReadOnlyList<FoodSource> EligibleFridges => eligibleFridgeList;

    private async void Awake()
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await WaitForFridgeGlowManagerAsync(destroyToken);
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

        if (fridgeGlowManager == null)
        {
            Debug.LogError("FridgeGlowEligibilityTracker: Failed to resolve IFridgeGlowManager");
            return;
        }

        fridgeGlowManager.EligibleFridgesChanged += HandleEligibleFridgesChanged;
        SyncEligibleFridges(fridgeGlowManager.GetEligibleFridgesSnapshot());

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: Listening for eligible fridge updates. Initial count: {eligibleFridgeList.Count}");
        }
    }

    private async UniTask WaitForFridgeGlowManagerAsync(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => ServiceLocator.Instance.IsServiceReady<IFridgeGlowManager>(), cancellationToken: cancellationToken);
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

        eligibleFridgeSet.Clear();
        eligibleFridgeList.Clear();

        foreach (var fridge in fridges)
        {
            if (fridge == null)
            {
                continue;
            }

            if (eligibleFridgeSet.Add(fridge))
            {
                eligibleFridgeList.Add(fridge);
            }
        }

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
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: EnableGlowOnEligible → {eligibleFridgeList.Count} fridges [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.EnableGlow();
        }
    }

    public void DisableGlowOnEligible()
    {
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: DisableGlowOnEligible [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.DisableGlow();
        }
    }

    public void EnableGlowOnEligibleForDuration(float seconds)
    {
        RequestLatestSnapshot();

        if (enableDebugLogs)
        {
            Debug.Log($"FridgeGlowEligibilityTracker: EnableGlowOnEligibleForDuration({seconds}) → {eligibleFridgeList.Count} fridges [{string.Join(", ", eligibleFridgeList.Select(f => f != null ? f.ItemName : "<null>"))}]");
        }

        foreach (FoodSource fridge in eligibleFridgeList)
        {
            fridge?.EnableGlowForDuration(seconds);
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
}
