using UnityEngine;
using UnityEngine.AI;

public partial class Mob
{

    #region Navigation & Movement

    private void RequestDestination(Vector3 worldPoint, float refreshInterval)
    {
        float time = Time.time;
        bool destinationChanged = (worldPoint - destinationRequest).sqrMagnitude > locomotion.maxDestinationDrift * locomotion.maxDestinationDrift;
        bool intervalExpired = time >= nextPathRefreshTime;

        if (agent == null || !agent.enabled)
        {
            return;
        }

        if (!EnsureAgentOnNavMesh())
        {
            return;
        }

        if (!destinationChanged && !intervalExpired && agent.hasPath)
        {
            return;
        }

        destinationRequest = worldPoint;

        if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit hit, locomotion.destinationSampleRadius, navMeshAreaMask))
        {
            return;
        }

        currentDestination = hit.position;
        agent.SetDestination(currentDestination);
        nextPathRefreshTime = time + refreshInterval;
    }

    /// <summary>
    /// Ensures the NavMeshAgent is on a NavMesh before issuing nav calls.
    /// Attempts a small sample + warp to recover if slightly off mesh.
    /// </summary>
    /// <returns>true if the agent is on a NavMesh after this call.</returns>
    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, navMeshAreaMask))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private void ClearPath()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        desiredPlanarVelocity = Vector3.zero;
    }

    private Vector3 PredictPlayerPosition()
    {
        if (player == null)
        {
            return lastKnownPlayerPosition;
        }

        float predictionTime = locomotion.targetPredictionTime;
        Vector3 predictedOffset = playerVelocity * predictionTime;
        if (predictedOffset.sqrMagnitude > locomotion.maxPredictionDistance * locomotion.maxPredictionDistance)
        {
            predictedOffset = predictedOffset.normalized * locomotion.maxPredictionDistance;
        }

        return player.position + predictedOffset;
    }

    private void UpdateNavigation(float deltaTime)
    {
        if (agent == null || !agent.enabled || !EnsureAgentOnNavMesh())
        {
            return;
        }

        agent.nextPosition = transform.position;

        Vector3 navVelocity = agent.hasPath
            ? Vector3.ProjectOnPlane(agent.desiredVelocity, Vector3.up)
            : Vector3.zero;

        float targetSpeed = GetTargetSpeed();
        if (navVelocity.sqrMagnitude > 0.0001f)
        {
            navVelocity = navVelocity.normalized * targetSpeed;
        }

        Vector3 steering = ComputeSteering(navVelocity);
        desiredPlanarVelocity = Vector3.ClampMagnitude(steering, targetSpeed);
    }

    private Vector3 ComputeSteering(Vector3 navVelocity)
    {
        Vector3 result = navVelocity;

        if (state == MobState.Attack && player != null)
        {
            Vector3 attackVelocity = ComputeAttackVelocity();
            result = Vector3.Lerp(result, attackVelocity, attack.pursuitBlend);
        }

        if (flocking.enabled)
        {
            float flockWeight = GetFlockingWeightForState();
            if (flockWeight > 0.0001f)
            {
                Vector3 flockForce = ComputeFlockingForce(result) * flockWeight;
                result += flockForce;
            }
        }

        if (navVelocity.sqrMagnitude > 0.0001f && result.sqrMagnitude < navVelocity.sqrMagnitude * 0.25f)
        {
            result = Vector3.Lerp(result, navVelocity, 0.65f);
        }

        return result;
    }

    private Vector3 ComputeAttackVelocity()
    {
        Vector3 toPlayer = player.position - transform.position;
        Vector3 planarToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float distance = Mathf.Max(0.05f, planarToPlayer.magnitude);
        Vector3 direction = planarToPlayer / distance;

        float preferred = Mathf.Max(0.1f, attack.preferredDistance);
        float tolerance = Mathf.Max(0.1f, attack.distanceTolerance);
        float radialError = Mathf.Clamp((distance - preferred) / tolerance, -1f, 1f);

        Vector3 forward = direction * locomotion.baseSpeed * locomotion.attackSpeedMultiplier;
        Vector3 tangential = Vector3.Cross(Vector3.up, direction) * attack.orbitSpeed * orbitDirection;
        float proximityBoost = Mathf.Lerp(attack.orbitBoost, 1f, Mathf.Clamp01(distance / preferred));
        tangential *= proximityBoost;

        Vector3 radialCorrection = -direction * radialError * attack.radialSpringStrength;

        Vector3 attackVelocity = forward + tangential + radialCorrection;
        return attackVelocity;
    }

    private Vector3 ComputeFlockingForce(Vector3 baseVelocity)
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        float radius = flocking.neighborRadius;
        float radiusSqr = radius * radius;
        Vector3 mobPosition = transform.position;

        // Use OverlapSphereNonAlloc for performance
        int hitCount = Physics.OverlapSphereNonAlloc(mobPosition, radius, s_flockingColliderBuffer, flockingLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = s_flockingColliderBuffer[i];
            if (col.transform == transform || col.transform.IsChildOf(transform))
            {
                continue;
            }

            Mob other = col.GetComponentInParent<Mob>();
            
            if (other == null || other == this || !other.isAlive)
            {
                continue;
            }

            Vector3 offset = other.transform.position - mobPosition;
            Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
            float sqrDistance = planarOffset.sqrMagnitude;

            if (sqrDistance <= Mathf.Epsilon || sqrDistance > radiusSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            Vector3 direction = planarOffset / distance;

            separation -= direction / Mathf.Max(distance, 0.2f);
            alignment += other.currentPlanarVelocity;
            cohesion += other.transform.position;
            neighborCount++;
        }

        if (neighborCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 separationAvg = separation / neighborCount;
        Vector3 alignmentAvg = alignment / neighborCount;
        Vector3 cohesionAvg = (cohesion / neighborCount) - mobPosition;

        separationAvg = Vector3.ProjectOnPlane(separationAvg, Vector3.up);
        alignmentAvg = Vector3.ProjectOnPlane(alignmentAvg, Vector3.up);
        cohesionAvg = Vector3.ProjectOnPlane(cohesionAvg, Vector3.up);

        Vector3 force = Vector3.zero;

        if (separationAvg.sqrMagnitude > 0.0001f)
        {
            force += separationAvg.normalized * flocking.separationWeight;
        }

        if (alignmentAvg.sqrMagnitude > 0.0001f)
        {
            Vector3 baseDir = baseVelocity.sqrMagnitude > 0.0001f
                ? baseVelocity
                : transform.forward * locomotion.baseSpeed * 0.5f;
            float desiredSpeed = Mathf.Max(baseDir.magnitude, locomotion.baseSpeed * 0.5f);
            Vector3 desiredDirection = alignmentAvg.normalized * desiredSpeed;
            force += (desiredDirection - baseDir) * flocking.alignmentWeight;
        }

        if (cohesionAvg.sqrMagnitude > 0.0001f)
        {
            force += cohesionAvg.normalized * flocking.cohesionWeight;
        }

        float maxForce = Mathf.Max(1f, locomotion.baseSpeed * 0.6f);
        return Vector3.ClampMagnitude(force, maxForce);
    }

    private float GetFlockingWeightForState()
    {
        switch (state)
        {
            case MobState.Attack:
                return 0f;
            case MobState.Chase:
                return 0.25f;
            case MobState.BreakOff:
                return 0.4f;
            default:
                return 1f;
        }
    }

    private float GetTargetSpeed()
    {
        switch (state)
        {
            case MobState.Attack:
                return locomotion.baseSpeed * locomotion.attackSpeedMultiplier;
            case MobState.Chase:
                return locomotion.baseSpeed * locomotion.chaseSpeedMultiplier;
            case MobState.Patrol:
                return locomotion.baseSpeed * 0.9f;
            case MobState.BreakOff:
                return locomotion.baseSpeed;
            default:
                return locomotion.baseSpeed * 0.6f;
        }
    }

    private void ApplyMovement(float deltaTime)
    {
        Vector3 planarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            desiredPlanarVelocity,
            locomotion.acceleration * deltaTime);

        if (desiredPlanarVelocity.sqrMagnitude < 0.0001f)
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, locomotion.deceleration * deltaTime);
        }

        currentPlanarVelocity = Vector3.Lerp(
            planarVelocity,
            desiredPlanarVelocity,
            1f - Mathf.Exp(-locomotion.velocitySmoothing * deltaTime));

        if (knockbackVelocity.sqrMagnitude > 0f)
        {
            currentPlanarVelocity += knockbackVelocity;
            float decay = knockbackDecaySpeed * deltaTime;
            if (decay > 0f)
            {
                knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, decay);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }

            if (knockbackVelocity.sqrMagnitude <= 0.0001f)
            {
                knockbackVelocity = Vector3.zero;
                knockbackDecaySpeed = 0f;
            }
        }

        if (body.isKinematic)
        {
            Vector3 displacement = currentPlanarVelocity * deltaTime;
            if (TryClampKinematicDisplacement(displacement, out Vector3 resolvedDisplacement, out RaycastHit sweepHit))
            {
                displacement = resolvedDisplacement;

                if (sweepHit.normal.sqrMagnitude > 0.0001f)
                {
                    currentPlanarVelocity = Vector3.ProjectOnPlane(currentPlanarVelocity, sweepHit.normal);
                    desiredPlanarVelocity = Vector3.ProjectOnPlane(desiredPlanarVelocity, sweepHit.normal);
                    knockbackVelocity = Vector3.ProjectOnPlane(knockbackVelocity, sweepHit.normal);
                }
            }
            Vector3 candidate = transform.position + displacement;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, locomotion.destinationSampleRadius, navMeshAreaMask))
            {
                candidate.y = Mathf.Lerp(transform.position.y, hit.position.y, 0.35f);
            }

            body.MovePosition(candidate);
            return;
        }

        Vector3 existingVelocity = body.linearVelocity;
        Vector3 newVelocity = new Vector3(currentPlanarVelocity.x, existingVelocity.y, currentPlanarVelocity.z);
        body.linearVelocity = newVelocity;

        if (body.useGravity)
        {
            body.AddForce(Vector3.down * locomotion.gravityCompensation, ForceMode.Acceleration);
        }
    }

    private void UpdateFacing(float deltaTime)
    {
        Vector3 facingVector = desiredPlanarVelocity;

        if (facingVector.sqrMagnitude < 0.05f && player != null)
        {
            facingVector = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
        }

        if (facingVector.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(facingVector.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, locomotion.turnRate * deltaTime);
    }

    private bool TryClampKinematicDisplacement(Vector3 displacement, out Vector3 resolvedDisplacement, out RaycastHit sweepHit)
    {
        resolvedDisplacement = displacement;
        sweepHit = default;

        if (body == null)
        {
            return false;
        }

        float distance = displacement.magnitude;
        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 direction = displacement / distance;
        if (body.SweepTest(direction, out sweepHit, distance, QueryTriggerInteraction.Ignore))
        {
            float allowedDistance = Mathf.Max(0f, sweepHit.distance - KinematicCollisionSkin);
            resolvedDisplacement = direction * allowedDistance;
            return true;
        }

        return false;
    }

    public void ApplyImpact(Vector3 direction, float strength, float fadeDuration = 0.35f)
    {
        if (!isAlive || strength <= 0f)
        {
            return;
        }

        Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planar.sqrMagnitude < 0.01f)
        {
            planar = transform.forward;
        }

        planar.Normalize();
        Vector3 impulse = planar * strength;

        knockbackVelocity += impulse;
        float duration = Mathf.Max(fadeDuration, 0.05f);
        float decay = impulse.magnitude / duration;
        knockbackDecaySpeed = decay;
    }

    #endregion
}
