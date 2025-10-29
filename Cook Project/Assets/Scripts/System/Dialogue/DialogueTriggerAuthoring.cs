using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Dialogue/Dialogue Trigger")]
[RequireComponent(typeof(DialogueEventPlayer))]
[RequireComponent(typeof(Collider))]
public class DialogueTriggerAuthoring : MonoBehaviour
{
    private enum TriggerBehaviour
    {
        PlayImmediately,
        QueueIfBusy,
        TryPlayOnly
    }

    [SerializeField]
    private DialogueEventPlayer eventPlayer;

    [SerializeField]
    private string triggeringTag = "Player";

    [SerializeField]
    private bool triggerOnce = true;

    [SerializeField]
    private bool disableColliderAfterTrigger = true;

    [SerializeField]
    private TriggerBehaviour triggerBehaviour = TriggerBehaviour.QueueIfBusy;

    [Header("Events")]
    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onDenied;

    private Collider cachedCollider;
    private bool hasTriggered;

    private void Reset()
    {
        eventPlayer = GetComponent<DialogueEventPlayer>();
        EnsureCollider();
    }

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        EnsureCollider();
    }

    private void OnValidate()
    {
        eventPlayer ??= GetComponent<DialogueEventPlayer>();
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

        if (!other.CompareTag(triggeringTag))
        {
            return;
        }

        if (eventPlayer == null)
        {
            Debug.LogWarning($"[{nameof(DialogueTriggerAuthoring)}] Missing DialogueEventPlayer reference on {name}.");
            return;
        }

        bool played = ExecuteBehaviour();
        if (!played)
        {
            onDenied?.Invoke();
            return;
        }

        hasTriggered = true;
        onEnter?.Invoke();

        if (triggerOnce && disableColliderAfterTrigger && cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }
    }

    private bool ExecuteBehaviour()
    {
        switch (triggerBehaviour)
        {
            case TriggerBehaviour.PlayImmediately:
                eventPlayer.Play();
                return true;
            case TriggerBehaviour.QueueIfBusy:
                eventPlayer.PlayAndQueue();
                return true;
            case TriggerBehaviour.TryPlayOnly:
                return eventPlayer.TryPlay();
            default:
                return false;
        }
    }

    private void EnsureCollider()
    {
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;
        cachedCollider = collider;
    }

    /// <summary>
    /// Allows other systems to reactivate the trigger.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        cachedCollider ??= GetComponent<Collider>();
        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }
    }
}
