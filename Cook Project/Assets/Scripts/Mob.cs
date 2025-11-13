using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// High level mob brain that handles perception, navigation, combat, and health.
/// Navigation is driven through a NavMeshAgent that feeds a custom locomotion controller so
/// mobs continue moving even while adjusting their path or attacking.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class Mob : MonoBehaviour
{
    #region Inspector Configuration

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private MobHealthBarController healthBarController;

    [Serializable]
    private class LocomotionSettings
    {
        [Header("Speed")]
        public float baseSpeed = 3.5f;
        [Tooltip("Multiplier applied while chasing the player.")]
        public float chaseSpeedMultiplier = 1.15f;
        [Tooltip("Multiplier applied while attacking, keeps forward momentum up close.")]
        public float attackSpeedMultiplier = 1.25f;
        [Header("Motion Tuning")]
        public float acceleration = 18f;
        public float deceleration = 22f;
        [Tooltip("Degrees per second the mob can rotate while in motion.")]
        public float turnRate = 540f;
        [Tooltip("Damping used when blending towards the desired planar velocity.")]
        public float velocitySmoothing = 12f;
        [Tooltip("How strongly to keep mobs glued to the ground.")]
        public float gravityCompensation = 2f;

        [Header("Navigation Sampling")]
        [Min(0.1f)] public float destinationSampleRadius = 1.25f;
        [Tooltip("If the cached path endpoint drifts further than this (meters), rebuild immediately.")]
        [Min(0.05f)] public float maxDestinationDrift = 0.75f;
        [Tooltip("Time between path rebuilds for non-combat states.")]
        [Min(0.05f)] public float basePathRefreshInterval = 0.45f;
        [Tooltip("Time between path rebuilds for chase state.")]
        [Min(0.05f)] public float chasePathRefreshInterval = 0.18f;
        [Tooltip("Time between path rebuilds for attack state.")]
        [Min(0.05f)] public float attackPathRefreshInterval = 0.1f;
        [Tooltip("Maximum lookahead time (seconds) when predicting the player's future position.")]
        [Min(0f)] public float targetPredictionTime = 0.35f;
        [Tooltip("Cap on predicted offset distance to avoid overshooting corners.")]
        [Min(0.1f)] public float maxPredictionDistance = 6f;
    }

    [Serializable]
    private class AttackSettings
    {
        public float range = 2f;
        public float preferredDistance = 1.5f;
        public float distanceTolerance = 0.6f;
        [Tooltip("Forward pursuit weight when blending nav velocity with attack orbiting.")]
        [Range(0f, 1f)] public float pursuitBlend = 0.55f;
        [Tooltip("Speed (meters/sec) used for orbiting around the player.")]
        public float orbitSpeed = 2.6f;
        [Tooltip("Additional tangential boost applied based on proximity to the preferred distance.")]
        public float orbitBoost = 1.1f;
        [Tooltip("Radial correction factor helping mobs maintain preferred distance during orbit.")]
        public float radialSpringStrength = 3f;
        public float damage = 10f;
        public float cooldown = 1.3f;
    }

    [Serializable]
    private class PerceptionSettings
    {
        public float detectionRange = 30f;
        [Range(0f, 180f)] public float fieldOfView = 160f;
        public float lostSightGrace = 1.6f;
        public LayerMask obstacleLayer = Physics.DefaultRaycastLayers;
        [Tooltip("Layers considered valid targets. Leave empty to auto-detect from the assigned player's colliders.")]
        public LayerMask targetLayers = 0;
    }

    [Serializable]
    private class PatrolSettings
    {
        public bool enabled = true;
        public Vector2 idleTimeRange = new Vector2(1.5f, 3f);
        [Min(0.5f)] public float radius = 6f;
        [Min(0.1f)] public float tolerance = 0.75f;
    }

    [Serializable]
    private class HealthSettings
    {
        public int maxHealth = 50;
    }

    [Serializable]
    private class DeathSettings
    {
        public float despawnDelay = 2f;
        public GameObject deathParticles = null;
        public Vector3 particleOffset = Vector3.zero;
        public bool autoDestroyParticles = true;
        [Header("Souls")]
        [Min(0)] public int soulReward = 0;
    }

    [Serializable]
    private class LightDamageSettings
    {
        public float attackLifetimeReduction = 2f;
        public float searchRadius = 15f;
    }

    [Serializable]
    private class FlockingSettings
    {
        public bool enabled = true;
        [Min(0.5f)] public float neighborRadius = 3f;
        public float separationWeight = 2.2f;
        public float alignmentWeight = 0.7f;
        public float cohesionWeight = 0.35f;
    }

    [SerializeField] private LocomotionSettings locomotion = new LocomotionSettings();
    [SerializeField] private AttackSettings attack = new AttackSettings();
    [SerializeField] private PerceptionSettings perception = new PerceptionSettings();
    [SerializeField] private PatrolSettings patrol = new PatrolSettings();
    [SerializeField] private HealthSettings health = new HealthSettings();
    [SerializeField] private DeathSettings death = new DeathSettings();
    [SerializeField] private LightDamageSettings lightDamage = new LightDamageSettings();
    [SerializeField] private FlockingSettings flocking = new FlockingSettings();

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool drawGizmos = false;

    #endregion

    #region Public Accessors

    public Transform PlayerTransform => player;

    #endregion

    #region State

    private enum MobState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        BreakOff
    }

    private static readonly List<Mob> ActiveMobs = new List<Mob>();
    private Rigidbody body;
    private NavMeshAgent agent;
    private bool isAlive = true;
    private MobState state = MobState.Idle;
    private float stateTimer;
    private float idleDuration;
    private float lastAttackTime = -999f;
    private float lastSeenPlayerTime = float.NegativeInfinity;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 currentDestination;
    private Vector3 destinationRequest;
    private float nextPathRefreshTime;
    private Vector3 desiredPlanarVelocity;
    private Vector3 currentPlanarVelocity;
    private Vector3 playerVelocity;
    private Vector3 previousPlayerPosition;
    private float previousPlayerSampleTime;
    private int orbitDirection;
    private Vector3 spawnPosition;
    private int cachedPlayerLayerMask;
    private int cachedPlayerInstanceId;
    private readonly List<Collider> cachedPlayerColliders = new List<Collider>(8);
    private Vector3 cachedPlayerScale = new Vector3(float.NaN, float.NaN, float.NaN);
    private float nextPlayerLayerRefreshTime;

    private Vector3 knockbackVelocity;
    private float knockbackDecaySpeed;

    private int currentHealth;
    private bool hasRegistered;
    private int navMeshAreaMask = NavMesh.AllAreas;

    private const float KinematicCollisionSkin = 0.02f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        ConfigureRigidbody();
        ConfigureAgent();

        spawnPosition = transform.position;
        currentHealth = health.maxHealth;
        orbitDirection = UnityEngine.Random.value < 0.5f ? 1 : -1;
        idleDuration = GetNextIdleDuration();
        previousPlayerPosition = Vector3.positiveInfinity;
        previousPlayerSampleTime = Time.time;

        TryAutoAssignReferences();
    }

    private void OnEnable()
    {
        if (!hasRegistered)
        {
            ActiveMobs.Add(this);
            hasRegistered = true;
        }
    }

    private void OnDisable()
    {
        if (hasRegistered)
        {
            ActiveMobs.Remove(this);
            hasRegistered = false;
        }
    }

    private void Start()
    {
        TryAutoAssignPlayer();
        EnsurePlayerLayerMaskUpToDate(true);
        if (healthBarController == null)
        {
            healthBarController = GetComponent<MobHealthBarController>();
        }
    }

    private void Update()
    {
        if (!isAlive)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        SamplePlayerMotion(deltaTime);
        UpdatePerception(deltaTime);
        UpdateStateMachine(deltaTime);
        UpdateNavigation(deltaTime);
    }

    private void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        ApplyMovement(deltaTime);
        UpdateFacing(deltaTime);
    }

    #endregion

    #region Initialization Helpers

    private void ConfigureRigidbody()
    {
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.useGravity = false;
        body.detectCollisions = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.isKinematic = true;
    }

    private void ConfigureAgent()
    {
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = locomotion.baseSpeed;
        agent.acceleration = locomotion.acceleration;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        agent.autoRepath = true;
        agent.angularSpeed = locomotion.turnRate;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    private void TryAutoAssignReferences()
    {
        if (healthBarController == null)
        {
            healthBarController = GetComponent<MobHealthBarController>();
        }
    }

    private void TryAutoAssignPlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            EnsurePlayerLayerMaskUpToDate(true);
        }
        else if (showDebug)
        {
            Debug.LogWarning("Mob: Unable to find player with tag 'Player'. Assign target manually.", this);
        }
    }

    #endregion

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

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, combinedMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;

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

    #endregion

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

    #region Navigation & Movement

    private void RequestDestination(Vector3 worldPoint, float refreshInterval)
    {
        float time = Time.time;
        bool destinationChanged = (worldPoint - destinationRequest).sqrMagnitude > locomotion.maxDestinationDrift * locomotion.maxDestinationDrift;
        bool intervalExpired = time >= nextPathRefreshTime;

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

    private void ClearPath()
    {
        if (agent != null)
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
        if (agent == null || !agent.enabled)
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
        if (ActiveMobs.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        float radius = flocking.neighborRadius;
        float radiusSqr = radius * radius;

        for (int i = 0; i < ActiveMobs.Count; i++)
        {
            Mob other = ActiveMobs[i];
            if (other == null || other == this || !other.isAlive)
            {
                continue;
            }

            Vector3 offset = other.transform.position - transform.position;
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

        Vector3 separationAvg = separation / Mathf.Max(1, neighborCount);
        Vector3 alignmentAvg = alignment / Mathf.Max(1, neighborCount);
        Vector3 cohesionAvg = (cohesion / Mathf.Max(1, neighborCount)) - transform.position;

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

    #endregion

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

    #region Combat & Damage

    private void TryAttackPlayer(float distanceToPlayer, float deltaTime)
    {
        if (Time.time - lastAttackTime < attack.cooldown)
        {
            return;
        }

        if (distanceToPlayer > attack.range)
        {
            return;
        }

        lastAttackTime = Time.time;

        PlayerStatSystem playerStats = PlayerStatSystem.Instance;
        if (playerStats != null)
        {
            int currentHP = playerStats.CurrentHP.Value;
            int damage = Mathf.RoundToInt(attack.damage);
            int newHP = Mathf.Max(0, currentHP - damage);
            playerStats.Damage(damage);

            if (showDebug)
            {
                Debug.Log($"Mob attacked player for {damage} damage. Player HP: {newHP}/{playerStats.MaxHP.Value}", this);
            }
        }

        DamageNearestLightSource();
    }

    public void TakeDamage(int amount)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (healthBarController != null)
        {
            healthBarController.FlashDamage();
        }

        if (showDebug)
        {
            Debug.Log($"Mob took {amount} damage. HP: {currentHealth}/{health.maxHealth}", this);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        ClearPath();
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        agent.enabled = false;

        SpawnDeathParticles();
        AwardSouls();
        enabled = false;

        Destroy(gameObject, death.despawnDelay);
    }

    private void SpawnDeathParticles()
    {
        if (death.deathParticles == null)
        {
            return;
        }

        Vector3 position = transform.position + death.particleOffset;
        GameObject particle = Instantiate(death.deathParticles, position, Quaternion.identity);

        if (death.autoDestroyParticles)
        {
            ParticleSystem ps = particle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(particle, ps.main.duration + ps.main.startLifetimeMultiplier);
            }
            else
            {
                Destroy(particle, 5f);
            }
        }
    }

    private void AwardSouls()
    {
        if (death.soulReward <= 0)
        {
            return;
        }

        PlayerStatSystem playerStats = PlayerStatSystem.Instance;
        if (playerStats == null)
        {
            return;
        }

        playerStats.AddSouls(death.soulReward);

        if (showDebug)
        {
            Debug.Log($"Mob awarded {death.soulReward} souls. Player Souls: {playerStats.CurrentSouls.Value}/{playerStats.MaxSouls.Value}", this);
        }
    }

    public void DamageNearestLightSource()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        if (allLights == null || allLights.Length == 0)
        {
            return;
        }

        TimedObjectDestroyer closest = null;
        Light closestLight = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < allLights.Length; i++)
        {
            Light light = allLights[i];
            if (!light.enabled)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, light.transform.position);
            if (distance > lightDamage.searchRadius || distance >= closestDistance)
            {
                continue;
            }

            TimedObjectDestroyer destroyer = light.GetComponent<TimedObjectDestroyer>();
            if (destroyer == null)
            {
                destroyer = light.GetComponentInParent<TimedObjectDestroyer>();
            }

            if (destroyer == null)
            {
                continue;
            }

            closest = destroyer;
            closestLight = light;
            closestDistance = distance;
        }

        if (closest == null)
        {
            return;
        }

        closest.lifeTime = Mathf.Max(0.1f, closest.lifeTime - lightDamage.attackLifetimeReduction);

        if (closest.lifeTime < 1f && closestLight != null)
        {
            StartCoroutine(FlickerLight(closestLight));
        }
    }

    private System.Collections.IEnumerator FlickerLight(Light light)
    {
        if (light == null)
        {
            yield break;
        }

        float originalIntensity = light.intensity;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (light == null)
            {
                yield break;
            }

            light.intensity = originalIntensity * UnityEngine.Random.Range(0.5f, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (light != null)
        {
            light.intensity = originalIntensity;
        }
    }

    #endregion

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

    #region Patrol

    private bool TryPickPatrolDestination(out Vector3 destination)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * patrol.radius;
            Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, locomotion.destinationSampleRadius, navMeshAreaMask))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    #endregion

    #region Utility

    private static bool Approximately(Vector3 a, Vector3 b, float tolerance = 0.001f)
    {
        if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z))
        {
            return false;
        }

        if (float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
        {
            return false;
        }

        return (a - b).sqrMagnitude <= tolerance * tolerance;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attack.range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perception.detectionRange);

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }

    #endregion
}
