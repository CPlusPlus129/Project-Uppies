using R3;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Overwhelming Kitchen UI system.
/// Decoupled from the system via RX data streams, automatically updates all UI elements.
/// </summary>
public class OverwhelmingKitchenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OverwhelmingKitchenSystem kitchenSystem;

    [Header("UI Elements")]
    [SerializeField] private GameObject uiRootPanel;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI completedCountText;
    [SerializeField] private Transform orderListContainer;
    [SerializeField] private GameObject orderCardPrefab;
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Money Display Settings")]
    [SerializeField] private Color normalMoneyColor = Color.white;
    [SerializeField] private Color negativeMoneyColor = Color.red;
    [SerializeField] private Color warningMoneyColor = Color.yellow;
    [SerializeField] private int warningThreshold = 100;

    private CompositeDisposable disposables = new CompositeDisposable();
    private Dictionary<Order, OverwhelmingKitchenOrderCard> orderCardMap = new Dictionary<Order, OverwhelmingKitchenOrderCard>();
    private List<GameObject> inventorySlots = new List<GameObject>();

    private void OnEnable()
    {
        kitchenSystem ??= UnityEngine.Object.FindFirstObjectByType<OverwhelmingKitchenSystem>();
        if (kitchenSystem == null)
        {
            Debug.LogError("[OverwhelmingKitchenUI] Kitchen system reference is null!");
            return;
        }

        SubscribeToDataStreams();
    }

    private void OnDisable()
    {
        UnsubscribeFromDataStreams();
    }

    private void SubscribeToDataStreams()
    {
        // Subscribe to Money
        kitchenSystem.Money
            .Subscribe(money => UpdateMoneyDisplay(money))
            .AddTo(disposables);

        // Subscribe to CompletedCount
        kitchenSystem.CompletedCount
            .Subscribe(count => UpdateCompletedCountDisplay(count))
            .AddTo(disposables);

        // Subscribe to ActiveOrders
        kitchenSystem.ActiveOrders
            .Subscribe(orders => UpdateOrderList(orders))
            .AddTo(disposables);

        // Subscribe to Inventory
        kitchenSystem.Inventory
            .Subscribe(items => UpdateInventoryDisplay(items))
            .AddTo(disposables);

        // Subscribe to CurrentState
        kitchenSystem.CurrentState
            .Subscribe(state => UpdateUIVisibility(state))
            .AddTo(disposables);

        // Subscribe to OnOrderCompleted for animations
        kitchenSystem.OnOrderCompleted
            .Subscribe(order => OnOrderCompletedAnimation(order))
            .AddTo(disposables);

        Debug.Log("[OverwhelmingKitchenUI] Subscribed to all data streams");
    }

    private void UnsubscribeFromDataStreams()
    {
        disposables.Clear();
        Debug.Log("[OverwhelmingKitchenUI] Unsubscribed from all data streams");
    }

    private void UpdateMoneyDisplay(int money)
    {
        if (moneyText == null) return;

        moneyText.text = $"${money}";

        // Color based on value
        if (money < 0)
        {
            moneyText.color = negativeMoneyColor;
        }
        else if (money < warningThreshold)
        {
            moneyText.color = warningMoneyColor;
        }
        else
        {
            moneyText.color = normalMoneyColor;
        }
    }

    private void UpdateCompletedCountDisplay(int count)
    {
        if (completedCountText == null) return;

        completedCountText.text = $"Completed: {count} / {kitchenSystem.RequiredCompletions}";
    }

    private void UpdateOrderList(List<Order> orders)
    {
        if (orderListContainer == null || orderCardPrefab == null) return;

        // Remove cards for orders that no longer exist
        var ordersToRemove = new List<Order>();
        foreach (var kvp in orderCardMap)
        {
            if (!orders.Contains(kvp.Key))
            {
                ordersToRemove.Add(kvp.Key);
            }
        }

        foreach (var order in ordersToRemove)
        {
            if (orderCardMap.TryGetValue(order, out var card))
            {
                if (card != null)
                    Destroy(card.gameObject);

                orderCardMap.Remove(order);
            }
        }

        // Add cards for new orders
        foreach (var order in orders)
        {
            if (!orderCardMap.ContainsKey(order))
            {
                CreateOrderCard(order);
            }
        }

        Debug.Log($"[OverwhelmingKitchenUI] Updated order list: {orders.Count} orders");
    }

    private void CreateOrderCard(Order order)
    {
        var cardObject = Instantiate(orderCardPrefab, orderListContainer);
        var card = cardObject.GetComponent<OverwhelmingKitchenOrderCard>();

        if (card != null)
        {
            card.SetOrder(order);
            orderCardMap[order] = card;
        }
        else
        {
            Debug.LogError("[OverwhelmingKitchenUI] Order card prefab missing OverwhelmingKitchenOrderCard component!");
            Destroy(cardObject);
        }
    }

    private void OnOrderCompletedAnimation(Order order)
    {
        if (orderCardMap.TryGetValue(order, out var card))
        {
            if (card != null)
            {
                card.PlayCompleteAnimation();
            }
        }
    }

    private void UpdateInventoryDisplay(List<ItemBase> items)
    {
        if (inventoryContainer == null || inventorySlotPrefab == null) return;

        // Clear existing slots
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        inventorySlots.Clear();

        // Create new slots
        foreach (var item in items)
        {
            var slotObject = Instantiate(inventorySlotPrefab, inventoryContainer);
            inventorySlots.Add(slotObject);

            // Set item info (assuming slot has a TextMeshProUGUI child)
            var text = slotObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = item != null ? item.ItemName : "Empty";
            }
        }

        Debug.Log($"[OverwhelmingKitchenUI] Updated inventory display: {items.Count} items");
    }

    private void UpdateUIVisibility(OverwhelmingKitchenState state)
    {
        if (uiRootPanel == null) return;

        uiRootPanel.SetActive(state == OverwhelmingKitchenState.Running || state == OverwhelmingKitchenState.Completed);

        Debug.Log($"[OverwhelmingKitchenUI] UI visibility: {uiRootPanel.activeSelf} (State: {state})");
    }

    private void OnDestroy()
    {
        UnsubscribeFromDataStreams();
        disposables?.Dispose();
    }
}
