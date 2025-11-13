using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] private TMPro.TMP_Text itemNameText;
    [SerializeField] private TMPro.TMP_Text priceText;
    [SerializeField] private Image iconImage;
    
    [Header("Upgrade Info")]
    [SerializeField] private TMPro.TMP_Text descriptionText;
    [SerializeField] private TMPro.TMP_Text statEffectText;
    [SerializeField] private TMPro.TMP_Text stockText;
    
    [Header("Purchase")]
    [SerializeField] private GameObject soldOutMask;
    [SerializeField] private Button purchaseButton;
    
    private string itemId;

    public void SetupUI(string itemId, string itemName, int price, int stock, System.Action<string> onPurchase)
    {
        this.itemId = itemId;
        itemNameText.text = itemName;
        priceText.text = $"$ {price}";
        
        // Update stock display
        if (stockText != null)
        {
            stockText.text = stock > 1 ? $"Stock: {stock}" : "";
        }
        
        soldOutMask.SetActive(stock <= 0);
        purchaseButton.interactable = stock > 0 && PlayerStatSystem.Instance.Money.Value >= price;
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() => onPurchase?.Invoke(itemId));
    }

    /// <summary>
    /// Enhanced setup that includes upgrade data for displaying description and effects
    /// </summary>
    public void SetupUI(string itemId, StatUpgradeData upgradeData, int price, int stock, System.Action<string> onPurchase)
    {
        this.itemId = itemId;
        
        // Set basic info
        if (itemNameText != null)
        {
            itemNameText.text = upgradeData != null ? upgradeData.upgradeName : itemId;
        }
        
        if (priceText != null)
        {
            priceText.text = $"$ {price}";
        }
        
        // Set icon if available
        if (iconImage != null && upgradeData != null && upgradeData.icon != null)
        {
            iconImage.sprite = upgradeData.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
        
        // Set description
        if (descriptionText != null && upgradeData != null)
        {
            descriptionText.text = upgradeData.description;
        }
        else if (descriptionText != null)
        {
            descriptionText.text = "";
        }
        
        // Set stat effect
        if (statEffectText != null && upgradeData != null)
        {
            statEffectText.text = upgradeData.GetStatEffectString();
        }
        else if (statEffectText != null)
        {
            statEffectText.text = "";
        }
        
        // Update stock display
        if (stockText != null)
        {
            stockText.text = stock > 1 ? $"Stock: {stock}" : "";
        }
        
        // Update purchase button
        bool canAfford = PlayerStatSystem.Instance.Money.Value >= price;
        soldOutMask.SetActive(stock <= 0);
        purchaseButton.interactable = stock > 0 && canAfford;
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() => onPurchase?.Invoke(itemId));
    }

    public void SetupAbilityUnlockUI(string itemId, PlayerSoulAbilityManager.AbilityShopEntry abilityData, int price, int stock, System.Action<string> onPurchase)
    {
        this.itemId = itemId;

        if (itemNameText != null)
        {
            itemNameText.text = string.IsNullOrWhiteSpace(abilityData.DisplayName) ? itemId : abilityData.DisplayName;
        }

        if (priceText != null)
        {
            priceText.text = $"$ {price}";
        }

        if (iconImage != null)
        {
            if (abilityData.Icon != null)
            {
                iconImage.sprite = abilityData.Icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(abilityData.Description) ? "Unlocks a new soul ability." : abilityData.Description;
        }

        if (statEffectText != null)
        {
            statEffectText.text = "Unlocks Soul Ability";
        }

        if (stockText != null)
        {
            stockText.text = stock > 1 ? $"Stock: {stock}" : "Single Purchase";
        }

        bool canAfford = PlayerStatSystem.Instance.Money.Value >= price;
        soldOutMask.SetActive(stock <= 0);
        purchaseButton.interactable = stock > 0 && canAfford;
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() => onPurchase?.Invoke(itemId));
    }

    public void ResetUI()
    {
        if (itemNameText != null) itemNameText.text = "";
        if (priceText != null) priceText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (statEffectText != null) statEffectText.text = "";
        if (stockText != null) stockText.text = "";
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }
        
        soldOutMask.SetActive(false);
        purchaseButton.interactable = false;
        purchaseButton.onClick.RemoveAllListeners();
        itemId = null;
    }
}
