using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GlowSceneObjectUntilInteract", menuName = "Game Flow/Story Events/Glow Scene Object")]
public sealed class GlowSceneObjectUntilInteractStoryEventAsset : StoryEventAsset
{
    [Header("Target")]
    [SerializeField]
    private string targetObjectName;

    [SerializeField]
    private bool includeInactive = true;

    [SerializeField]
    private bool searchAllScenes = false;

    [Header("Glow Settings")]
    [SerializeField]
    private Color glowColor = Color.cyan;

    [SerializeField]
    private float glowIntensity = 2.5f;

    [SerializeField]
    private bool pulse = true;

    [SerializeField]
    private float pulseSpeed = 2f;

    [SerializeField]
    private bool addGlowComponentIfMissing = true;

    [Header("Interaction")]
    [SerializeField]
    private bool waitForInteraction = true;

    [SerializeField]
    private bool disableInteractableAfterUse = true;

    [SerializeField]
    private string signalOnInteract;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetObjectName))
        {
            return StoryEventResult.Failed("Target object name is not set.");
        }

        var targets = FindTargets(targetObjectName);
        if (targets.Count == 0)
        {
            return StoryEventResult.Failed($"No object named '{targetObjectName}' was found in the loaded scenes.");
        }

        var waitList = new List<UniTask>();

        foreach (var target in targets)
        {
            if (target == null)
            {
                continue;
            }

            var interactable = target.GetComponent<UnityInteractable>() ?? target.AddComponent<UnityInteractable>();
            var glow = target.GetComponent<SceneObjectGlowController>();
            if (glow == null && addGlowComponentIfMissing)
            {
                glow = target.AddComponent<SceneObjectGlowController>();
            }

            if (glow == null)
            {
                Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Target '{target.name}' lacks {nameof(SceneObjectGlowController)} and could not add one.");
                continue;
            }

            glow.Configure(glowColor, glowIntensity, pulse, pulseSpeed);
            glow.EnableGlow();

            if (waitForInteraction)
            {
                var tcs = new UniTaskCompletionSource<bool>();
                UnityAction handler = null;
                handler = () =>
                {
                    glow.DisableGlow();
                    interactable.RemoveOnInteractListener(handler);
                    if (disableInteractableAfterUse)
                    {
                        interactable.enabled = false;
                    }

                    if (!string.IsNullOrWhiteSpace(signalOnInteract))
                    {
                        context.SendSignal(signalOnInteract);
                    }

                    tcs.TrySetResult(true);
                };

                interactable.AddOnInteractListener(handler);
                waitList.Add(tcs.Task);
            }
        }

        if (waitForInteraction && waitList.Count > 0)
        {
            await UniTask.WhenAll(waitList).AttachExternalCancellation(cancellationToken);
        }

        return StoryEventResult.Completed(waitForInteraction ? "Glow object interacted." : "Glow applied to targets.");
    }

    private List<GameObject> FindTargets(string desiredName)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new List<GameObject>();
        if (searchAllScenes)
        {
            var count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                Collect(scene, desiredName, comparison, results);
            }
        }
        else
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() && SceneManager.sceneCount > 0)
            {
                active = SceneManager.GetSceneAt(0);
            }

            if (active.IsValid())
            {
                Collect(active, desiredName, comparison, results);
            }
        }

        return results;
    }

    private void Collect(Scene scene, string desiredName, StringComparison comparison, List<GameObject> buffer)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Traverse(roots[i], desiredName, comparison, buffer);
        }
    }

    private void Traverse(GameObject node, string desiredName, StringComparison comparison, List<GameObject> buffer)
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
            buffer.Add(node);
        }

        var transform = node.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            Traverse(transform.GetChild(i).gameObject, desiredName, comparison, buffer);
        }
    }
}
