using System.Collections.Generic;

namespace DialogueModule
{
    class CharacterSettings
    {
        internal IReadOnlyDictionary<string, CharacterSettingData> DataDict => dataDict;
        Dictionary<string, CharacterSettingData> dataDict = new Dictionary<string, CharacterSettingData>();

        public void Init(StringGridDictionary characterSettings)
        {
            dataDict.Clear();
            foreach (var grid in characterSettings.Values)
            {
                var headerRow = grid.GetRow(0);
                if (headerRow == null)
                    throw new System.Exception($"header row is null for grid {grid.Name}");
                for (int i = 1; i < grid.RowCount; i++)
                {
                    var row = grid.GetRow(i);
                    if(row.IsEmpty || row.IsCommentOut)
                        continue;
                    var d = new CharacterSettingData(headerRow, row);
                    dataDict.Add(d.characterID, d);
                }
            }
        }

    }
}