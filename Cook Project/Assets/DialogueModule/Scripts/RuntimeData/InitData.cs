using System.Collections.Generic;

namespace DialogueModule
{
    /// <summary>
    /// This class is meant to be exposed to outside of the namespace for inits like UIs.
    /// </summary>
    public class InitData
    {
        public string DEFAULT_LAYER_NAME;
        public IReadOnlyList<LayerSettingData> layerSettingDatas;
    }
}