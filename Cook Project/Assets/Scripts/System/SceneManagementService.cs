using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneTransitionType
{
    Fade,
    Instant
}

public class SceneManagementService : ISceneManagementService
{
    public ReactiveProperty<string> CurrentSceneName { get; private set; } = new ReactiveProperty<string>();
    private List<string> sceneList;
    private string pendingSpawnPointId;

    public async UniTask Init()
    {
        CurrentSceneName.Value = SceneManager.GetActiveScene().name;
        sceneList = GetAllSceneNamesInBuild();
        // Listen for scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;
        await UniTask.CompletedTask;
    }

    public async UniTask LoadSceneAsync(string sceneName, string spawnPointId = null, SceneTransitionType transitionType = SceneTransitionType.Fade)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneManagementService] Scene name is null or empty!");
            return;
        }

        Debug.Log($"[SceneManagementService] Loading scene: {sceneName}, spawn point: {spawnPointId}");

        // Store spawn info for the next scene
        pendingSpawnPointId = spawnPointId;

        // Apply transition effect
        if (transitionType == SceneTransitionType.Fade)
        {
            await ShowTransitionEffect();
        }

        // Load the new scene
        var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        if (asyncOperation == null)
        {
            Debug.LogError($"[SceneManagementService] Failed to load scene: {sceneName}");
            return;
        }

        // Wait for scene to load
        while (!asyncOperation.isDone)
        {
            await UniTask.Yield();
        }

        CurrentSceneName.Value = sceneName;
        Debug.Log($"[SceneManagementService] Scene loaded: {sceneName}");

        // Wait a moment for scene initialization, then fade in
        if (transitionType == SceneTransitionType.Fade)
        {
            await UniTask.Delay(100); // Brief delay for scene setup
            await HideTransitionEffect();
        }
    }

    public string GetNextSceneName()
    {
        for (int i = 0; i < sceneList.Count; i++)
        {
            var sceneName = sceneList[i];
            if (sceneName == CurrentSceneName.Value)
            {
                return i + 1 >= sceneList.Count ? null : sceneList[i + 1];
            }
        }
        return null;
    }

    private List<string> GetAllSceneNamesInBuild()
    {
        List<string> names = new List<string>();
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            names.Add(sceneName);
        }
        return names;
    }


    public void SetPlayerSpawnInfo(string sceneName, string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneManagementService] Scene loaded event: {scene.name}");

        // Find and setup player spawn using scene-based spawn points
        //var playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();
        //if (playerSpawner != null)
        //{
        //    playerSpawner.SpawnPlayer(pendingSpawnPointId);
        //    pendingSpawnPointId = null; // Clear after use
        //}
        //else
        //{
        //    Debug.LogWarning($"[SceneManagementService] No PlayerSpawner found in scene: {scene.name}");
        //}
    }

    private async UniTask ShowTransitionEffect()
    {
        Debug.Log("[SceneManagementService] Starting fade transition");
        var transitionCanvas = GetTransitionCanvas();
        if (transitionCanvas != null)
        {
            await transitionCanvas.FadeOut();
        }
    }

    private async UniTask HideTransitionEffect()
    {
        Debug.Log("[SceneManagementService] Ending fade transition");
        var transitionCanvas = GetTransitionCanvas();
        if (transitionCanvas != null)
        {
            await transitionCanvas.FadeIn();
        }
    }

    private TransitionCanvas GetTransitionCanvas()
    {
        var uiRoot = UIRoot.Instance;
        if (uiRoot == null)
        {
            Debug.LogWarning("[SceneManagementService] UIRoot not found!");
            return null;
        }

        var transitionCanvas = uiRoot.GetUIComponent<TransitionCanvas>();
        if (transitionCanvas == null)
        {
            Debug.LogWarning("[SceneManagementService] TransitionCanvas not found in UIRoot!");
        }

        return transitionCanvas;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}