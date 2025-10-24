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

            // Warning if no sprite assigned
            if (billboard.GetSprite() == null)
            {
                EditorGUILayout.HelpBox("No sprite assigned! Drag a sprite into the Sprite field above.", MessageType.Warning);
            }
        }
    }
}
