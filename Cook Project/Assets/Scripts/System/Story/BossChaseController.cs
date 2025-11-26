using UnityEngine;
using UnityEngine.AI;

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

    [Header("Visual Effects")]
    [SerializeField]
    [Tooltip("Optional pulse effect to enable only during the chase.")]
    private UniversalPulseController pulseEffect;

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Freeze the boss in place until BeginChase is invoked.")]
    private bool holdPositionUntilChase = true;

    [Header("Task")]
    [SerializeField]
    [Tooltip("Complete the task Id on chase starts.")]
    private string onChaseStart_toCompleteTaskId;

    private NavMeshAgent cachedAgent;
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
            cachedAgent = bossMob.GetComponent<NavMeshAgent>();
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

        if (pulseEffect != null)
        {
            pulseEffect.enabled = false;
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
        if (!EnsureMobActive())
        {
            return;
        }

        if (holdPositionUntilChase)
        {
            ApplyHoldPositionState(false);
        }
        else
        {
            EnsureAgentReady();
        }

        if (pulseEffect != null)
        {
            pulseEffect.enabled = true;
        }

        bossMob.ForceChase(playerOverride);
        if(!string.IsNullOrEmpty(onChaseStart_toCompleteTaskId))
        {
            TaskManager.Instance.CompleteTask(onChaseStart_toCompleteTaskId);
        }   
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

        if (pulseEffect != null)
        {
            pulseEffect.enabled = false;
        }

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
                if (!cachedRigidbody.isKinematic)
                {
                    cachedRigidbody.linearVelocity = Vector3.zero;
                    cachedRigidbody.angularVelocity = Vector3.zero;
                }
                cachedRigidbody.isKinematic = true;
            }
            bossMob.enabled = false;
        }
        else
        {
            bossMob.enabled = true;
            EnsureAgentReady();
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

    private void EnsureAgentReady()
    {
        if (cachedAgent == null && bossMob != null)
        {
            cachedAgent = bossMob.GetComponent<NavMeshAgent>();
        }

        if (cachedAgent == null)
        {
            return;
        }

        if (!cachedAgent.enabled)
        {
            cachedAgent.enabled = true;
        }

        if (!cachedAgent.isOnNavMesh)
        {
            // Try to warp onto nearest navmesh within a small radius
            if (NavMesh.SamplePosition(cachedAgent.transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                cachedAgent.Warp(hit.position);
            }
        }
    }

    /// <summary>
    /// Makes sure the mob/agent are enabled before a chase is started. This is
    /// primarily to handle scenes where the Mob component was left disabled in
    /// the inspector while waiting for an interactable trigger.
    /// </summary>
    private bool EnsureMobActive()
    {
        if (bossMob == null)
        {
            Debug.LogWarning($"{nameof(BossChaseController)} on {name} has no Mob reference assigned.", this);
            return false;
        }

        if (!bossMob.enabled)
        {
            bossMob.enabled = true;
        }

        EnsureAgentReady();

        if (cachedAgent != null && !cachedAgent.enabled)
        {
            cachedAgent.enabled = true;
        }

        return true;
    }
}
