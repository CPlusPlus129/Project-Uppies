using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : IOrderManager
{
    public Subject<Order> OnNewOrder { get; } = new Subject<Order>();
    public Subject<Order> OnOrderServed { get; } = new Subject<Order>();
    public Subject<Unit> OnOrdersCleared { get; } = new Subject<Unit>();
    public Subject<OrderRewardResult> OnOrderRewarded { get; } = new Subject<OrderRewardResult>();
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
            var rewardResult = CalculateOrderReward(match, servedMeal);
            PlayerStatSystem.Instance.Money.Value += rewardResult.TotalRewardRounded;
            _pendingOrders.Remove(match);
            OnOrderServed.OnNext(match);
            OnOrderRewarded.OnNext(rewardResult);
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

    private OrderRewardResult CalculateOrderReward(Order order, Meal servedMeal)
    {
        var placedTime = order.PlacedAtTime <= 0f ? Time.time : order.PlacedAtTime;
        var completionDuration = Mathf.Max(0f, Time.time - placedTime);
        var clampedDuration = Mathf.Clamp(completionDuration, quickServeSeconds, slowServeSeconds);
        var durationWindow = Mathf.Max(0.001f, slowServeSeconds - quickServeSeconds);
        var timeScore = 1f - ((clampedDuration - quickServeSeconds) / durationWindow);

        var qualityScore = servedMeal != null ? Mathf.Clamp01(servedMeal.Quality) : 0.5f;
        var totalWeight = Mathf.Max(0.001f, timeWeight + qualityWeight);
        var combinedScore = ((timeScore * timeWeight) + (qualityScore * qualityWeight)) / totalWeight;

        var totalReward = Mathf.Lerp(minReward, maxReward, combinedScore);
        var normalizedTimeWeight = timeWeight / totalWeight;
        var normalizedQualityWeight = qualityWeight / totalWeight;
        var weightedTimeScore = Mathf.Max(0f, timeScore) * normalizedTimeWeight;
        var weightedQualityScore = Mathf.Max(0f, qualityScore) * normalizedQualityWeight;
        var contributionSum = Mathf.Max(0.001f, weightedTimeScore + weightedQualityScore);
        var timeContributionRatio = weightedTimeScore / contributionSum;
        var qualityContributionRatio = weightedQualityScore / contributionSum;

        return new OrderRewardResult
        {
            Order = order,
            Meal = servedMeal,
            TotalReward = totalReward,
            RoundedReward = Mathf.RoundToInt(totalReward),
            CombinedScore = Mathf.Clamp01(combinedScore),
            CompletionSeconds = completionDuration,
            TimeScore = Mathf.Clamp01(timeScore),
            QualityScore = Mathf.Clamp01(qualityScore),
            TimeContribution = totalReward * timeContributionRatio,
            QualityContribution = totalReward * qualityContributionRatio,
            TimeContributionRatio = timeContributionRatio,
            QualityContributionRatio = qualityContributionRatio,
            TimeWeightNormalized = normalizedTimeWeight,
            QualityWeightNormalized = normalizedQualityWeight,
            QuickServeSeconds = quickServeSeconds,
            SlowServeSeconds = slowServeSeconds,
            MinReward = minReward,
            MaxReward = maxReward
        };
    }

    public void Dispose()
    {
        OnNewOrder?.Dispose();
        OnOrderServed?.Dispose();
        OnOrdersCleared?.Dispose();
        OnOrderRewarded?.Dispose();
        _pendingOrders.Clear();
    }
}
