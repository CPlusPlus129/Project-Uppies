using UnityEngine;

[CreateAssetMenu(fileName = "ItemPrefabData", menuName = "ScriptableObjects/ItemPrefabData")]
public class ItemPrefabData : ScriptableObject
{
    public ItemBase[] itemPrefabs;
    public ItemBase GetItemByName(string itemName)
    {
        foreach (var item in itemPrefabs)
        {
            if (item.ItemName == itemName)
            {
                return item;
            }
        }
        return null;
    }
}