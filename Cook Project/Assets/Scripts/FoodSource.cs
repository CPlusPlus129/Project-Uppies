using Cysharp.Threading.Tasks;
using UnityEngine;

public class FoodSource : InteractableBase
{
    public string ItemName;
    public TMPro.TMP_Text ItemNameText;
    
    private FridgeGlowController glowController;
    private IFridgeGlowManager glowManager;

    protected override void Awake()
    {
        base.Awake();
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        if (ItemNameText != null && !string.IsNullOrEmpty(ItemName))
        {
            ItemNameText.text = ItemName;
        }
        
        glowController = GetComponent<FridgeGlowController>();
        if (glowController == null)
        {
            glowController = gameObject.AddComponent<FridgeGlowController>();
        }

        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        glowManager = await ServiceLocator.Instance.GetAsync<IFridgeGlowManager>();
        glowManager?.RegisterFoodSource(this);
    }

    private void OnEnable()
    {
        glowManager?.RegisterFoodSource(this);
    }

    private void OnDisable()
    {
        glowManager?.UnregisterFoodSource(this);
    }

    public void SetItemName(string name)
    {
        ItemName = name;
        if (ItemNameText != null)
        {
            ItemNameText.text = ItemName;
        }
        
        if (glowManager != null)
        {
            glowManager.RefreshGlowStates();
        }
    }

    public override async void Interact()
    {
        // Get inventory system
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        var inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();

        // Check if inventory is full
        if (inventorySystem.IsInventoryFull())
        {
            Debug.Log("Inventory is full!");
            return;
        }

        // Get item prefab from database
        var itemPrefab = Database.Instance.itemPrefabData.GetItemByName(ItemName);
        var foodObj = itemPrefab != null ? Instantiate(itemPrefab) : null;

        if (foodObj != null)
        {
            // Try to add to inventory
            var wasAdded = inventorySystem.AddItem(foodObj);
            if (wasAdded)
            {
                foodObj.gameObject.SetActive(false);
            }
            else
            {
                Destroy(foodObj.gameObject);
            }
        }
    }
    
    public void StartGlowing()
    {
        if (glowController != null)
        {
            glowController.EnableGlow();
        }
    }
    
    public void StopGlowing()
    {
        if (glowController != null)
        {
            glowController.DisableGlow();
        }
    }

    public void EnableGlow()
    {
        if (glowController != null)
        {
            glowController.EnableGlow();
        }
    }

    public void DisableGlow()
    {
        if (glowController != null)
        {
            glowController.DisableGlow();
        }
    }

    public void EnableGlowForDuration(float seconds)
    {
        if (glowController != null)
        {
            glowController.EnableGlowForDuration(seconds);
        }
    }
    
    public bool ContainsIngredient(string ingredientName)
    {
        return !string.IsNullOrEmpty(ItemName) && 
               ItemName.Equals(ingredientName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    private void OnDestroy()
    {
        glowManager?.UnregisterFoodSource(this);
    }
}
