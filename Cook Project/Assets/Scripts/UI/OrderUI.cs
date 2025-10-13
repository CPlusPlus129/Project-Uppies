using UnityEngine;
using R3;
using System.Collections.Generic;
using System.Linq;

public class OrderUI : MonoBehaviour
{
    public Transform listRoot;
    public OrderListItem orderListItemPrefab;
    private IOrderManager orderManager;
    private List<OrderListItem> itemList = new List<OrderListItem>();

    private async void Awake()
    {
        orderListItemPrefab.gameObject.SetActive(false);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        orderManager.OnNewOrder.Subscribe(OnNewOrder).AddTo(this);
        orderManager.OnOrderServed.Subscribe(OnOrderServed).AddTo(this);
        orderManager.OnOrdersCleared.Subscribe(OnOrdersCleared).AddTo(this);
    }

    private void OnNewOrder(Order order)
    {
        var item = Instantiate(orderListItemPrefab, listRoot);
        item.gameObject.SetActive(true);
        item.SetupUI(order, order.CustomerName, order.MealName);
        itemList.Add(item);
    }

    private void OnOrderServed(Order order)
    {
        var item = itemList.FirstOrDefault(x => x.order.Equals(order));
        if (item != null)
        {
            itemList.Remove(item);
            Destroy(item.gameObject);
        }
    }

    private void OnOrdersCleared(Unit _)
    {
        foreach (var item in itemList)
        {
            Destroy(item.gameObject);
        }
        itemList.Clear();
    }
}