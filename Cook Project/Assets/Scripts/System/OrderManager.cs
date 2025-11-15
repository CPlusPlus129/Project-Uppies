using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : IOrderManager
{
    public Subject<Order> OnNewOrder { get; } = new Subject<Order>();
    public Subject<Order> OnOrderServed { get; } = new Subject<Order>();
    public Subject<Unit> OnOrdersCleared { get; } = new Subject<Unit>();
    private List<Order> _pendingOrders = new List<Order>();
    public IReadOnlyList<Order> pendingOrders => _pendingOrders;
    private const float quickServeSeconds = 60f;
    private const float slowServeSeconds = 240f;
    private const float minReward = 40f;
    private const float maxReward = 250f;
    private const float timeWeight = 0.45f;
    private const float qualityWeight = 0.55f;

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    public void PlaceOrder(Order order)
    {
        _pendingOrders.Add(order);
        OnNewOrder.OnNext(order);
    }

    public bool ServeOrder(Order servedOrder, Meal servedMeal)
    {
        var match = _pendingOrders.Find(o => o.Equals(servedOrder));
        if (match != null)
        {
            var reward = CalculateOrderReward(match, servedMeal);
            PlayerStatSystem.Instance.Money.Value += Mathf.RoundToInt(reward);
            _pendingOrders.Remove(match);
            OnOrderServed.OnNext(match);
            return true;
        }
        return false;
    }

    public void ClearOrders()
    {
        _pendingOrders.Clear();
        OnOrdersCleared.OnNext(Unit.Default);
    }

    public bool CustomerHasPendingOrder(string customerName)
    {
        return _pendingOrders.Exists(o => o.CustomerName == customerName);
    }

    public Order GetPendingOrderForCustomer(string customerName)
    {
        return _pendingOrders.Find(o => o.CustomerName == customerName);
    }

    private float CalculateOrderReward(Order order, Meal servedMeal)
    {
        var placedTime = order.PlacedAtTime <= 0f ? Time.time : order.PlacedAtTime;
        var completionDuration = Mathf.Max(0f, Time.time - placedTime);
        var clampedDuration = Mathf.Clamp(completionDuration, quickServeSeconds, slowServeSeconds);
        var durationWindow = Mathf.Max(0.001f, slowServeSeconds - quickServeSeconds);
        var timeScore = 1f - ((clampedDuration - quickServeSeconds) / durationWindow);

        var qualityScore = servedMeal != null ? Mathf.Clamp01(servedMeal.Quality) : 0.5f;
        var totalWeight = Mathf.Max(0.001f, timeWeight + qualityWeight);
        var combinedScore = ((timeScore * timeWeight) + (qualityScore * qualityWeight)) / totalWeight;

        return Mathf.Lerp(minReward, maxReward, combinedScore);
    }

    public void Dispose()
    {
        OnNewOrder?.Dispose();
        OnOrderServed?.Dispose();
        OnOrdersCleared?.Dispose();
        _pendingOrders.Clear();
    }
}
