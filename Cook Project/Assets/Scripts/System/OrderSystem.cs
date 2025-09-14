using System.Collections.Generic;

public class OrderSystem : SimpleSingleton<OrderSystem>
{
    private List<Order> pendingOrders = new List<Order>();

    public void PlaceOrder(Order order)
    {
        pendingOrders.Add(order);
    }

    public bool ServeOrder(Order servedOrder)
    {
        var match = pendingOrders.Find(o => o.Equals(servedOrder));
        if (match != null)
        {
            pendingOrders.Remove(match);
            return true;
        }
        return false;
    }

    public List<Order> GetPendingOrders()
    {
        return new List<Order>(pendingOrders);
    }

    public bool CustomerHasPendingOrder(string customerName)
    {
        return pendingOrders.Exists(o => o.CustomerName == customerName);
    }
}