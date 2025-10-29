using DialogueModule;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueTriggerAuthoring))]
public class DialogueTriggerAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Configure Box Trigger"))
        {
            foreach (var t in targets)
            {
                ConfigureCollider((DialogueTriggerAuthoring)t);
            }
        }

        if (GUILayout.Button("Fit Collider To Children"))
        {
            foreach (var t in targets)
            {
                FitColliderToRenderers((DialogueTriggerAuthoring)t);
            }
        }

        if (GUILayout.Button("Create Dialogue Event Asset…"))
        {
            CreateDialogueEventAsset((DialogueTriggerAuthoring)target);
        }
    }

    private static void ConfigureCollider(DialogueTriggerAuthoring trigger)
    {
        Undo.RegisterCompleteObjectUndo(trigger.gameObject, "Configure Box Trigger");

        var collider = trigger.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(trigger.gameObject);
        }

        collider.isTrigger = true;
        EditorUtility.SetDirty(collider);
    }

    private static void FitColliderToRenderers(DialogueTriggerAuthoring trigger)
    {
        var collider = trigger.GetComponent<BoxCollider>();
        if (collider == null)
        {
            Debug.LogWarning($"{trigger.name}: Fit Collider requires a BoxCollider.");
            return;
        }

        var renderers = trigger.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"{trigger.name}: No renderers found to fit collider to.");
            return;
        }

        var worldToLocal = trigger.transform.worldToLocalMatrix;
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (var renderer in renderers)
        {
            var bounds = renderer.bounds;
            foreach (var corner in GetCorners(bounds))
            {
                var local = worldToLocal.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }

        Undo.RecordObject(collider, "Fit Collider To Renderers");
        collider.center = (min + max) * 0.5f;
        collider.size = max - min;
        EditorUtility.SetDirty(collider);
    }

    private static void CreateDialogueEventAsset(DialogueTriggerAuthoring trigger)
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "Create Dialogue Event Asset",
            $"{trigger.gameObject.name}_DialogueEvent.asset",
            "asset",
            "Choose a location for the new DialogueEventAsset.");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var asset = ScriptableObject.CreateInstance<DialogueEventAsset>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var player = trigger.GetComponent<DialogueEventPlayer>();
        if (player == null)
        {
            return;
        }

        var serializedPlayer = new SerializedObject(player);
        serializedPlayer.FindProperty("defaultEvent").objectReferenceValue = asset;
        serializedPlayer.ApplyModifiedProperties();

        EditorGUIUtility.PingObject(asset);
    }

    private static Vector3[] GetCorners(Bounds bounds)
    {
        return new[]
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
        };
    }

    [MenuItem("GameObject/Dialogue/Box Trigger", false, 10)]
    private static void CreateDialogueTrigger(MenuCommand menuCommand)
    {
        var go = new GameObject("Dialogue Trigger");
        Undo.RegisterCreatedObjectUndo(go, "Create Dialogue Trigger");

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        go.AddComponent<DialogueEventPlayer>();
        go.AddComponent<DialogueTriggerAuthoring>();

        Selection.activeGameObject = go;
    }
}
