using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;

public class OrderManager : IOrderManager
{
    public Subject<Order> OnNewOrder { get; } = new Subject<Order>();
    public Subject<Order> OnOrderServed { get; } = new Subject<Order>();
    public Subject<Unit> OnOrdersCleared { get; } = new Subject<Unit>();
    private List<Order> _pendingOrders = new List<Order>();
    public IReadOnlyList<Order> pendingOrders => _pendingOrders;

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    public void PlaceOrder(Order order)
    {
        _pendingOrders.Add(order);
        OnNewOrder.OnNext(order);
    }

    public bool ServeOrder(Order servedOrder)
    {
        var match = _pendingOrders.Find(o => o.Equals(servedOrder));
        if (match != null)
        {
            PlayerStatSystem.Instance.Money.Value += UnityEngine.Random.Range(100, 151);
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
}