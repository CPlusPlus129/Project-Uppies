namespace DialogueModule 
{
    public class LayerSettingData
    {
        public string layerName;
        public string type;
        public float x;
        public float y;
        public int order;

        public LayerSettingData(StringGridRow headerRow, StringGridRow row)
        {
            for (int i = 0; i < headerRow.Length; i++)
            {
                var header = headerRow.GetCell(i);
                if (string.IsNullOrEmpty(header))
                    continue;
                var value = row.GetCell(i);
                if (string.IsNullOrEmpty(value))
                    continue;
                switch (header)
                {
                    case "LayerName":
                        layerName = value;
                        break;
                    case "Type":
                        type = value;
                        break;
                    case "X":
                        if (!float.TryParse(value, out x))
                            throw new System.InvalidCastException($"Error from Character sheet: header {header}, cell {value}, row {row}");
                        break;
                    case "Y":
                        if (!float.TryParse(value, out y))
                            throw new System.InvalidCastException($"Error from Character sheet: header {header}, cell {value}, row {row}");
                        break;
                    case "Scale":
                        if (!int.TryParse(value, out order))
                            throw new System.InvalidCastException($"Error from Character sheet: header {header}, cell {value}, row {row}");
                        break;
                    default:
                        break;
                }
            }
        }

        internal LayerSettingData() { }
    }
}