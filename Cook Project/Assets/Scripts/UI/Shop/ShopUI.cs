using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemUI itemPrefab;
    private List<ShopItemUI> itemList = new List<ShopItemUI>();

    private void Awake()
    {
        itemPrefab.gameObject.SetActive(false);
        closeButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(this);
        ShopSystem.Instance.OnShopItemsUpdated.Subscribe(_ => RefreshShopItems()).AddTo(this);
    }

    public void OnEnable()
    {
        RefreshShopItems();
        InputManager.Instance.PushActionMap("UI");
    }

    public void OnDisable()
    {
        InputManager.Instance.PopActionMap("UI");
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void RefreshShopItems()
    {
        ResetShopItems();
        var list = ShopSystem.Instance.GetShopItems();
        for (int i = 0; i < list.Count; i++)
        {
            var itemData = list[i];
            ShopItemUI uiItem = null;
            if (i >= itemList.Count)
            {
                uiItem = Instantiate(itemPrefab, itemsContainer);
                itemList.Add(uiItem);
            }
            else
            {
                uiItem = itemList[i];
            }
            uiItem.gameObject.SetActive(true);
            uiItem.SetupUI(itemData.itemId, itemData.itemId, itemData.price, itemData.stock, itemId =>
            {
                if (ShopSystem.Instance.PurchaseItem(itemId))
                {
                    Debug.Log($"Purchased item: {itemId}");
                    RefreshShopItems();
                }
                else
                {
                    Debug.Log($"Failed to purchase item: {itemId}");
                }
            });
        }
        if(itemList.Count > list.Count)
        {
            for(int j = list.Count; j < itemList.Count; j++)
            {
                itemList[j].gameObject.SetActive(false);
            }
        }
    }

    private void ResetShopItems()
    {
        foreach (var item in itemList)
        {
            item.ResetUI();
            item.gameObject.SetActive(false);
        }
    }
}