using UnityEngine;
using UnityEngine.UI;

namespace DialogueModule
{
    class CharacterObject : MonoBehaviour
    {
        [SerializeField] Image image;
        public CharacterLayerData characterLayerData { get; private set; }

        public void Setup(CharacterLayerData characterLayerData)
        {
            this.characterLayerData = characterLayerData;
            gameObject.name = characterLayerData.SettingData.characterID;
            image.sprite = characterLayerData.Sprite;
            image.SetNativeSize();
            image.color = new Color(image.color.r, image.color.g, image.color.b, characterLayerData.Alpha);
            var rectT = GetComponent<RectTransform>();
            rectT.anchoredPosition = characterLayerData.Position;
            var scale = characterLayerData.SettingData.scale == 0 ? 1 : characterLayerData.SettingData.scale;
            rectT.localScale = new Vector3(scale, scale, scale);
        }

        public void Reset()
        {
            characterLayerData = null;
            image.sprite = null;
        }
    }
}