using R3;
using System.Collections.Generic;

public class OrderManager : SimpleSingleton<OrderManager>
{
    public Subject<Order> OnNewOrder = new Subject<Order>();
    public Subject<Order> OnOrderServed = new Subject<Order>();
    public Subject<Unit> OnOrdersCleared = new Subject<Unit>();
    private List<Order> pendingOrders = new List<Order>();

    public void PlaceOrder(Order order)
    {
        pendingOrders.Add(order);
        OnNewOrder.OnNext(order);
    }

    public bool ServeOrder(Order servedOrder)
    {
        var match = pendingOrders.Find(o => o.Equals(servedOrder));
        if (match != null)
        {
            PlayerStatSystem.Instance.Money.Value += UnityEngine.Random.Range(100, 151);
            pendingOrders.Remove(match);
            OnOrderServed.OnNext(match);
            return true;
        }
        return false;
    }

    public void ClearOrders()
    {
        pendingOrders.Clear();
        OnOrdersCleared.OnNext(Unit.Default);
    }

    public IReadOnlyList<Order> GetPendingOrders()
    {
        return pendingOrders;
    }

    public bool CustomerHasPendingOrder(string customerName)
    {
        return pendingOrders.Exists(o => o.CustomerName == customerName);
    }

    public Order GetPendingOrderForCustomer(string customerName)
    {
        return pendingOrders.Find(o => o.CustomerName == customerName);
    }
}