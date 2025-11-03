using UnityEditor;
using UnityEngine;

public static class StoryEventTriggerAuthoringEditor
{
    private const string MenuPath = "GameObject/Story/Story Event Trigger";

    [MenuItem(MenuPath, false, 10)]
    private static void CreateStoryEventTrigger(MenuCommand menuCommand)
    {
        var go = new GameObject("Story Event Trigger");
        Undo.RegisterCreatedObjectUndo(go, "Create Story Event Trigger");

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        go.AddComponent<StoryEventTriggerAuthoring>();

        Selection.activeGameObject = go;
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateCreateStoryEventTrigger()
    {
        return !Application.isPlaying;
    }
}
