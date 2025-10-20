using System.Collections.Generic;

namespace DialogueModule
{
    public class LayerSettings
    {
        public const string DEFAULT_LAYER_NAME = "";
        internal IReadOnlyDictionary<string, LayerSettingData> DataDict => dataDict;
        Dictionary<string, LayerSettingData> dataDict = new Dictionary<string, LayerSettingData>();

        public void Init(StringGridDictionary settings)
        {
            dataDict.Clear();
            var defaultLayerData = new LayerSettingData() 
            {
                layerName = DEFAULT_LAYER_NAME,
                type = "Character",
                x = 0,
                y = 0,
                order = 0,
            };
            dataDict.Add(string.Empty, defaultLayerData); //use string empty as key
            foreach (var grid in settings.Values)
            {
                var headerRow = grid.GetRow(0);
                if (headerRow == null)
                    throw new System.Exception($"header row is null for grid {grid.Name}");
                for (int i = 1; i < grid.RowCount; i++)
                {
                    var row = grid.GetRow(i);
                    if (row.IsEmpty || row.IsCommentOut)
                        continue;
                    var d = new LayerSettingData(headerRow, row);
                    dataDict.Add(d.layerName, d);
                }
            }
        }

    }
}