using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using R3;

public class FridgeGlowManager : MonoSingleton<FridgeGlowManager>
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private Dictionary<Order, List<FoodSource>> glowingFridgesPerOrder = new Dictionary<Order, List<FoodSource>>();
    private CompositeDisposable disposables = new CompositeDisposable();
    
    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(InitializeSubscriptions());
    }
    
    private System.Collections.IEnumerator InitializeSubscriptions()
    {
        yield return null;
        
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.OnNewOrder
                .Subscribe(order => HandleNewOrder(order))
                .AddTo(disposables);
                
            OrderManager.Instance.OnOrderServed
                .Subscribe(order => HandleOrderServed(order))
                .AddTo(disposables);
                
            OrderManager.Instance.OnOrdersCleared
                .Subscribe(_ => HandleOrdersCleared())
                .AddTo(disposables);
                
            if (enableDebugLogs) Debug.Log("FridgeGlowManager: Subscribed to OrderManager events");
        }
        else
        {
            Debug.LogError("FridgeGlowManager: OrderManager.Instance is null!");
        }
    }
    
    private void HandleNewOrder(Order order)
    {
        if (order == null)
        {
            if (enableDebugLogs) Debug.LogWarning("FridgeGlowManager: Received null order");
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"New Order: {order.MealName}");
        
        if (Database.Instance == null || Database.Instance.recipeData == null)
        {
            Debug.LogError("FridgeGlowManager: Database or RecipeData is not available");
            return;
        }
        
        Recipe recipe = Database.Instance.recipeData.GetRecipeByName(order.MealName);
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
        {
            if (enableDebugLogs) Debug.LogWarning($"No recipe found for {order.MealName}");
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"Ingredients needed: {string.Join(", ", recipe.ingredients)}");
        
        FoodSource[] allFridges = FindObjectsByType<FoodSource>(FindObjectsSortMode.None);
        if (allFridges == null || allFridges.Length == 0)
        {
            if (enableDebugLogs) Debug.LogWarning("No fridges found in scene");
            return;
        }
        
        HashSet<string> uniqueIngredients = new HashSet<string>(recipe.ingredients, System.StringComparer.InvariantCultureIgnoreCase);
        List<FoodSource> fridgesToGlow = new List<FoodSource>();
        
        foreach (string ingredient in uniqueIngredients)
        {
            FoodSource fridge = allFridges.FirstOrDefault(f => f.ContainsIngredient(ingredient));
            if (fridge != null && !fridgesToGlow.Contains(fridge))
            {
                fridgesToGlow.Add(fridge);
                if (enableDebugLogs) Debug.Log($"Matched '{ingredient}' → {fridge.ItemName}");
            }
            else if (fridge == null && enableDebugLogs)
            {
                Debug.LogWarning($"No fridge found for ingredient: {ingredient}");
            }
        }
        
        if (fridgesToGlow.Count > 0)
        {
            glowingFridgesPerOrder[order] = fridgesToGlow;
            
            foreach (var fridge in fridgesToGlow)
            {
                fridge.StartGlowing();
            }
            
            if (enableDebugLogs) Debug.Log($"Started glowing {fridgesToGlow.Count} fridges for {order.MealName}");
        }
        else if (enableDebugLogs)
        {
            Debug.LogError($"No fridges found with ingredients for {order.MealName}");
        }
    }
    
    private void HandleOrderServed(Order order)
    {
        Order matchingOrder = glowingFridgesPerOrder.Keys.FirstOrDefault(o => o.Equals(order));
        
        if (matchingOrder != null && glowingFridgesPerOrder.TryGetValue(matchingOrder, out List<FoodSource> fridges))
        {
            foreach (var fridge in fridges)
            {
                if (!IsFridgeNeededForOtherOrders(fridge, matchingOrder))
                {
                    fridge.StopGlowing();
                }
            }
            
            glowingFridgesPerOrder.Remove(matchingOrder);
            
            if (enableDebugLogs) Debug.Log($"Stopped glowing fridges for order: {order.MealName}");
        }
    }
    
    private void HandleOrdersCleared()
    {
        foreach (var kvp in glowingFridgesPerOrder)
        {
            foreach (var fridge in kvp.Value)
            {
                if (fridge != null) fridge.StopGlowing();
            }
        }
        
        glowingFridgesPerOrder.Clear();
        if (enableDebugLogs) Debug.Log("Cleared all glowing fridges");
    }
    
    private bool IsFridgeNeededForOtherOrders(FoodSource fridge, Order excludeOrder)
    {
        foreach (var kvp in glowingFridgesPerOrder)
        {
            if (!kvp.Key.Equals(excludeOrder) && kvp.Value.Contains(fridge))
            {
                return true;
            }
        }
        return false;
    }
    
    public void RefreshGlowStates()
    {
        foreach (var kvp in glowingFridgesPerOrder)
        {
            foreach (var fridge in kvp.Value)
            {
                if (fridge != null) fridge.StopGlowing();
            }
        }
        glowingFridgesPerOrder.Clear();
        
        var pendingOrders = OrderManager.Instance.GetPendingOrders();
        foreach (var order in pendingOrders)
        {
            HandleNewOrder(order);
        }
    }
    
    override public void OnDestroy()
    {
        disposables?.Dispose();
    }
}
