using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared light cache for mob systems so multiple mobs can query scene lights
/// without each performing their own full FindObjectsByType scan every frame.
/// </summary>
internal static class MobLightUtility
{
    private const float RefreshInterval = 0.35f;

    private static readonly List<Light> LightCache = new List<Light>(64);
    private static float nextRefreshTime = 0f;

    /// <summary>
    /// Returns an up-to-date list of enabled lights. The cache is refreshed at
    /// most once per RefreshInterval (and immediately while in edit mode).
    /// </summary>
    public static IReadOnlyList<Light> GetLights()
    {
        if (!Application.isPlaying)
        {
            RefreshNow();
            return LightCache;
        }

        if (Time.unscaledTime >= nextRefreshTime || LightCache.Count == 0)
        {
            RefreshNow();
        }

        return LightCache;
    }

    public static void MarkDirty()
    {
        nextRefreshTime = 0f;
    }

    private static void RefreshNow()
    {
        nextRefreshTime = Application.isPlaying ? Time.unscaledTime + RefreshInterval : 0f;

        LightCache.Clear();
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || !light.enabled)
            {
                continue;
            }

            LightCache.Add(light);
        }
    }
}
