using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemUI itemPrefab;
    [SerializeField] private UIAnimationController uiAnim;
    private List<ShopItemUI> itemList = new List<ShopItemUI>();

    public async UniTask Init()
    {
        itemPrefab.gameObject.SetActive(false);
        closeButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(this);
        ShopSystem.Instance.OnShopItemsUpdated.Subscribe(_ => RefreshShopItems()).AddTo(this);
        InputSystem.actions.FindActionMap("Shop").FindAction("Esc").performed += ctx => Close();
        uiAnim.OnCloseComplete.Subscribe(_ => OnPanelCloseComplete()).AddTo(this);
        await UniTask.CompletedTask;
    }

    public void Open()
    {
        if (!ShopSystem.Instance.IsStoreOpen)
        {
            WorldBroadcastSystem.Instance?.Broadcast("The shop is closed right now.", 4f);
            return;
        }
        gameObject.SetActive(true);
        uiAnim.Open();
        RefreshShopItems();
        InputManager.Instance.PushActionMap("Shop");
    }

    public void Close()
    {
        uiAnim.Close();
        InputManager.Instance.PopActionMap("Shop");
    }

    private void OnPanelCloseComplete()
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
            
            // Create new UI item if needed
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
            
            // Use the enhanced setup method if upgrade data is available
            if (itemData.isAbilityUnlock)
            {
                uiItem.SetupAbilityUnlockUI(
                    itemData.itemId,
                    itemData.abilityUnlockData,
                    itemData.price,
                    itemData.stock,
                    OnPurchaseItem);
            }
            else if (itemData.upgradeData != null)
            {
                uiItem.SetupUI(
                    itemData.itemId, 
                    itemData.upgradeData, 
                    itemData.price, 
                    itemData.stock, 
                    OnPurchaseItem
                );
            }
            else
            {
                // Fallback to basic setup
                uiItem.SetupUI(
                    itemData.itemId, 
                    itemData.itemId, 
                    itemData.price, 
                    itemData.stock, 
                    OnPurchaseItem
                );
            }
        }
        
        // Hide excess UI items
        if (itemList.Count > list.Count)
        {
            for (int j = list.Count; j < itemList.Count; j++)
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

    private void OnPurchaseItem(string itemId)
    {
        if (ShopSystem.Instance.PurchaseItem(itemId))
        {
            Debug.Log($"Successfully purchased: {itemId}");
            RefreshShopItems();
        }
        else
        {
            Debug.Log($"Failed to purchase: {itemId}");
        }
    }
}
