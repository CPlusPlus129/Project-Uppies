using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DialogueModule
{
    public static class DialogueEventAssetGenerator
    {
        private const string DefaultSettingsPath = "Assets/DialogueModule/Settings/DialogueSettings.asset";
        private const string OutputRoot = "Assets/Events/Generated";

        [MenuItem("Tools/Generate Dialogue Event Assets")]
        public static void GenerateDialogueEventAssets()
        {
            var settings = LoadSettings(DefaultSettingsPath);
            if (settings == null)
            {
                Debug.LogError($"DialogueSettings not found at {DefaultSettingsPath}. Create one via Tools ▸ Create Dialogue Settings.");
                return;
            }

            if (settings.csvFolderPaths == null || settings.csvFolderPaths.Count == 0)
            {
                Debug.LogWarning("DialogueSettings.csvFolderPaths is empty. Nothing to generate.");
                return;
            }

            EnsureFolder(OutputRoot);

            var reader = new ScenarioFileReaderCsv();
            var createdAssets = new List<string>();
            var updatedAssets = new List<string>();
            var untouchedAssets = new List<string>();

            foreach (var relativeFolder in settings.csvFolderPaths)
            {
                if (string.IsNullOrWhiteSpace(relativeFolder))
                {
                    continue;
                }

                var absoluteFolder = Path.Combine(Application.dataPath, relativeFolder);
                if (!Directory.Exists(absoluteFolder))
                {
                    Debug.LogWarning($"Dialogue CSV folder not found: {absoluteFolder}");
                    continue;
                }

                var csvFiles = Directory.GetFiles(absoluteFolder, "*.csv", SearchOption.AllDirectories);
                foreach (var csvFile in csvFiles)
                {
                    if (!reader.TryReadFile(csvFile, out var dict) || dict.Count == 0)
                    {
                        Debug.LogWarning($"Skipped CSV (parse failed or empty): {csvFile}");
                        continue;
                    }

                    foreach (var grid in dict.Values)
                    {
                        GenerateAssetsForGrid(grid, csvFile, createdAssets, updatedAssets, untouchedAssets);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Dialogue event generation complete. Created {createdAssets.Count}, updated {updatedAssets.Count}, left unchanged {untouchedAssets.Count} pre-existing assets.");
        }

        private static void GenerateAssetsForGrid(
            StringGrid grid,
            string csvFilePath,
            List<string> created,
            List<string> updated,
            List<string> untouched)
        {
            var csvName = grid?.Name ?? Path.GetFileNameWithoutExtension(csvFilePath);
            if (string.IsNullOrEmpty(csvName))
            {
                csvName = "Dialogue";
            }

            var outputFolder = $"{OutputRoot}/{csvName}";
            EnsureFolder(outputFolder);

            var labels = ExtractScenarioLabels(grid).ToList();
            if (labels.Count == 0)
            {
                Debug.LogWarning($"No dialogue labels found in CSV '{csvFilePath}'.");
                return;
            }

            var existingAssets = AssetDatabase.FindAssets("t:DialogueModule.DialogueEventAsset", new[] { outputFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                var assetName = SanitizeFileName(label);
                var assetPath = $"{outputFolder}/{assetName}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<DialogueEventAsset>(assetPath);
                var isNew = asset == null;

                if (isNew)
                {
                    asset = ScriptableObject.CreateInstance<DialogueEventAsset>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created.Add(assetPath);
                }

                var so = new SerializedObject(asset);
                bool dirty = false;

                var labelProp = so.FindProperty("label");
                if (labelProp != null && labelProp.stringValue != label)
                {
                    labelProp.stringValue = label;
                    dirty = true;
                }

                if (isNew)
                {
                    var notesProp = so.FindProperty("notes");
                    if (notesProp != null)
                    {
                        notesProp.stringValue = $"Auto-generated from {csvName}.csv";
                        dirty = true;
                    }

                    var hintProp = so.FindProperty("hint");
                    if (hintProp != null)
                    {
                        hintProp.stringValue = string.Empty;
                        dirty = true;
                    }

                    var playOnceProp = so.FindProperty("playOnce");
                    if (playOnceProp != null)
                    {
                        playOnceProp.boolValue = true;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                    if (!isNew)
                    {
                        updated.Add(assetPath);
                    }
                }

                asset.name = assetName;
                existingAssets.Remove(assetPath);
            }

            untouched.AddRange(existingAssets);
        }

        private static IEnumerable<string> ExtractScenarioLabels(StringGrid grid)
        {
            if (grid == null)
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in grid.Rows)
            {
                if (row == null || row.IsEmpty || row.IsCommentOut)
                {
                    continue;
                }

                var raw = row.GetCell(0);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                raw = raw.Trim();
                if (!raw.StartsWith("*", StringComparison.Ordinal))
                {
                    continue;
                }

                var label = raw.TrimStart('*').Trim();
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                if (string.Equals(label, "EndScenario", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seen.Add(label))
                {
                    yield return label;
                }
            }
        }

        private static string SanitizeFileName(string label)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = label.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
            return new string(chars);
        }

        private static DialogueSettings LoadSettings(string defaultPath)
        {
            var settings = AssetDatabase.LoadAssetAtPath<DialogueSettings>(defaultPath);
            if (settings != null)
            {
                return settings;
            }

            var guids = AssetDatabase.FindAssets("t:DialogueModule.DialogueSettings");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<DialogueSettings>(path);
            }

            return null;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            var current = parts[0];
            if (!string.Equals(current, "Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Path must be under Assets: {assetPath}");
            }

            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
