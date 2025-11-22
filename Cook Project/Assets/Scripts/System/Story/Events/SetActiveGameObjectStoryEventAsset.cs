using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "SetActiveObjectEvent", menuName = "Game Flow/Environment Events/Set Active GameObject")]
public class SetActiveGameObjectStoryEventAsset : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Name of the target GameObject (case-insensitive).")]
    private string targetObjectName;

    [SerializeField]
    [Tooltip("True to enable the object, False to disable it.")]
    private bool setActive = true;

    [SerializeField]
    [Tooltip("If true, searches through inactive parents to find the object.")]
    private bool includeInactive = true;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetObjectName))
        {
            return UniTask.FromResult(StoryEventResult.Failed("Target object name is empty."));
        }

        var targets = FindTargets(targetObjectName);
        int count = 0;
        
        foreach(var target in targets)
        {
            if(target != null) 
            {
                target.SetActive(setActive);
                count++;
            }
        }

        if (count == 0)
        {
            return UniTask.FromResult(StoryEventResult.Completed($"No objects named '{targetObjectName}' found. No changes made."));
        }

        return UniTask.FromResult(StoryEventResult.Completed($"Set {count} object(s) named '{targetObjectName}' to active={setActive}"));
    }

    private List<GameObject> FindTargets(string name)
    {
        var results = new List<GameObject>();
        var comparison = StringComparison.OrdinalIgnoreCase;
        var scene = SceneManager.GetActiveScene();
        
        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                Traverse(root, name, comparison, results);
            }
        }
        
        return results;
    }

    private void Traverse(GameObject node, string name, StringComparison comparison, List<GameObject> results)
    {
        if (!includeInactive && !node.activeInHierarchy) 
        {
            return;
        }

        if (string.Equals(node.name, name, comparison))
        {
            results.Add(node);
        }

        foreach(Transform child in node.transform)
        {
            Traverse(child.gameObject, name, comparison, results);
        }
    }
}
