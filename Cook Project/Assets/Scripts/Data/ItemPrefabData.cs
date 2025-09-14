using UnityEngine;

[CreateAssetMenu(fileName = "ItemPrefabData", menuName = "ScriptableObjects/ItemPrefabData")]
public class ItemPrefabData : ScriptableObject
{
    public ItemBase[] itemPrefabs;
    public ItemBase GetItemByName(string itemName)
    {
        foreach (var item in itemPrefabs)
        {
            if (string.Equals(item.ItemName, itemName, System.StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }
}