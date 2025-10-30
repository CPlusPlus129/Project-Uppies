using System;
using System.Globalization;
using UnityEngine;

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
        public bool hasNameCardColor { get; private set; } = false;
        public Color nameCardColor { get; private set; } = Color.white;

        public CharacterSettingData(StringGridRow headerRow, StringGridRow row)
        {
            for (int i = 0; i < headerRow.Length; i++)
            {
                var header = headerRow.GetCell(i);
                if (string.IsNullOrEmpty(header))
                    continue;
                var value = row.GetCell(i);
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                value = value.Trim();
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
                    case "NameCardColor":
                        if (TryParseColor(value, out var parsedColor))
                        {
                            nameCardColor = parsedColor;
                            hasNameCardColor = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to parse NameCardColor '{value}' for character row: {row}");
                            hasNameCardColor = false;
                            nameCardColor = Color.white;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(value))
                return false;

            if (ColorUtility.TryParseHtmlString(value, out color))
            {
                return true;
            }

            var segments = value.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3 && segments.Length != 4)
            {
                return false;
            }

            var components = new float[4];
            var anyAboveOne = false;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (!float.TryParse(segment, NumberStyles.Float, CultureInfo.InvariantCulture, out components[i]))
                {
                    if (!float.TryParse(segment, NumberStyles.Float, CultureInfo.CurrentCulture, out components[i]))
                    {
                        return false;
                    }
                }

                if (components[i] > 1f)
                {
                    anyAboveOne = true;
                }
            }

            for (int i = 0; i < segments.Length; i++)
            {
                if (anyAboveOne)
                {
                    components[i] = Mathf.Clamp01(components[i] / 255f);
                }
                else
                {
                    components[i] = Mathf.Clamp01(components[i]);
                }
            }

            color = new Color(
                components[0],
                components[1],
                components[2],
                segments.Length > 3 ? components[3] : 1f);

            return true;
        }
    }

}
