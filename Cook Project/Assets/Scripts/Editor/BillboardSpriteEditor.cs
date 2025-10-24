using UnityEngine;
using UnityEditor;

namespace NvJ.Rendering.Editor
{
    /// <summary>
    /// Custom inspector for BillboardSprite to add helpful buttons and warnings
    /// </summary>
    [CustomEditor(typeof(BillboardSprite))]
    public class BillboardSpriteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BillboardSprite billboard = (BillboardSprite)target;

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Refresh button
            if (GUILayout.Button("Refresh Billboard", GUILayout.Height(30)))
            {
                billboard.Refresh();
                EditorUtility.SetDirty(billboard);
            }
            
            // Force Recreate button (for troubleshooting lighting issues)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Recreate (Fix Lighting Issues)", GUILayout.Height(25), GUILayout.Width(250)))
            {
                if (EditorUtility.DisplayDialog("Force Recreate Billboard",
                    "This will destroy and recreate the billboard material and mesh. Use this if lighting is not working correctly.\n\nContinue?",
                    "Yes", "Cancel"))
                {
                    billboard.ForceRecreate();
                    EditorUtility.SetDirty(billboard);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Warning if no sprite assigned
            if (billboard.GetSprite() == null)
            {
                EditorGUILayout.HelpBox("No sprite assigned! Drag a sprite into the Sprite field above.", MessageType.Warning);
            }
        }
    }
}
