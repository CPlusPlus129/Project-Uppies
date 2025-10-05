using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optional debug visualizer for the Dynamic Shadow System.
/// Shows which lights are currently casting shadows in the Game view.
/// Attach to the same GameObject as DynamicShadowManager.
/// </summary>
[RequireComponent(typeof(DynamicShadowManager))]
public class DynamicShadowDebugger : MonoBehaviour
{
    [Header("Debug Display")]
    [Tooltip("Show debug overlay in Game view")]
    public bool showDebugOverlay = true;

    [Tooltip("Show shadow indicators in Scene view")]
    public bool showSceneGizmos = true;

    [Header("Display Settings")]
    public Color shadowCastingColor = Color.yellow;
    public Color noShadowColor = Color.gray;
    public float gizmoRadius = 0.5f;

    [Header("Performance")]
    [Tooltip("How often to refresh the light cache (in frames). Higher = better performance.")]
    [Range(1, 120)]
    public int cacheRefreshInterval = 60;

    private DynamicShadowManager manager;
    private GUIStyle labelStyle;
    private List<DynamicShadowLight> cachedLights = new List<DynamicShadowLight>();
    private int framesSinceLastRefresh = 0;

    private void Awake()
    {
        manager = GetComponent<DynamicShadowManager>();
    }

    private void OnGUI()
    {
        if (!showDebugOverlay || manager == null) return;

        // Initialize style
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box);
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.fontSize = 14;
            labelStyle.normal.textColor = Color.white;
        }

        // Display info box
        string info = $"Dynamic Shadow System\n";
        info += $"Max Shadow Lights: {manager.maxShadowCastingLights}\n";
        info += $"Update Interval: {manager.updateInterval} frames\n";
        info += $"Shadow Type: {(manager.useHardShadows ? "Hard" : "Soft")}";

        GUI.Box(new Rect(10, 10, 300, 100), info, labelStyle);
    }

    private void OnDrawGizmos()
    {
        if (!showSceneGizmos) return;

        // Refresh cache periodically instead of every frame
        framesSinceLastRefresh++;
        if (framesSinceLastRefresh >= cacheRefreshInterval || cachedLights.Count == 0)
        {
            RefreshLightCache();
            framesSinceLastRefresh = 0;
        }

        // Use cached lights instead of finding them every frame
        foreach (var light in cachedLights)
        {
            if (light == null || !light.isActiveAndEnabled) continue;

            // Color based on shadow status
            Gizmos.color = light.IsCastingShadows() ? shadowCastingColor : noShadowColor;

            // Draw sphere at light position
            Gizmos.DrawWireSphere(light.transform.position, gizmoRadius);

            // Draw label
            #if UNITY_EDITOR
            if (light.IsCastingShadows())
            {
                UnityEditor.Handles.Label(
                    light.transform.position + Vector3.up * (gizmoRadius + 0.2f),
                    "SHADOW",
                    new GUIStyle() { normal = new GUIStyleState() { textColor = shadowCastingColor } }
                );
            }
            #endif
        }

        // Draw line from player to shadow-casting lights
        if (manager != null && manager.playerTransform != null)
        {
            foreach (var light in cachedLights)
            {
                if (light != null && light.IsCastingShadows())
                {
                    Gizmos.color = shadowCastingColor;
                    Gizmos.DrawLine(manager.playerTransform.position, light.transform.position);
                }
            }
        }
    }

    /// <summary>
    /// Refresh the cached list of lights (called periodically)
    /// </summary>
    private void RefreshLightCache()
    {
        cachedLights.Clear();
        DynamicShadowLight[] lights = FindObjectsByType<DynamicShadowLight>(FindObjectsSortMode.None);
        cachedLights.AddRange(lights);
    }
}
