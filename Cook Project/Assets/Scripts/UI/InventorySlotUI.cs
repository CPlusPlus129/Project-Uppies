using UnityEngine;

public class InventorySlotUI : MonoBehaviour
{
    public GameObject itemDisplay;
    public TMPro.TextMeshProUGUI item; // use text just for demo

    public void SetItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            itemDisplay.SetActive(false);
            item.text = "";
        }
        else
        {
            itemDisplay.SetActive(true);
            item.text = itemName;
        }
    }
}