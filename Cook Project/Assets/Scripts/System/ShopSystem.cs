using R3;
using System.Collections.Generic;

public class ShopSystem : SimpleSingleton<ShopSystem>
{
    public Subject<IReadOnlyList<ShopItem>> OnShopItemsUpdated = new Subject<IReadOnlyList<ShopItem>>();
    private List<ShopItem> inStock = new List<ShopItem>();
    private ShopItem[] dummyCommodities = new ShopItem[]
    {
        new ShopItem { itemId = "item_001", price = 100, stock = 1 },
        new ShopItem { itemId = "item_002", price = 200, stock = 1 },
        new ShopItem { itemId = "item_003", price = 150, stock = 1 },
        new ShopItem { itemId = "item_004", price = 300, stock = 1 },
        new ShopItem { itemId = "item_005", price = 250, stock = 1 },
        new ShopItem { itemId = "item_006", price = 500, stock = 1 },
        new ShopItem { itemId = "item_007", price = 1000, stock = 1 },
        new ShopItem { itemId = "item_008", price = 1234567890, stock = 1 },
        new ShopItem { itemId = "item_009", price = 1, stock = 1 },
    };

    public IReadOnlyList<ShopItem> GetShopItems() => inStock;

    public void RefreshShopItems()
    {
        //placeholder
        inStock.Clear();
        var picked = RandomHelper.PickWithoutReplacement(dummyCommodities, 6);
        foreach (var item in picked)
        {
            inStock.Add(item);
        }
        OnShopItemsUpdated.OnNext(inStock);
    }

    public bool PurchaseItem(string itemId)
    {
        var item = inStock.Find(i => i.itemId == itemId);
        if (item != null && item.stock > 0)
        {
            if(item.price > PlayerStatSystem.Instance.Money.Value)
                return false;

            PlayerStatSystem.Instance.Money.Value -= item.price;
            item.stock--;
            OnShopItemsUpdated.OnNext(inStock);
            return true;
        }
        return false;
    }
}

public class ShopItem
{
    public string itemId;
    public int price;
    public int stock;
}