using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Story/Story Signal Emitter")]
public class StorySignalEmitter : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Signal identifier to deliver to GameFlow. Leave blank to use the value provided when invoking Emit(string).")]
    private string signalId;

    [SerializeField]
    [Tooltip("Log to the console when the signal is emitted. Useful for debugging sequencing issues.")]
    private bool verboseLogging = false;

    [Header("Trigger Options")]
    [SerializeField]
    [Tooltip("Automatically emit when something enters this collider.")]
    private bool emitOnTriggerEnter = false;

    [SerializeField]
    [Tooltip("Only emit when the entering collider matches this tag. Leave empty to accept any tag.")]
    private string triggeringTag = "Player";

    [SerializeField]
    [Tooltip("Limit automatic emission to a single trigger enter.")]
    private bool triggerOnce = true;

    [SerializeField]
    [Tooltip("Disable the collider after an automatic emission.")]
    private bool disableColliderAfterEmit = true;

    private Collider cachedCollider;
    private bool hasEmitted;

    private void Reset()
    {
        CacheCollider();
    }

    private void Awake()
    {
        CacheCollider();
    }

    private void OnValidate()
    {
        CacheCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!emitOnTriggerEnter || !isActiveAndEnabled)
        {
            return;
        }

        if (triggerOnce && hasEmitted)
        {
            return;
        }

        if (!string.IsNullOrEmpty(triggeringTag) && !other.CompareTag(triggeringTag))
        {
            return;
        }

        var fulfilled = Emit();
        if (!fulfilled)
        {
            // No listener yet—keep collider active so the player can try again later.
            return;
        }

        hasEmitted = true;

        if (triggerOnce && disableColliderAfterEmit && cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }
    }

    public bool Emit()
    {
        return EmitInternal(signalId);
    }

    public bool Emit(string overrideSignalId)
    {
        return EmitInternal(string.IsNullOrWhiteSpace(overrideSignalId) ? signalId : overrideSignalId);
    }

    private bool EmitInternal(string resolvedId)
    {
        if (string.IsNullOrWhiteSpace(resolvedId))
        {
            Debug.LogWarning($"[{nameof(StorySignalEmitter)}] No signal id specified on {name}.", this);
            return false;
        }

        var flow = GameFlow.Instance;
        if (flow == null)
        {
            Debug.LogWarning($"[{nameof(StorySignalEmitter)}] GameFlow instance not present when emitting '{resolvedId}'.", this);
            return false;
        }

        var fulfilled = flow.Signal(resolvedId);
        if (verboseLogging)
        {
            var suffix = fulfilled ? string.Empty : " (no listeners yet)";
            Debug.Log($"[{nameof(StorySignalEmitter)}] Emitted signal '{resolvedId}' from {name}{suffix}.", this);
        }

        return fulfilled;
    }

    private void CacheCollider()
    {
        if (!emitOnTriggerEnter)
        {
            return;
        }

        cachedCollider ??= GetComponent<Collider>();
        if (cachedCollider == null)
        {
            cachedCollider = gameObject.AddComponent<BoxCollider>();
        }

        cachedCollider.isTrigger = true;
    }
}
