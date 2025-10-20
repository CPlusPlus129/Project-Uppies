using UnityEngine;

namespace DialogueModule
{
    class LayerItem : MonoBehaviour
    {
        public LayerSettingData data { get; set; }
        public int sortingOrder => (data?.order).GetValueOrDefault();

        public void Init(LayerSettingData data)
        {
            this.data = data;
            gameObject.name = data.layerName;
            var rectT = GetComponent<RectTransform>();
            rectT.anchoredPosition = new Vector2(data.x, data.y);
        }

        public CharacterObject GetCharacterObject(string characterId) //too coupled
        {
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent<CharacterObject>(out var obj))
                    continue;
                if(obj.characterLayerData?.SettingData.characterID == characterId)
                {
                    return obj;
                }
            }
            return null;
        }
    }
}