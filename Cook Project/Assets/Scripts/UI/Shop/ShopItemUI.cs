using UnityEngine;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text itemNameText;
    [SerializeField] private TMPro.TMP_Text priceText;
    [SerializeField] private GameObject soldOutMask;
    [SerializeField] private UnityEngine.UI.Button purchaseButton;
    private string itemId;

    public void SetupUI(string itemId, string itemName, int price, int stock, System.Action<string> onPurchase)
    {
        this.itemId = itemId;
        itemNameText.text = itemName;
        priceText.text = $"$ {price}";
        soldOutMask.SetActive(stock <= 0);
        purchaseButton.interactable = stock > 0;
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() => onPurchase?.Invoke(itemId));
    }

    public void ResetUI()
    {
        itemNameText.text = "";
        priceText.text = "";
        soldOutMask.SetActive(false);
        purchaseButton.interactable = false;
        purchaseButton.onClick.RemoveAllListeners();
        itemId = null;
    }

}