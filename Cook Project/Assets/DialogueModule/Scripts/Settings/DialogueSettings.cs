using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    [CreateAssetMenu(fileName = "DialogueSettings", menuName = "Dialogue/Dialogue Settings")]
    public class DialogueSettings : ScriptableObject
    {
        [Header("CSV Import Settings")]
        [Tooltip("List of folder paths relative to Assets folder (e.g., 'DialogueData/Scenarios')")]
        public List<string> csvFolderPaths = new List<string>();

        [Header("Output Settings")]
        [Tooltip("Where to save the generated ScenarioBook asset")]
        public string outputPath = "Assets/DialogueModule/Data/ScenarioBook.asset";

        [Tooltip("Where to save the generated SettingsBook asset")]
        public string settingsBookOutputPath = "Assets/DialogueModule/Data/SettingsBook.asset";

        [Header("CSV Parser Settings")]
        public char delimiter = ',';
        public System.Text.Encoding encoding = System.Text.Encoding.UTF8;
    }
}
