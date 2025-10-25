using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for StatUpgradeConfig to make it easier to manage upgrades
/// </summary>
[CustomEditor(typeof(StatUpgradeConfig))]
public class StatUpgradeConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        StatUpgradeConfig config = (StatUpgradeConfig)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create New Upgrade Template"))
        {
            CreateUpgradeTemplate(config);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Tip: Each upgrade should have a unique ID. The system tracks purchase counts to respect max purchases.", 
            MessageType.Info
        );
    }
    
    private void CreateUpgradeTemplate(StatUpgradeConfig config)
    {
        // This is a placeholder that shows the structure needed
        Debug.Log("To add a new upgrade:");
        Debug.Log("1. Increase the 'Available Upgrades' array size");
        Debug.Log("2. Fill in the fields:");
        Debug.Log("   - Upgrade Id: unique identifier (e.g., 'hp_boost_1')");
        Debug.Log("   - Upgrade Name: display name (e.g., 'Health Boost I')");
        Debug.Log("   - Description: what the upgrade does");
        Debug.Log("   - Stat Type: which stat to upgrade");
        Debug.Log("   - Upgrade Value: how much to increase (positive for increases, negative for decreases)");
        Debug.Log("   - Is Percentage: whether the value is a percentage or flat amount");
        Debug.Log("   - Price: cost in money");
        Debug.Log("   - Max Purchases: how many times this can be bought");
    }
}
