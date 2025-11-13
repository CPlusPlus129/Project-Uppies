using UnityEngine;
using UnityEngine.AI;

public partial class Mob
{


    #region Public API

    public int CurrentHealth => currentHealth;
    public int MaxHealth => health.maxHealth;
    public bool IsAlive => isAlive;
    public float HealthPercentage => health.maxHealth <= 0 ? 0f : (float)currentHealth / health.maxHealth;

    public void ForceChase(Transform target = null, bool resetPath = true)
    {
        if (!isAlive)
        {
            return;
        }

        if (target != null)
        {
            player = target;
            EnsurePlayerLayerMaskUpToDate(true);
        }
        else if (player == null)
        {
            TryAutoAssignPlayer();
        }

        if (player != null)
        {
            EnsurePlayerLayerMaskUpToDate();
        }

        if (player == null)
        {
            if (showDebug)
            {
                Debug.LogWarning("Mob.ForceChase called but no player is assigned.", this);
            }
            return;
        }

        lastKnownPlayerPosition = player.position;
        lastSeenPlayerTime = Time.time;

        if (resetPath)
        {
            ClearPath();
        }

        TransitionTo(MobState.Chase);
        RequestDestination(PredictPlayerPosition(), locomotion.chasePathRefreshInterval);
    }

    public void StopChase(bool resumePatrol = true)
    {
        if (!isAlive)
        {
            return;
        }

        ClearPath();
        lastSeenPlayerTime = float.NegativeInfinity;

        if (resumePatrol && patrol.enabled)
        {
            TransitionTo(MobState.Patrol);
        }
        else
        {
            TransitionTo(MobState.Idle);
        }
    }

    public void ApplyNavMeshOverrides(float sampleRadius, float pathInterval, float cornerThreshold, int areaMask)
    {
        locomotion.destinationSampleRadius = Mathf.Max(0.1f, sampleRadius);
        locomotion.basePathRefreshInterval = Mathf.Max(0.05f, pathInterval);
        locomotion.chasePathRefreshInterval = Mathf.Max(0.05f, pathInterval * 0.6f);
        locomotion.attackPathRefreshInterval = Mathf.Max(0.05f, locomotion.chasePathRefreshInterval * 0.7f);
        attack.distanceTolerance = Mathf.Max(0.1f, cornerThreshold);
        navMeshAreaMask = areaMask;
        ClearPath();
    }

    #endregion
}
