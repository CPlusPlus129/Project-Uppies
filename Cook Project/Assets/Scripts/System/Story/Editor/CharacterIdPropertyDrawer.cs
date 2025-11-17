using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CharacterIdAttribute))]
internal sealed class CharacterIdPropertyDrawer : PropertyDrawer
{
    private class CharacterCsvRow
    {
        [Column("CharacterName")] public string characterId;
        [Column("DisplayName")] public string displayName;
    }

    private static GUIContent[] cachedOptions;
    private static string[] cachedIds;
    private static double lastRefreshTime;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();
        EditorGUI.BeginProperty(position, label, property);

        if (cachedOptions == null || cachedOptions.Length == 0)
        {
            EditorGUI.HelpBox(position, "Character.csv not found or empty.", MessageType.Warning);
            EditorGUI.EndProperty();
            return;
        }

        var currentIndex = Array.IndexOf(cachedIds, property.stringValue);
        currentIndex = currentIndex < 0 ? 0 : currentIndex;
        var nextIndex = EditorGUI.Popup(position, label, currentIndex, cachedOptions);
        property.stringValue = cachedIds[nextIndex];

        EditorGUI.EndProperty();
    }

    private static void EnsureCache()
    {
        if (cachedOptions != null && EditorApplication.timeSinceStartup - lastRefreshTime < 5f)
        {
            return;
        }

        lastRefreshTime = EditorApplication.timeSinceStartup;
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CharacterIdAttribute.CharacterCsvAssetPath);
        if (asset == null)
        {
            cachedOptions = Array.Empty<GUIContent>();
            cachedIds = Array.Empty<string>();
            return;
        }

        var rows = CsvReader.ReadFromString<CharacterCsvRow>(asset.text);
        rows.RemoveAll(r => string.IsNullOrWhiteSpace(r.characterId));
        cachedIds = new string[rows.Count];
        cachedOptions = new GUIContent[rows.Count];

        for (int i = 0; i < rows.Count; i++)
        {
            var id = rows[i].characterId?.Trim();
            var display = rows[i].displayName;
            cachedIds[i] = id;
            cachedOptions[i] = new GUIContent(string.IsNullOrWhiteSpace(display) ? id : $"{display} ({id})");
        }
    }
}
