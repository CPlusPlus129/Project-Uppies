using R3;
using System.Collections.Generic;

public interface IOrderManager : IGameService
{
	Subject<Order> OnNewOrder { get; }
	Subject<Order> OnOrderServed { get; }
	Subject<Unit> OnOrdersCleared { get; }
    Subject<OrderRewardResult> OnOrderRewarded { get; }
    IReadOnlyList<Order> pendingOrders { get; }
	void PlaceOrder(Order order);
	bool ServeOrder(Order servedOrder, Meal servedMeal);
	void ClearOrders();
	bool CustomerHasPendingOrder(string customerName);
	Order GetPendingOrderForCustomer(string customerName);
}
