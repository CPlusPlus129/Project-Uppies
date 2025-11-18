using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using R3;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FridgeGlowManager : IFridgeGlowManager
{
    public event Action<IReadOnlyCollection<FoodSource>> EligibleFridgesChanged;

    private readonly IOrderManager orderManager;
    private const float MinInventoryCheckIntervalSeconds = 0.05f;
    private const float DefaultInventoryCheckIntervalSeconds = 0.25f;

    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly HashSet<FoodSource> trackedFridges = new HashSet<FoodSource>();
    private readonly HashSet<string> inventoryItems = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    private readonly bool enableDebugLogs;
    private readonly IInventorySystem inventorySystem;

    private CancellationTokenSource inventoryMonitorCts;
    private float inventoryCheckIntervalSeconds = DefaultInventoryCheckIntervalSeconds;
    private int eligibilityVersion;
    private FoodSource[] lastEligibleSnapshot = Array.Empty<FoodSource>();
    private FoodSource[] lastSuppressedSnapshot = Array.Empty<FoodSource>();

    public FridgeGlowManager(IOrderManager orderManager, IInventorySystem inventorySystem)
    {
        this.orderManager = orderManager;
        this.inventorySystem = inventorySystem;
        //enableDebugLogs = Debug.isDebugBuild;
        enableDebugLogs = false;
    }

    public int EligibilityVersion => eligibilityVersion;

    public async UniTask Init()
    {
        InitializeSubscriptions();
        InitializeInventoryMonitoring();
        NotifyEligibleFridgesChanged();
        await UniTask.CompletedTask;
    }

    private void InitializeSubscriptions()
    {
        if (orderManager != null)
        {
            orderManager.OnNewOrder
                .Subscribe(order => HandleNewOrder(order))
                .AddTo(disposables);

            orderManager.OnOrderServed
                .Subscribe(order => HandleOrderServed(order))
                .AddTo(disposables);

            orderManager.OnOrdersCleared
                .Subscribe(_ => HandleOrdersCleared())
                .AddTo(disposables);

            if (enableDebugLogs) Debug.Log("FridgeGlowManager: Subscribed to OrderManager events");
        }
        else
        {
            Debug.LogError("FridgeGlowManager: OrderManager is null!");
        }
    }

    private void InitializeInventoryMonitoring()
    {
        if (inventorySystem == null)
        {
            Debug.LogError("FridgeGlowManager: InventorySystem is null!");
            return;
        }

        UpdateInventoryCache(inventorySystem.GetAllItems());

        inventorySystem.OnInventoryChanged
            .Subscribe(items =>
            {
                if (UpdateInventoryCache(items))
                {
                    if (enableDebugLogs) Debug.Log("FridgeGlowManager: Inventory change detected, refreshing glow states");
                    NotifyEligibleFridgesChanged();
                }
            })
            .AddTo(disposables);

        RestartInventoryMonitorLoop();
    }

    private void HandleNewOrder(Order order)
    {
        if (order == null)
        {
            if (enableDebugLogs) Debug.LogWarning("FridgeGlowManager: Received null order");
            return;
        }

        var recipe = GetRecipeForOrder(order);
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
        {
            if (enableDebugLogs) Debug.LogWarning($"FridgeGlowManager: No recipe found for {order.MealName}");
            return;
        }

        if (enableDebugLogs)
        {
            var matches = GetUniqueFridgesForIngredients(recipe.ingredients);
            if (matches.Count == 0)
            {
                Debug.LogWarning($"FridgeGlowManager: No eligible fridges found for order {order.MealName}");
            }
            else
            {
                Debug.Log($"FridgeGlowManager: Order {order.MealName} eligible fridges → {string.Join(", ", matches.Select(f => f.ItemName))}");
            }
        }

        NotifyEligibleFridgesChanged();
    }

    private void HandleOrderServed(Order order)
    {
        if (enableDebugLogs) Debug.Log($"FridgeGlowManager: Order served {order?.MealName ?? "<null>"}");
        NotifyEligibleFridgesChanged();
    }

    private void HandleOrdersCleared()
    {
        if (enableDebugLogs) Debug.Log("FridgeGlowManager: Orders cleared");
        NotifyEligibleFridgesChanged();
    }

    public void RefreshGlowStates()
    {
        if (enableDebugLogs) Debug.Log("FridgeGlowManager: RefreshGlowStates invoked");
        if (inventorySystem != null && UpdateInventoryCache(inventorySystem.GetAllItems()))
        {
            if (enableDebugLogs) Debug.Log("FridgeGlowManager: Inventory cache refreshed via manual glow refresh");
        }
        NotifyEligibleFridgesChanged();
    }

    public void RegisterFoodSource(FoodSource foodSource)
    {
        if (foodSource == null)
            return;

        PruneTrackedFridges();

        if (trackedFridges.Add(foodSource))
        {
            if (enableDebugLogs) Debug.Log($"FridgeGlowManager: Registered fridge {foodSource.ItemName}");
            if (orderManager?.pendingOrders?.Count > 0)
            {
                NotifyEligibleFridgesChanged();
            }
        }
    }

    public void UnregisterFoodSource(FoodSource foodSource)
    {
        if (foodSource == null)
            return;

        if (trackedFridges.Remove(foodSource))
        {
            if (enableDebugLogs) Debug.Log($"FridgeGlowManager: Unregistered fridge {foodSource.ItemName}");
            NotifyEligibleFridgesChanged();
        }
    }

    public IReadOnlyCollection<FoodSource> GetEligibleFridgesSnapshot()
    {
        return lastEligibleSnapshot;
    }

    public void SetInventoryCheckInterval(float seconds)
    {
        float clamped = Mathf.Max(MinInventoryCheckIntervalSeconds, seconds);
        if (Mathf.Approximately(inventoryCheckIntervalSeconds, clamped))
        {
            inventoryCheckIntervalSeconds = clamped;
            return;
        }

        inventoryCheckIntervalSeconds = clamped;

        if (enableDebugLogs) Debug.Log($"FridgeGlowManager: Inventory check interval set to {inventoryCheckIntervalSeconds:0.###} seconds");

        if (inventorySystem != null)
        {
            RestartInventoryMonitorLoop();
        }
    }

    private void NotifyEligibleFridgesChanged()
    {
        var allEligible = BuildEligibleSnapshot();
        lastEligibleSnapshot = ApplyInventorySuppression(allEligible, out var suppressed);
        lastSuppressedSnapshot = suppressed;
        eligibilityVersion++;

        if (enableDebugLogs)
        {
            var activeNames = lastEligibleSnapshot.Length > 0
                ? string.Join(", ", lastEligibleSnapshot.Select(f => f != null ? f.ItemName : "<null>"))
                : "(none)";
            var suppressedNames = lastSuppressedSnapshot.Length > 0
                ? string.Join(", ", lastSuppressedSnapshot.Select(f => f != null ? f.ItemName : "<null>"))
                : "(none)";
            Debug.Log($"FridgeGlowManager: Eligible (active={lastEligibleSnapshot.Length}, suppressed={lastSuppressedSnapshot.Length}) Active[{activeNames}] Suppressed[{suppressedNames}]");
        }

        EligibleFridgesChanged?.Invoke(lastEligibleSnapshot);
    }

    private FoodSource[] BuildEligibleSnapshot()
    {
        PruneTrackedFridges();

        if (orderManager?.pendingOrders == null || orderManager.pendingOrders.Count == 0)
        {
            return Array.Empty<FoodSource>();
        }

        var fridgeSet = new HashSet<FoodSource>();
        var snapshot = new List<FoodSource>();

        foreach (var pendingOrder in orderManager.pendingOrders)
        {
            var recipe = GetRecipeForOrder(pendingOrder);
            if (recipe?.ingredients == null || recipe.ingredients.Length == 0)
            {
                continue;
            }

            foreach (var fridge in GetUniqueFridgesForIngredients(recipe.ingredients))
            {
                if (fridgeSet.Add(fridge))
                {
                    snapshot.Add(fridge);
                }
            }
        }

        return snapshot.Count == 0 ? Array.Empty<FoodSource>() : snapshot.ToArray();
    }

    private Recipe GetRecipeForOrder(Order order)
    {
        if (order == null)
        {
            return null;
        }

        if (order.Recipe != null)
        {
            return order.Recipe;
        }

        return Database.Instance?.recipeData?.GetRecipeByName(order.MealName);
    }

    private List<FoodSource> GetUniqueFridgesForIngredients(IEnumerable<string> ingredients)
    {
        var matches = new List<FoodSource>();
        if (ingredients == null)
        {
            return matches;
        }

        PruneTrackedFridges();

        var ingredientSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        var fridgeSet = new HashSet<FoodSource>();

        foreach (var ingredient in ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient) || !ingredientSet.Add(ingredient))
            {
                continue;
            }

            foreach (var fridge in trackedFridges)
            {
                if (fridge == null)
                {
                    continue;
                }

                if (fridge.ContainsIngredient(ingredient) && fridgeSet.Add(fridge))
                {
                    matches.Add(fridge);
                }
            }
        }

        return matches;
    }

    private void PruneTrackedFridges()
    {
        trackedFridges.RemoveWhere(f => f == null);
    }

    private bool UpdateInventoryCache(IReadOnlyList<ItemBase> items)
    {
        if (items == null)
        {
            return false;
        }

        var newItems = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.ItemName))
            {
                newItems.Add(item.ItemName);
            }
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

        return true;
    }

    private bool ShouldSuppressFridge(FoodSource fridge)
    {
        if (fridge == null || string.IsNullOrWhiteSpace(fridge.ItemName))
        {
            return false;
        }

        return inventoryItems.Contains(fridge.ItemName);
    }

    private FoodSource[] ApplyInventorySuppression(FoodSource[] fridges, out FoodSource[] suppressed)
    {
        if (fridges == null || fridges.Length == 0)
        {
            suppressed = Array.Empty<FoodSource>();
            return Array.Empty<FoodSource>();
        }

        var activeList = new List<FoodSource>(fridges.Length);
        List<FoodSource> suppressedList = null;

        foreach (var fridge in fridges)
        {
            if (fridge == null)
            {
                continue;
            }

            if (ShouldSuppressFridge(fridge))
            {
                suppressedList ??= new List<FoodSource>();
                suppressedList.Add(fridge);
                continue;
            }

            activeList.Add(fridge);
        }

        suppressed = suppressedList == null || suppressedList.Count == 0
            ? Array.Empty<FoodSource>()
            : suppressedList.ToArray();

        return activeList.Count == 0 ? Array.Empty<FoodSource>() : activeList.ToArray();
    }

    private void RestartInventoryMonitorLoop()
    {
        inventoryMonitorCts?.Cancel();
        inventoryMonitorCts?.Dispose();

        if (inventorySystem == null)
        {
            inventoryMonitorCts = null;
            return;
        }

        inventoryMonitorCts = new CancellationTokenSource();
        MonitorInventoryLoop(inventoryMonitorCts.Token).Forget();
    }

    private async UniTaskVoid MonitorInventoryLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delaySeconds = Mathf.Max(MinInventoryCheckIntervalSeconds, inventoryCheckIntervalSeconds);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested || inventorySystem == null)
            {
                continue;
            }

            if (UpdateInventoryCache(inventorySystem.GetAllItems()))
            {
                if (enableDebugLogs) Debug.Log("FridgeGlowManager: Inventory poll detected change");
                NotifyEligibleFridgesChanged();
            }
        }
    }

    public void Dispose()
    {
        // Cancel and dispose inventory monitoring
        inventoryMonitorCts?.Cancel();
        inventoryMonitorCts?.Dispose();
        inventoryMonitorCts = null;

        // Dispose all subscriptions
        disposables?.Dispose();

        // Clear collections
        trackedFridges.Clear();
        inventoryItems.Clear();
        lastEligibleSnapshot = System.Array.Empty<FoodSource>();
        lastSuppressedSnapshot = System.Array.Empty<FoodSource>();

        // Clear event
        EligibleFridgesChanged = null;
    }
}
