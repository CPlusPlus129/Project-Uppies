using UnityEngine;

/// <summary>
/// Provides a UnityEvent-friendly hook for kicking off story sequences or events from interactables.
/// </summary>
public class StorySequenceActivator : MonoBehaviour
{
    [Header("Story Assets")]
    [SerializeField]
    [Tooltip("Sequence to enqueue when triggered. Takes priority if both sequence and single event are assigned.")]
    private StorySequenceAsset storySequence;

    [SerializeField]
    [Tooltip("Optional single story event to enqueue when no sequence is provided.")]
    private StoryEventAsset storyEvent;

    [Header("Queue Options")]
    [SerializeField]
    [Tooltip("Insert at the front of the queue to run immediately after the current story event.")]
    private bool insertAtFront = false;

    /// <summary>
    /// Call this from UnityInteractable or other systems to enqueue the configured story content.
    /// </summary>
    public void TriggerStory()
    {
        var flow = GameFlow.Instance;
        if (flow == null)
        {
            Debug.LogWarning($"{nameof(StorySequenceActivator)} on {name} could not find GameFlow.Instance.", this);
            return;
        }

        if (!flow.IsInitialized)
        {
            Debug.LogWarning($"{nameof(StorySequenceActivator)} attempted to trigger before GameFlow finished initializing.", this);
            return;
        }

        if (storySequence != null)
        {
            var runtimes = flow.EnqueueSequence(storySequence, insertAtFront);
            if (runtimes == null || runtimes.Count == 0)
            {
                Debug.LogWarning($"{nameof(StorySequenceActivator)} enqueued sequence '{storySequence.name}', but it contained no events.", this);
            }
            return;
        }

        if (storyEvent != null)
        {
            var runtime = flow.EnqueueEvent(storyEvent, insertAtFront);
            if (runtime == null)
            {
                Debug.LogWarning($"{nameof(StorySequenceActivator)} failed to enqueue event '{storyEvent.name}'.", this);
            }
            return;
        }

        Debug.LogWarning($"{nameof(StorySequenceActivator)} on {name} has no story assets assigned.", this);
    }
}
