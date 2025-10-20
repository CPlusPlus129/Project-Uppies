using System.IO;
using UnityEditor;
using UnityEngine;

namespace DialogueModule
{
    public class DialogueImporter
    {
        private const string SettingsPath = "Assets/DialogueModule/Settings/DialogueSettings.asset";

        [MenuItem("Tools/Update Dialogue Assets")]
        public static void UpdateDialogueAssets()
        {
            DialogueSettings settings = LoadSettings();
            if (settings == null)
            {
                Debug.LogError("DialogueSettings not found. Please create one at: " + SettingsPath);
                return;
            }

            if (settings.csvFolderPaths == null || settings.csvFolderPaths.Count == 0)
            {
                Debug.LogWarning("No CSV folder paths configured in DialogueSettings.");
                return;
            }

            ScenarioBook scenarioBook = LoadOrCreateScenarioBook(settings.outputPath);
            SettingsBook settingsBook = LoadOrCreateSettingsBook(settings.settingsBookOutputPath);
            StringGridDictionary allScenarios = new StringGridDictionary();
            StringGridDictionary allCharacters = new StringGridDictionary();
            StringGridDictionary allLayers = new StringGridDictionary();
            ScenarioFileReaderCsv reader = new ScenarioFileReaderCsv();

            int totalFiles = 0;
            int successFiles = 0;

            foreach (string folderPath in settings.csvFolderPaths)
            {
                string fullPath = Path.Combine(Application.dataPath, folderPath);

                if (!Directory.Exists(fullPath))
                {
                    Debug.LogWarning($"Folder not found: {fullPath}");
                    continue;
                }

                string[] csvFiles = Directory.GetFiles(fullPath, "*.csv", SearchOption.AllDirectories);

                foreach (string csvFile in csvFiles)
                {
                    totalFiles++;

                    if (reader.TryReadFile(csvFile, out StringGridDictionary gridDict))
                    {
                        foreach (string key in gridDict.Keys)
                        {
                            if (gridDict.TryGetValue(key, out StringGrid grid))
                            {
                                switch (key)
                                {
                                    case "Character":
                                        allCharacters.Add(key, grid);
                                        break;
                                    case "Layer":
                                        allLayers.Add(key, grid);
                                        break;
                                    default:
                                        var headerRow = grid.GetHeaderRow();
                                        if (headerRow != null && !headerRow.IsEmpty && !headerRow.IsCommentOut && headerRow.CheckIsScenarioHeaderRow())
                                            allScenarios.Add(key, grid);
                                        break;
                                }
                                successFiles++;
                            }
                        }
                    }
                }
            }

            scenarioBook.SetScenarioData(allScenarios);
            settingsBook.SetSettings("Character", allCharacters);
            settingsBook.SetSettings("Layer", allLayers);
            EditorUtility.SetDirty(scenarioBook);
            EditorUtility.SetDirty(settingsBook);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Dialogue assets updated successfully! Processed {successFiles}/{totalFiles} files. Scenarios: {scenarioBook.ScenarioCount}, Characters: {settingsBook.GetSettingsCount("Character")}, Layers: {settingsBook.GetSettingsCount("Layer")}");
        }

        private static DialogueSettings LoadSettings()
        {
            DialogueSettings settings = AssetDatabase.LoadAssetAtPath<DialogueSettings>(SettingsPath);

            if (settings == null)
            {
                // Try to find it anywhere in the project
                string[] guids = AssetDatabase.FindAssets("t:DialogueSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<DialogueSettings>(path);
                }
            }

            return settings;
        }

        private static ScenarioBook LoadOrCreateScenarioBook(string path)
        {
            ScenarioBook book = AssetDatabase.LoadAssetAtPath<ScenarioBook>(path);

            if (book == null)
            {
                book = ScriptableObject.CreateInstance<ScenarioBook>();

                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(book, path);
                AssetDatabase.SaveAssets();
            }

            return book;
        }

        private static SettingsBook LoadOrCreateSettingsBook(string path)
        {
            SettingsBook book = AssetDatabase.LoadAssetAtPath<SettingsBook>(path);

            if (book == null)
            {
                book = ScriptableObject.CreateInstance<SettingsBook>();

                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(book, path);
                AssetDatabase.SaveAssets();
            }

            return book;
        }

        [MenuItem("Tools/Create Dialogue Settings")]
        public static void CreateDialogueSettings()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            DialogueSettings settings = ScriptableObject.CreateInstance<DialogueSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;

            Debug.Log($"DialogueSettings created at: {SettingsPath}");
        }
    }
}
