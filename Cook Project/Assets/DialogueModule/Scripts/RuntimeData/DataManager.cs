using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    class DataManager : MonoBehaviour
    {
        [SerializeField] private SettingsBook settingsBook;
        [SerializeField] private ScenarioBook scenarioBook;

        internal SettingDataManager settingDataManager = new SettingDataManager();
        DialogueTagParser dialogueTagParser = new DialogueTagParser();
        Dictionary<string, ScenarioData> scenarioDataDict = new Dictionary<string, ScenarioData>();

        public void Init()
        {
            settingDataManager.Init(settingsBook);
            dialogueTagParser.Init();
            InitScenarios();
        }

        private void InitScenarios()
        {
            scenarioDataDict.Clear();
            foreach (var kvp in scenarioBook.ScenarioData.ToDictionary())
            {
                var grid = kvp.Value;
                grid.Name = kvp.Key;
                var scenarioData = new ScenarioData(grid);
                scenarioDataDict[grid.Name] = scenarioData;
            }
        }

        public LabelData GetLabelData(string label)
        {
            foreach (var sData in scenarioDataDict.Values)
            {
                if (sData.ScenarioLabelDict.TryGetValue(label, out var labelData))
                {
                    return labelData;
                }
            }

            return null;
        }

        public string ParseDialogueText(string content)
        {
            return dialogueTagParser.Parse(this, content);
        }
    }
}