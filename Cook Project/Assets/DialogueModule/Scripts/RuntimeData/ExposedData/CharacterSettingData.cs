namespace DialogueModule
{
    public class CharacterSettingData
    {
        public string characterID;
        public string displayName;
        public float x;
        public float y;
        public float scale;
        public string fileName;
        public string voiceFileName;
        public float voiceSpeedMultiplier = 1f;

        public CharacterSettingData(StringGridRow headerRow, StringGridRow row)
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
                    case "CharacterName":
                        characterID = value;
                        break;
                    case "DisplayName":
                        displayName = value;
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
                        if (!float.TryParse(value, out scale))
                            throw new System.InvalidCastException($"Error from Character sheet: header {header}, cell {value}, row {row}");
                        break;
                    case "FileName":
                        fileName = value;
                        break;
                    case "VoiceFileName":
                        voiceFileName = value;
                        break;
                    case "VoiceSpeedMultiplier":
                        if (!float.TryParse(value, out voiceSpeedMultiplier))
                            throw new System.InvalidCastException($"Error from Character sheet: header {header}, cell {value}, row {row}");
                        if (voiceSpeedMultiplier <= 0f)
                            voiceSpeedMultiplier = 1f;
                        break;
                    default:
                        break;
                }
            }
        }
    }

}
