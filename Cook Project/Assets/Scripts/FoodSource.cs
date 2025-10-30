using Cysharp.Threading.Tasks;
using UnityEngine;

public class FoodSource : MonoBehaviour, IInteractable
{
    public string ItemName;
    public TMPro.TMP_Text ItemNameText;
    
    private FridgeGlowController glowController;
    private IFridgeGlowManager glowManager;

    private async void Awake()
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

        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
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

    public void Interact()
    {
        // Interaction logic here
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
