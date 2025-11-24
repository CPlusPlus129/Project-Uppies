using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GlowObjectStoryEvent", menuName = "Game Flow/Environment Events/Glow Object Until Interact (New)")]
public class GlowObjectStoryEvent : StoryEventAsset, IBackgroundStoryEvent
{
    [Header("Execution Flow")]
    [SerializeField]
    [Tooltip("If true, this event runs concurrently with the queue.")]
    private bool runInBackground = false;

    [SerializeField]
    [Tooltip("If running in background, blocks subsequent events from the SAME sequence until completion.")]
    private bool blockSourceSequence = false;

    public bool RunInBackground => runInBackground;
    public bool BlockSourceSequence => blockSourceSequence;

    [Header("Targeting")]
    [SerializeField]
    [Tooltip("Name of the GameObject to find in the scene. Can be a simple name (e.g., 'DoorButton1') or a path (e.g., 'Parent/Child/DoorButton1').")]
    private string targetObjectName;

    [SerializeField]
    [Tooltip("If this task is complete, the glowing process will be skipped.")]
    private string skipIfTaskIsCompleteTaskID = "";

    [SerializeField]
    private bool searchAllScenes = false;

    [Header("Glow Configuration")]
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField] private float glowIntensity = 2.5f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float glowScale = 1.15f;
    [SerializeField] private float fresnelPower = 3f;
    [SerializeField] private float pulseVariation = 0.35f;

    [Header("Interaction")]
    [SerializeField]
    [Tooltip("If true, the event waits until the player interacts with the object.")]
    private bool waitForInteraction = true;

    [SerializeField]
    [Tooltip("If true, the UnityInteractable component is disabled after interaction.")]
    private bool disableInteractableAfterUse = true;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(skipIfTaskIsCompleteTaskID))
        {
            var isCompleted = TaskManager.Instance.IsTaskCompleted(skipIfTaskIsCompleteTaskID);
            if(isCompleted)
            {
                return StoryEventResult.Completed($"GlowObjectStoryEvent: Task '{skipIfTaskIsCompleteTaskID}' is already completed. Skipping glow.");
            }
        }

        // 1. Find Target
        GameObject target = FindTarget(targetObjectName, searchAllScenes);
        if (target == null)
        {
            return StoryEventResult.Failed($"GlowObjectStoryEvent: Could not find object named '{targetObjectName}'");
        }

        // 2. Setup FridgeGlowController
        var fridgeGlow = target.GetComponent<FridgeGlowController>();
        if (fridgeGlow == null)
        {
            fridgeGlow = target.AddComponent<FridgeGlowController>();
        }

        fridgeGlow.ConfigureGlow(
            color: glowColor,
            intensity: glowIntensity,
            newPulseSpeed: pulseSpeed,
            newScale: glowScale,
            newFresnelPower: fresnelPower,
            newPulseVariation: pulseVariation
        );

        // NEW: Explicitly set mesh if the target has one to prevent it picking a child mesh
        var targetMeshFilter = target.GetComponent<MeshFilter>();
        if (targetMeshFilter != null)
        {
            fridgeGlow.SetTargetMesh(targetMeshFilter);
        }

        fridgeGlow.StartGlowing();

        // 3. Setup Interaction - always setup to stop glow when player interacts
        UnityInteractable interactable = target.GetComponent<UnityInteractable>();
        if (interactable == null)
        {
            interactable = target.AddComponent<UnityInteractable>();
        }

        // Define listener that stops glow and optionally disables the interactable
        UnityAction onStopGlow = null;
        onStopGlow = () =>
        {
            fridgeGlow.StopGlowing();

            if (disableInteractableAfterUse)
            {
                interactable.enabled = false;
            }

            // Remove self after execution to prevent duplicate calls
            interactable.RemoveOnInteractListener(onStopGlow);
        };

        // Always subscribe to stop glow on interaction
        interactable.AddOnInteractListener(onStopGlow);

        // 4. Conditionally wait for interaction
        if (waitForInteraction)
        {
            var tcs = new UniTaskCompletionSource();
            UnityAction onComplete = () => tcs.TrySetResult();

            interactable.AddOnInteractListener(onComplete);

            try
            {
                // Wait for player to interact
                await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Clean up completion listener on cancellation
                interactable.RemoveOnInteractListener(onComplete);
                throw;
            }
            finally
            {
                // Always clean up completion listener after await
                interactable.RemoveOnInteractListener(onComplete);
            }

            return StoryEventResult.Completed("Glow object interacted.");
        }
        else
        {
            // Don't wait - return immediately while the glow persists
            // The onStopGlow listener will handle stopping when player interacts later
            // Unity will auto-cleanup the listener when GameObject is destroyed (UnityEvent behavior)
            return StoryEventResult.Completed("Glow started (no wait).");
        }
    }

    private GameObject FindTarget(string nameOrPath, bool allScenes)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;

        // Check if it's a path
        bool isPath = nameOrPath.Contains("/");

        if (!allScenes)
        {
            // Search active scene
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                if (isPath)
                {
                    return FindByPath(activeScene, nameOrPath);
                }
                else
                {
                    return FindByNameRecursive(activeScene, nameOrPath);
                }
            }
        }
        else
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject found = isPath ? FindByPath(scene, nameOrPath) : FindByNameRecursive(scene, nameOrPath);
                    if (found != null) return found;
                }
            }
        }

        return null;
    }

    private GameObject FindByPath(Scene scene, string path)
    {
        // Path usually looks like "Root/Child/GrandChild"
        // Since GameObject.Find only works in active scene, we manually traverse.
        // Split path
        var parts = path.Split('/');
        if (parts.Length == 0) return null;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == parts[0])
            {
                if (parts.Length == 1) return root;

                // Traverse down
                Transform current = root.transform;
                bool match = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current.Find(parts[i]);
                    if (next == null)
                    {
                        match = false;
                        break;
                    }
                    current = next;
                }

                if (match) return current.gameObject;
            }
        }
        return null;
    }

    private GameObject FindByNameRecursive(Scene scene, string name)
    {
        var roots = scene.GetRootGameObjects();
        List<GameObject> matches = new List<GameObject>();

        foreach (var root in roots)
        {
            FindMatchesRecursive(root.transform, name, matches);
        }

        if (matches.Count == 0) return null;

        if (matches.Count > 1)
        {
            Debug.LogWarning($"[GlowObjectStoryEvent] Found {matches.Count} objects named '{name}'. Using the first one found ('{GetPath(matches[0].transform)}'). Consider using a full path or renaming to avoid ambiguity.");
        }

        return matches[0];
    }

    private void FindMatchesRecursive(Transform parent, string name, List<GameObject> results)
    {
        if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            results.Add(parent.gameObject);
        }

        foreach (Transform child in parent)
        {
            FindMatchesRecursive(child, name, results);
        }
    }

    private string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
