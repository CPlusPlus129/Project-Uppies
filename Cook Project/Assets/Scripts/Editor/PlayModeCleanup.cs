#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures simple singletons and service infrastructure are torn down when exiting play mode,
/// which is critical when domain reloads are disabled.
/// </summary>
[InitializeOnLoad]
internal static class PlayModeCleanup
{
    static PlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            CleanupPersistentSystems();
        }
    }

    private static void CleanupPersistentSystems()
    {
        var locator = SimpleSingleton<ServiceLocator>.TryGetInstance();
        locator?.Shutdown();

        SimpleSingleton<WorldBroadcastSystem>.ResetInstance();
        SimpleSingleton<ShopSystem>.ResetInstance();

        var singletonRoot = GameObject.Find("Singleton");
        if (singletonRoot != null)
        {
            Object.DestroyImmediate(singletonRoot);
        }
    }
}
#endif
