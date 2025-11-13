using UnityEngine;
using UnityEngine.AI;

public partial class Mob
{
    #region State Machine

    private void UpdateStateMachine(float deltaTime)
    {
        stateTimer += deltaTime;

        switch (state)
        {
            case MobState.Idle:
                TickIdle();
                break;
            case MobState.Patrol:
                TickPatrol();
                break;
            case MobState.Chase:
                TickChase();
                break;
            case MobState.Attack:
                TickAttack(deltaTime);
                break;
            case MobState.BreakOff:
                TickBreakOff();
                break;
        }
    }

    private void TickIdle()
    {
        if (PlayerIsVisible())
        {
            TransitionTo(MobState.Chase);
            return;
        }

        if (!patrol.enabled)
        {
            desiredPlanarVelocity = Vector3.zero;
            if (stateTimer >= idleDuration)
            {
                idleDuration = GetNextIdleDuration();
                stateTimer = 0f;
            }
            return;
        }

        if (stateTimer >= idleDuration)
        {
            TransitionTo(MobState.Patrol);
        }
    }

    private void TickPatrol()
    {
        if (PlayerIsVisible())
        {
            TransitionTo(MobState.Chase);
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= patrol.tolerance)
        {
            Vector3 patrolPoint;
            if (TryPickPatrolDestination(out patrolPoint))
            {
                RequestDestination(patrolPoint, locomotion.basePathRefreshInterval);
            }
            else
            {
                TransitionTo(MobState.Idle);
            }
        }
    }

    private void TickChase()
    {
        if (player == null)
        {
            TransitionTo(MobState.BreakOff);
            return;
        }

        bool canSeePlayer = PlayerIsVisible();
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attack.range * 1.1f)
        {
            TransitionTo(MobState.Attack);
            return;
        }

        if (!canSeePlayer && Time.time - lastSeenPlayerTime > perception.lostSightGrace)
        {
            TransitionTo(MobState.BreakOff);
            return;
        }

        Vector3 predicted = PredictPlayerPosition();
        RequestDestination(predicted, locomotion.chasePathRefreshInterval);
    }

    private void TickAttack(float deltaTime)
    {
        if (player == null)
        {
            TransitionTo(MobState.BreakOff);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = PlayerIsVisible();

        if (!canSeePlayer && Time.time - lastSeenPlayerTime > perception.lostSightGrace)
        {
            TransitionTo(MobState.BreakOff);
            return;
        }

        if (distanceToPlayer > attack.range * 1.35f)
        {
            TransitionTo(MobState.Chase);
            return;
        }

        Vector3 predicted = PredictPlayerPosition();
        RequestDestination(predicted, locomotion.attackPathRefreshInterval);

        TryAttackPlayer(distanceToPlayer, deltaTime);
    }

    private void TickBreakOff()
    {
        if (PlayerIsVisible())
        {
            TransitionTo(MobState.Chase);
            return;
        }

        if (!float.IsNegativeInfinity(lastSeenPlayerTime))
        {
            RequestDestination(lastKnownPlayerPosition, locomotion.basePathRefreshInterval);
            float remaining = agent.remainingDistance;
            if (!agent.hasPath || remaining <= patrol.tolerance)
            {
                lastSeenPlayerTime = float.NegativeInfinity;
                TransitionTo(patrol.enabled ? MobState.Patrol : MobState.Idle);
            }
        }
        else
        {
            TransitionTo(patrol.enabled ? MobState.Patrol : MobState.Idle);
        }
    }

    private void TransitionTo(MobState newState)
    {
        if (state == newState)
        {
            stateTimer = 0f;
            return;
        }

        state = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case MobState.Idle:
                idleDuration = GetNextIdleDuration();
                ClearPath();
                break;
            case MobState.Patrol:
                idleDuration = GetNextIdleDuration();
                if (!agent.hasPath)
                {
                    Vector3 patrolPoint;
                    if (TryPickPatrolDestination(out patrolPoint))
                    {
                        RequestDestination(patrolPoint, locomotion.basePathRefreshInterval);
                    }
                }
                break;
            case MobState.Chase:
                agent.speed = locomotion.baseSpeed * locomotion.chaseSpeedMultiplier;
                break;
            case MobState.Attack:
                agent.speed = locomotion.baseSpeed * locomotion.attackSpeedMultiplier;
                break;
            case MobState.BreakOff:
                agent.speed = locomotion.baseSpeed * 0.9f;
                break;
        }
    }

    private float GetNextIdleDuration()
    {
        return Mathf.Max(0.5f, UnityEngine.Random.Range(patrol.idleTimeRange.x, patrol.idleTimeRange.y));
    }

    #endregion
}
