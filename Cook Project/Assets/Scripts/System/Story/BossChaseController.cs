using UnityEngine;

/// <summary>
/// Helper component that exposes UnityEvent-friendly entry points for starting or stopping a boss chase.
/// </summary>
public class BossChaseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("Mob component that drives the boss AI.")]
    private Mob bossMob;

    [SerializeField]
    [Tooltip("Optional explicit player target. If left empty the mob's assigned target or a tagged Player will be used.")]
    private Transform playerOverride;

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Freeze the boss in place until BeginChase is invoked.")]
    private bool holdPositionUntilChase = true;

    private Rigidbody cachedRigidbody;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialIsKinematic;
    private bool hasCachedRigidbodyState;

    private void Awake()
    {
        if (bossMob == null)
        {
            bossMob = GetComponent<Mob>();
        }

        if (bossMob != null)
        {
            cachedRigidbody = bossMob.GetComponent<Rigidbody>();
            if (cachedRigidbody != null)
            {
                initialIsKinematic = cachedRigidbody.isKinematic;
                hasCachedRigidbodyState = true;
            }
        }

        if (holdPositionUntilChase)
        {
            CacheStartingTransform();
            ApplyHoldPositionState(true);
        }
    }

    private void CacheStartingTransform()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    /// <summary>
    /// Public entry point for UnityEvents or interactables to begin the chase.
    /// </summary>
    public void BeginChase()
    {
        if (bossMob == null)
        {
            Debug.LogWarning($"{nameof(BossChaseController)} on {name} has no Mob reference assigned.", this);
            return;
        }

        if (holdPositionUntilChase)
        {
            ApplyHoldPositionState(false);
        }

        bossMob.ForceChase(playerOverride);
    }

    /// <summary>
    /// Stops the chase and lets the mob resume its idle/patrol behaviour.
    /// </summary>
    public void StopChase()
    {
        if (bossMob == null)
        {
            return;
        }

        bossMob.StopChase();

        if (holdPositionUntilChase)
        {
            ApplyHoldPositionState(true);
        }
    }

    private void ApplyHoldPositionState(bool hold)
    {
        if (bossMob == null)
        {
            return;
        }

        if (hold)
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (cachedRigidbody != null)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
            }
            bossMob.enabled = false;
        }
        else
        {
            bossMob.enabled = true;
            if (cachedRigidbody != null)
            {
                if (hasCachedRigidbodyState)
                {
                    cachedRigidbody.isKinematic = initialIsKinematic;
                }
                else
                {
                    cachedRigidbody.isKinematic = false;
                }
            }
        }
    }
}
