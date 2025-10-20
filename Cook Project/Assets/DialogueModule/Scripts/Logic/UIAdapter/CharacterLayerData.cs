using UnityEngine;

namespace DialogueModule
{
    public class CharacterLayerData
    {
        public string LayerName { get; set; }
        public CharacterSettingData SettingData { get; set; }
        public Sprite Sprite { get; set; }

        public string Expression { get; set; }
        public Vector2 Position { get; set; }
        public float Alpha { get; set; } = 1f;

        public CharacterLayerData(string layerName, CharacterSettingData settingData, Sprite sprite)
        {
            LayerName = layerName;
            SettingData = settingData;
            Sprite = sprite;
            Position = new Vector2(settingData.x, settingData.y);
        }

        public CharacterLayerData Clone()
        {
            return new CharacterLayerData(LayerName, SettingData, Sprite)
            {
                Expression = Expression,
                Position = Position,
                Alpha = Alpha
            };
        }
    }
}