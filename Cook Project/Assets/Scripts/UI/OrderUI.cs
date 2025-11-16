using UnityEngine;
using R3;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class OrderUI : MonoBehaviour, IUIInitializable
{
    public Transform listRoot;
    public OrderListItem orderListItemPrefab;
    private IOrderManager orderManager;
    private List<OrderListItem> itemList = new List<OrderListItem>();

    public async UniTask Init()
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
        item.SetupUI(order);
        item.Enter();
        itemList.Add(item);
    }

    private void OnOrderServed(Order order)
    {
        var item = itemList.FirstOrDefault(x => x.order.Equals(order));
        if (item != null)
        {
            itemList.Remove(item);
            item.Exit();
            Destroy(item.gameObject, 1f);
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