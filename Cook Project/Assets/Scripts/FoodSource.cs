using UnityEngine;

public class FoodSource : MonoBehaviour, IInteractable
{
    public string ItemName;
    public TMPro.TMP_Text ItemNameText;

    private void Awake()
    {
        ItemNameText.text = ItemName;
    }

    public void Interact()
    {
        
    }
}