using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FridgeGlowManager : IFridgeGlowManager
{
    public event Action<IReadOnlyCollection<FoodSource>> EligibleFridgesChanged;

    private readonly IOrderManager orderManager;
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly HashSet<FoodSource> trackedFridges = new HashSet<FoodSource>();
    private readonly bool enableDebugLogs;
    private int eligibilityVersion;
    private FoodSource[] lastEligibleSnapshot = Array.Empty<FoodSource>();

    public FridgeGlowManager(IOrderManager orderManager)
    {
        this.orderManager = orderManager;
        enableDebugLogs = Debug.isDebugBuild;
    }

    public int EligibilityVersion => eligibilityVersion;

    public async UniTask Init()
    {
        InitializeSubscriptions();
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

    private void NotifyEligibleFridgesChanged()
    {
        lastEligibleSnapshot = BuildEligibleSnapshot();
        eligibilityVersion++;

        if (enableDebugLogs)
        {
            var names = lastEligibleSnapshot.Length > 0
                ? string.Join(", ", lastEligibleSnapshot.Select(f => f != null ? f.ItemName : "<null>"))
                : "(none)";
            Debug.Log($"FridgeGlowManager: Eligible fridge count {lastEligibleSnapshot.Length} [{names}]");
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
}
