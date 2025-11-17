using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "FadeSceneObjectStoryEvent", menuName = "Game Flow/Environment Events/Fade Scene Object")]
public sealed class FadeSceneObjectStoryEventAsset : StoryEventAsset
{
    [Header("Target Resolution")]
    [SerializeField]
    [Tooltip("Name of the GameObject in the active scene (match is case-insensitive).")]
    private string targetObjectName = "BabyBossReturns";

    [SerializeField]
    [Tooltip("Include inactive objects when searching for the target.")]
    private bool includeInactive = true;

    [SerializeField]
    [Tooltip("Also search across all loaded scenes instead of just the active one.")]
    private bool searchAllLoadedScenes = false;

    [Header("Fade Options")]
    [SerializeField]
    [Tooltip("Add a SceneObjectFadeAway component automatically if it's missing.")]
    private bool addFadeComponentIfMissing = true;

    [SerializeField]
    [Tooltip("Wait for the object to be destroyed/disabled after triggering the fade. Uses the destroyOnComplete flag on SceneObjectFadeAway.")]
    private bool waitForFadeToFinish = true;

    [SerializeField]
    [Tooltip("Maximum seconds to wait for the fade completion before the event continues.")]
    private float fadeWaitTimeoutSeconds = 5f;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetObjectName))
        {
            return StoryEventResult.Failed("Target object name is not configured.");
        }

        var targets = FindTargets(targetObjectName);
        if (targets.Count == 0)
        {
            return StoryEventResult.Failed($"No GameObject named '{targetObjectName}' was found in the loaded scenes.");
        }

        foreach (var target in targets)
        {
            if (target == null)
            {
                continue;
            }

            var fadeComponent = target.GetComponent<SceneObjectFadeAway>();
            if (fadeComponent == null && addFadeComponentIfMissing)
            {
                fadeComponent = target.AddComponent<SceneObjectFadeAway>();
            }

            if (fadeComponent == null)
            {
                Debug.LogWarning($"[{nameof(FadeSceneObjectStoryEventAsset)}] Target '{target.name}' does not have a {nameof(SceneObjectFadeAway)} component.");
                continue;
            }

            fadeComponent.FadeAway();

            if (waitForFadeToFinish)
            {
                await WaitForFadeCompletionAsync(fadeComponent, cancellationToken);
            }
        }

        return StoryEventResult.Completed($"Triggered fade on {targets.Count} object(s) matching '{targetObjectName}'.");
    }

    private async UniTask WaitForFadeCompletionAsync(SceneObjectFadeAway fadeComponent, CancellationToken token)
    {
        var timeout = Mathf.Max(0.1f, fadeWaitTimeoutSeconds);
        var deadline = Time.realtimeSinceStartup + timeout;

        while (fadeComponent != null && fadeComponent.gameObject != null && Time.realtimeSinceStartup < deadline)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private List<GameObject> FindTargets(string desiredName)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new List<GameObject>();
        if (searchAllLoadedScenes)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                CollectMatches(scene, desiredName, comparison, results);
            }
        }
        else
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                activeScene = SceneManager.GetSceneAt(0);
            }

            if (activeScene.IsValid())
            {
                CollectMatches(activeScene, desiredName, comparison, results);
            }
        }

        return results;
    }

    private void CollectMatches(Scene scene, string desiredName, StringComparison comparison, List<GameObject> results)
    {
        var rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            var root = rootObjects[i];
            TraverseHierarchy(root, desiredName, comparison, results);
        }
    }

    private void TraverseHierarchy(GameObject node, string desiredName, StringComparison comparison, List<GameObject> results)
    {
        if (node == null)
        {
            return;
        }

        if (!includeInactive && !node.activeInHierarchy)
        {
            return;
        }

        if (string.Equals(node.name, desiredName, comparison))
        {
            results.Add(node);
        }

        var transform = node.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            TraverseHierarchy(child.gameObject, desiredName, comparison, results);
        }
    }
}
