using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Mob
{
    #region Perception & Player Tracking

    private void EnsurePlayerLayerMaskUpToDate(bool force = false)
    {
        if (player == null)
        {
            cachedPlayerLayerMask = 0;
            cachedPlayerInstanceId = 0;
            cachedPlayerColliders.Clear();
            cachedPlayerScale = new Vector3(float.NaN, float.NaN, float.NaN);
            nextPlayerLayerRefreshTime = 0f;
            return;
        }

        int instanceId = player.GetInstanceID();
        bool scaleChanged = !Approximately(player.lossyScale, cachedPlayerScale);
        bool refreshDue = Time.time >= nextPlayerLayerRefreshTime;

        if (!force && cachedPlayerInstanceId == instanceId && cachedPlayerLayerMask != 0 && cachedPlayerColliders.Count > 0 && !scaleChanged && !refreshDue)
        {
            return;
        }

        cachedPlayerInstanceId = instanceId;
        cachedPlayerLayerMask = 0;
        cachedPlayerColliders.Clear();
        cachedPlayerScale = player.lossyScale;
        nextPlayerLayerRefreshTime = Time.time + 0.5f;

        Collider[] colliders = player.GetComponentsInChildren<Collider>(true);
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                {
                    continue;
                }

                cachedPlayerColliders.Add(col);
                cachedPlayerLayerMask |= 1 << col.gameObject.layer;
            }
        }

        if (cachedPlayerLayerMask == 0)
        {
            cachedPlayerLayerMask = 1 << player.gameObject.layer;
        }
    }

    private int GetEffectiveTargetLayerMask()
    {
        int configuredMask = perception.targetLayers.value;
        if (configuredMask != 0)
        {
            return configuredMask;
        }

        if (player == null)
        {
            return 0;
        }

        EnsurePlayerLayerMaskUpToDate();
        return cachedPlayerLayerMask;
    }

    private void SamplePlayerMotion(float deltaTime)
    {
        if (player == null)
        {
            playerVelocity = Vector3.zero;
            previousPlayerPosition = Vector3.positiveInfinity;
            return;
        }

        if (!float.IsFinite(previousPlayerPosition.x))
        {
            previousPlayerPosition = player.position;
            previousPlayerSampleTime = Time.time;
            playerVelocity = Vector3.zero;
            return;
        }

        float timeSinceLastSample = Mathf.Max(0.0001f, Time.time - previousPlayerSampleTime);
        Vector3 delta = player.position - previousPlayerPosition;
        playerVelocity = Vector3.Lerp(playerVelocity, delta / timeSinceLastSample, 1f - Mathf.Exp(-6f * deltaTime));
        previousPlayerPosition = player.position;
        previousPlayerSampleTime = Time.time;
    }

    private void UpdatePerception(float deltaTime)
    {
        bool canSeePlayer = PlayerIsVisible();

        if (canSeePlayer)
        {
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = player.position;
        }
        else if (Time.time - lastSeenPlayerTime > perception.lostSightGrace && state == MobState.Attack)
        {
            // If we lose sight while attacking, flip orbit direction to help weave around obstacles next time.
            orbitDirection *= -1;
        }
    }

    private bool PlayerIsVisible()
    {
        int frame = Time.frameCount;
        if (cachedVisibilityFrame == frame)
        {
            return cachedPlayerVisible;
        }

        cachedPlayerVisible = ComputePlayerVisibility();
        cachedVisibilityFrame = frame;
        return cachedPlayerVisible;
    }

    private bool ComputePlayerVisibility()
    {
        if (player == null)
        {
            return false;
        }

        int targetMask = GetEffectiveTargetLayerMask();
        if (targetMask == 0)
        {
            return false;
        }

        Vector3 playerPosition = GetPlayerVisibilityPoint();
        Vector3 origin = transform.position + Vector3.up * 0.85f;
        Vector3 toPlayer = playerPosition - origin;
        float distance = toPlayer.magnitude;

        if (distance > perception.detectionRange)
        {
            return false;
        }

        Vector3 toPlayerFlat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (state == MobState.Idle || state == MobState.Patrol)
        {
            float angle = Vector3.Angle(transform.forward, toPlayerFlat);
            if (angle > perception.fieldOfView * 0.5f)
            {
                return false;
            }
        }

        Vector3 direction = distance > 0.0001f ? toPlayer / distance : Vector3.forward;
        int obstacleMask = perception.obstacleLayer.value;
        int combinedMask = obstacleMask | targetMask;

        int hitCount = Physics.RaycastNonAlloc(origin, direction, visibilityHits, distance, combinedMask, QueryTriggerInteraction.Ignore);
        while (hitCount == visibilityHits.Length)
        {
            GrowVisibilityBuffer();
            hitCount = Physics.RaycastNonAlloc(origin, direction, visibilityHits, distance, combinedMask, QueryTriggerInteraction.Ignore);
        }

        if (hitCount == 0)
        {
            return false;
        }

        Array.Sort(visibilityHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = visibilityHits[i].transform;

            if (hitTransform == null)
            {
                continue;
            }

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (BelongsToMob(hitTransform))
            {
                continue;
            }

            int layerBit = 1 << hitTransform.gameObject.layer;
            bool isTargetLayer = (targetMask & layerBit) != 0;

            if (isTargetLayer && IsPlayerTransform(hitTransform))
            {
                return true;
            }

            bool isObstacleLayer = (obstacleMask & layerBit) != 0;
            if (isObstacleLayer)
            {
                return false;
            }

            if (isTargetLayer)
            {
                continue;
            }

            return false;
        }

        return false;
    }

    private void GrowVisibilityBuffer()
    {
        int newSize = Mathf.Max(visibilityHits.Length * 2, 8);
        visibilityHits = new RaycastHit[newSize];
    }

    private Vector3 GetPlayerVisibilityPoint()
    {
        if (player == null)
        {
            return transform.position;
        }

        if (TryGetPlayerBounds(out Bounds bounds))
        {
            Vector3 point = bounds.center;
            point.y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.65f);
            return point;
        }

        float scaledHeight = Mathf.Clamp(player.lossyScale.y, 0.1f, 3f);
        return player.position + Vector3.up * (0.75f * scaledHeight);
    }

    private bool TryGetPlayerBounds(out Bounds bounds)
    {
        bounds = default;
        if (player == null)
        {
            return false;
        }

        EnsurePlayerLayerMaskUpToDate();
        if (BuildPlayerBoundsFromCache(ref bounds))
        {
            return true;
        }

        EnsurePlayerLayerMaskUpToDate(true);
        return BuildPlayerBoundsFromCache(ref bounds);
    }

    private bool BuildPlayerBoundsFromCache(ref Bounds bounds)
    {
        bool hasBounds = false;

        for (int i = 0; i < cachedPlayerColliders.Count; i++)
        {
            Collider col = cachedPlayerColliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds colBounds = col.bounds;
            if (!hasBounds)
            {
                bounds = colBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colBounds);
            }
        }

        return hasBounds;
    }

    private bool IsPlayerTransform(Transform candidate)
    {
        if (candidate == null || player == null)
        {
            return false;
        }

        if (candidate == player || candidate.IsChildOf(player))
        {
            return true;
        }

        Rigidbody rb = candidate.GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            return false;
        }

        Transform rbTransform = rb.transform;
        return rbTransform == player || rbTransform.IsChildOf(player);
    }

    private bool BelongsToMob(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Mob mobComponent = candidate.GetComponentInParent<Mob>();
        if (mobComponent == null)
        {
            return false;
        }

        return mobComponent != this;
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }

    #endregion
}
