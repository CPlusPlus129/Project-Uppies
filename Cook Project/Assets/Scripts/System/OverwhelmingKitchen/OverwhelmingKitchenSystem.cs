using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public enum OverwhelmingKitchenState
{
    Idle,
    Running,
    Completed
}

/// <summary>
/// Core system for the Overwhelming Kitchen minigame.
/// Completely independent, does not use ServiceLocator, and does not affect the main game systems.
/// </summary>
public class OverwhelmingKitchenSystem : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject doorBlocker;
    [SerializeField] private GameObject victoryTriggerObject;
    [SerializeField] private GameObject[] fireEffects;

    [Header("Game Settings")]
    [SerializeField] private int initialMoney = 1000;
    [SerializeField] private float drainInterval = 1f;
    [SerializeField] private int baseDrainAmount = 10;
    [SerializeField] private float drainGrowthRate = 1.1f;
    [SerializeField] private int requiredCompletions = 10;
    [SerializeField] private int initialOrderCount = 10;

    // RX Data Streams
    public ReactiveProperty<int> Money { get; private set; }
    public ReactiveProperty<int> CompletedCount { get; private set; }
    public ReactiveProperty<OverwhelmingKitchenState> CurrentState { get; private set; }
    public ReadOnlyReactiveProperty<List<Order>> ActiveOrders { get; private set; }
    public ReadOnlyReactiveProperty<List<ItemBase>> Inventory { get; private set; }

    // Events
    public Subject<Order> OnOrderCompleted { get; private set; }
    public Subject<Unit> OnGameComplete { get; private set; }

    // Internal State
    private readonly ReactiveProperty<List<Order>> activeOrders = new ReactiveProperty<List<Order>>(new List<Order>());
    private readonly ReactiveProperty<List<ItemBase>> inventory = new ReactiveProperty<List<ItemBase>>(new List<ItemBase>());
    private readonly List<GameObject> spawnedItemObjects = new List<GameObject>();
    private readonly CompositeDisposable disposables = new CompositeDisposable();

    private CancellationTokenSource drainCts;
    private int drainTick = 0;

    public int RequiredCompletions => requiredCompletions;

    private void Awake()
    {
        InitializeProperties();
    }

    private void InitializeProperties()
    {
        Money = new ReactiveProperty<int>(initialMoney);
        CompletedCount = new ReactiveProperty<int>(0);
        CurrentState = new ReactiveProperty<OverwhelmingKitchenState>(OverwhelmingKitchenState.Idle);
        OnOrderCompleted = new Subject<Order>();
        OnGameComplete = new Subject<Unit>();

        ActiveOrders = activeOrders.ToReadOnlyReactiveProperty();
        Inventory = inventory.ToReadOnlyReactiveProperty();
    }

    /// <summary>
    /// Start the minigame
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[OverwhelmingKitchen] Starting game...");

        // Reset all data
        ResetInternalState();

        // Initialize money
        Money.Value = initialMoney;
        CompletedCount.Value = 0;
        drainTick = 0;

        // Generate initial orders
        GenerateOrders(initialOrderCount);

        // Set state
        CurrentState.Value = OverwhelmingKitchenState.Running;

        // Activate scene objects
        if (doorBlocker != null)
            doorBlocker.SetActive(true);

        if (victoryTriggerObject != null)
            victoryTriggerObject.SetActive(false);

        foreach (var fire in fireEffects)
        {
            if (fire != null)
                fire.SetActive(false);
        }

        // Start money drain
        StartMoneyDrain();

        Debug.Log($"[OverwhelmingKitchen] Game started with {activeOrders.Value.Count} orders");
    }

    /// <summary>
    /// Generate orders
    /// </summary>
    private void GenerateOrders(int count)
    {
        var recipes = Database.Instance.recipeData.GetAllRecipes();
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogError("[OverwhelmingKitchen] No recipes found in database!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var randomRecipe = recipes[UnityEngine.Random.Range(0, recipes.Length)];
            var order = new Order()
            {
                CustomerName = $"Customer {activeOrders.Value.Count + 1}",
                MealName = randomRecipe.mealName,
                Recipe = randomRecipe,
                PlacedAtTime = Time.time
            };
            
            activeOrders.Value.Add(order);
        }

        // Trigger update
        activeOrders.ForceNotify();
    }

    /// <summary>
    /// Start continuous money drain
    /// </summary>
    private void StartMoneyDrain()
    {
        drainCts?.Cancel();
        drainCts?.Dispose();
        drainCts = new CancellationTokenSource();

        MoneyDrainLoop(drainCts.Token).Forget();
    }

    private async UniTaskVoid MoneyDrainLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && CurrentState.Value == OverwhelmingKitchenState.Running)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(drainInterval), cancellationToken: ct);

                if (ct.IsCancellationRequested)
                    break;

                drainTick++;
                int drainAmount = Mathf.CeilToInt(baseDrainAmount * Mathf.Pow(drainGrowthRate, drainTick - 1));
                Money.Value -= drainAmount;

                Debug.Log($"[OverwhelmingKitchen] Drained ${drainAmount}. Current money: ${Money.Value}");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
    }

    /// <summary>
    /// Add item to fake inventory
    /// </summary>
    public void AddItemToInventory(ItemBase item, GameObject itemObject)
    {
        if (CurrentState.Value != OverwhelmingKitchenState.Running)
            return;

        inventory.Value.Add(item);
        spawnedItemObjects.Add(itemObject);
        inventory.ForceNotify();

        Debug.Log($"[OverwhelmingKitchen] Added {item.ItemName} to inventory. Total items: {inventory.Value.Count}");
    }

    /// <summary>
    /// Check if the inventory has the specified ingredient
    /// </summary>
    public bool HasIngredient(string ingredientName)
    {
        return inventory.Value.Any(item => item.ItemName == ingredientName);
    }

    /// <summary>
    /// Remove ingredient from inventory
    /// </summary>
    public void RemoveIngredient(string ingredientName)
    {
        var item = inventory.Value.FirstOrDefault(i => i.ItemName == ingredientName);
        if (item != null)
        {
            int index = inventory.Value.IndexOf(item);
            inventory.Value.RemoveAt(index);

            // Also destroy the GameObject
            if (index < spawnedItemObjects.Count && spawnedItemObjects[index] != null)
            {
                Destroy(spawnedItemObjects[index]);
                spawnedItemObjects.RemoveAt(index);
            }

            inventory.ForceNotify();
            Debug.Log($"[OverwhelmingKitchen] Removed {ingredientName} from inventory");
        }
    }

    /// <summary>
    /// Complete an order
    /// </summary>
    public void CompleteOrder(Order order)
    {
        if (CurrentState.Value != OverwhelmingKitchenState.Running)
            return;

        if (!activeOrders.Value.Contains(order))
        {
            Debug.LogWarning($"[OverwhelmingKitchen] Order {order.MealName} not found in active orders");
            return;
        }

        // Remove order
        activeOrders.Value.Remove(order);
        CompletedCount.Value++;

        Debug.Log($"[OverwhelmingKitchen] Completed order: {order.MealName}. Total completed: {CompletedCount.Value}");

        // Notify
        OnOrderCompleted.OnNext(order);

        // Add new order to replace completed one
        if (CompletedCount.Value < requiredCompletions)
        {
            GenerateOrders(1);
        }

        activeOrders.ForceNotify();

        // Check win condition
        if (CompletedCount.Value >= requiredCompletions)
        {
            CompleteGame();
        }
    }

    /// <summary>
    /// Complete the game
    /// </summary>
    private void CompleteGame()
    {
        Debug.Log("[OverwhelmingKitchen] Game completed!");

        CurrentState.Value = OverwhelmingKitchenState.Completed;

        // Stop money drain
        drainCts?.Cancel();

        // Show victory object
        if (victoryTriggerObject != null)
            victoryTriggerObject.SetActive(true);

        // Notify
        OnGameComplete.OnNext(Unit.Default);
    }

    /// <summary>
    /// Trigger fire effects and end the game
    /// </summary>
    public void TriggerFireAndEnd()
    {
        Debug.Log("[OverwhelmingKitchen] Triggering fire effects...");

        foreach (var fire in fireEffects)
        {
            if (fire != null)
                fire.SetActive(true);
        }

        // Can trigger dialogue or other events here via GameFlow signals
    }

    /// <summary>
    /// Reset the game to initial state (for replay)
    /// </summary>
    public void ResetGame()
    {
        Debug.Log("[OverwhelmingKitchen] Resetting game...");

        // Cancel all async operations
        drainCts?.Cancel();
        drainCts?.Dispose();
        drainCts = null;

        // Destroy all spawned items
        foreach (var obj in spawnedItemObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedItemObjects.Clear();

        // Reset internal state
        ResetInternalState();

        // Reset scene objects
        if (doorBlocker != null)
            doorBlocker.SetActive(false);

        if (victoryTriggerObject != null)
            victoryTriggerObject.SetActive(false);

        foreach (var fire in fireEffects)
        {
            if (fire != null)
                fire.SetActive(false);
        }

        CurrentState.Value = OverwhelmingKitchenState.Idle;

        Debug.Log("[OverwhelmingKitchen] Game reset complete. Ready to play again.");
    }

    private void ResetInternalState()
    {
        activeOrders.Value.Clear();
        inventory.Value.Clear();
        activeOrders.ForceNotify();
        inventory.ForceNotify();
        Money.Value = initialMoney;
        CompletedCount.Value = 0;
        drainTick = 0;
    }

    private void OnDestroy()
    {
        // Clean up all resources
        drainCts?.Cancel();
        drainCts?.Dispose();

        foreach (var obj in spawnedItemObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        disposables?.Dispose();
        Money?.Dispose();
        CompletedCount?.Dispose();
        CurrentState?.Dispose();
        OnOrderCompleted?.Dispose();
        OnGameComplete?.Dispose();
        activeOrders?.Dispose();
        inventory?.Dispose();
    }
}
