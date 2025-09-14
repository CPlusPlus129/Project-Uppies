using R3;
using System.Collections.Generic;

public class OrderManager : SimpleSingleton<OrderManager>
{
    public Subject<Order> OnNewOrder = new Subject<Order>();
    public Subject<Order> OnOrderServed = new Subject<Order>();
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
            pendingOrders.Remove(match);
            OnOrderServed.OnNext(match);
            return true;
        }
        return false;
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