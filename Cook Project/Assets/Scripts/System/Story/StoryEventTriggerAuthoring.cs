using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Story/Story Event Trigger")]
[RequireComponent(typeof(Collider))]
public class StoryEventTriggerAuthoring : MonoBehaviour
{
    private const string LogPrefix = "[StoryEventTrigger]";

    [SerializeField]
    [Tooltip("Optional single event to enqueue when the trigger fires.")]
    private StoryEventAsset storyEvent;

    [SerializeField]
    [Tooltip("Optional sequence to enqueue when the trigger fires. Takes priority over the single event if both are set.")]
    private StorySequenceAsset storySequence;

    [SerializeField]
    [Tooltip("Tag that must be present on the entering collider.")]
    private string triggeringTag = "Player";

    [SerializeField]
    [Tooltip("Enqueue only once. Disable to allow repeat triggering.")]
    private bool triggerOnce = true;

    [SerializeField]
    [Tooltip("Disable the collider after a successful trigger.")]
    private bool disableColliderAfterTrigger = true;

    [SerializeField]
    [Tooltip("Insert the event/sequence at the front of the queue so it runs next.")]
    private bool insertAtFront = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onTriggered;
    [SerializeField] private UnityEvent onFailed;

    private Collider cachedCollider;
    private bool hasTriggered;

    private void Reset()
    {
        EnsureCollider();
    }

    private void Awake()
    {
        EnsureCollider();
    }

    private void OnValidate()
    {
        EnsureCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled)
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!string.IsNullOrEmpty(triggeringTag) && !other.CompareTag(triggeringTag))
        {
            return;
        }

        if (!TryEnqueueStory(out var warning))
        {
            if (!string.IsNullOrEmpty(warning))
            {
                Debug.LogWarning($"{LogPrefix} {warning}", this);
            }

            onFailed?.Invoke();
            return;
        }

        hasTriggered = true;
        onTriggered?.Invoke();

        if (triggerOnce && disableColliderAfterTrigger && cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        EnsureCollider();
        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }
    }

    private bool TryEnqueueStory(out string warning)
    {
        warning = null;

        if (storySequence == null && storyEvent == null)
        {
            warning = "Nothing to enqueue. Assign a StorySequenceAsset or StoryEventAsset.";
            return false;
        }

        var flow = GameFlow.Instance;
        if (flow == null)
        {
            warning = "GameFlow instance not found in the scene.";
            return false;
        }

        if (!flow.IsInitialized)
        {
            warning = "GameFlow is not initialized yet.";
            return false;
        }

        if (storySequence != null)
        {
            var runtimes = flow.EnqueueSequence(storySequence, insertAtFront);
            if (runtimes == null || runtimes.Count == 0)
            {
                warning = $"Sequence '{storySequence.name}' had no events to enqueue.";
                return false;
            }

            return true;
        }

        var runtime = flow.EnqueueEvent(storyEvent, insertAtFront);
        if (runtime == null)
        {
            warning = $"Failed to enqueue event '{storyEvent.name}'.";
            return false;
        }

        return true;
    }

    private void EnsureCollider()
    {
        cachedCollider ??= GetComponent<Collider>();
        if (cachedCollider == null)
        {
            cachedCollider = gameObject.AddComponent<BoxCollider>();
        }

        cachedCollider.isTrigger = true;
    }
}
