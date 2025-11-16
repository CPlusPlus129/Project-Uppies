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
		readonly Dictionary<string, LabelData> runtimeLabelDict = new Dictionary<string, LabelData>();

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
			if (!string.IsNullOrWhiteSpace(label) && runtimeLabelDict.TryGetValue(label, out var runtimeLabel))
			{
				return runtimeLabel;
			}

			foreach (var sData in scenarioDataDict.Values)
			{
				if (sData.ScenarioLabelDict.TryGetValue(label, out var labelData))
				{
					return labelData;
				}
			}

			return null;
		}

		public void RegisterRuntimeLabel(LabelData labelData)
		{
			if (labelData == null || string.IsNullOrWhiteSpace(labelData.name))
			{
				return;
			}

			runtimeLabelDict[labelData.name] = labelData;
		}

		public void UnregisterRuntimeLabel(string label)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return;
			}

			runtimeLabelDict.Remove(label);
		}

        public string ParseDialogueText(string content)
        {
            return dialogueTagParser.Parse(this, content);
        }
    }
}
