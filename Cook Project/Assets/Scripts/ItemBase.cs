using UnityEngine;

public abstract class ItemBase : MonoBehaviour
{
    public string ItemName;
    public bool EqualsItemName(string itemName) => string.Equals(this.ItemName, itemName, System.StringComparison.InvariantCultureIgnoreCase);
}