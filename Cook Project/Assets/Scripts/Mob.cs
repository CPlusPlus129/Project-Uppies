using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Main mob component that handles AI, movement towards player, and health management.
/// Takes damage from bright lights (opposite of player).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Mob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody rb;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [Tooltip("How close the mob needs to be to attack")]
    [SerializeField] private float attackRange = 2f;

    [Header("Navigation")]
    [Tooltip("Seconds between NavMesh path recalculations while chasing the player.")]
    [SerializeField, Min(0.05f)] private float pathRecalculationInterval = 0.4f;
    [Tooltip("How close the mob must get to a path corner before advancing to the next one.")]
    [SerializeField, Min(0.05f)] private float cornerArrivalThreshold = 0.35f;
    [Tooltip("Sampling radius used when projecting the mob and player onto the NavMesh surface.")]
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1.5f;
    [Tooltip("NavMesh area mask to use when building paths.")]
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
    
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;
    
    [Header("Death")]
    [Tooltip("Time in seconds before the mob despawns after dying")]
    [SerializeField] private float despawnDelay = 2f;
    [Tooltip("Particle effect to spawn when the mob dies (optional)")]
    [SerializeField] private GameObject deathParticlePrefab;
    [Tooltip("Offset from mob position where particles spawn")]
    [SerializeField] private Vector3 particleOffset = Vector3.zero;
    [Tooltip("If true, particle system will auto-destroy after playing. If false, you must handle cleanup.")]
    [SerializeField] private bool autoDestroyParticles = true;
    
    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    
    [Header("Light Source Damage")]
    [Tooltip("Time in seconds to reduce from nearest light source when attacking")]
    [SerializeField] private float lightLifetimeReduction = 2f;
    [Tooltip("Maximum range to search for light sources to damage")]
    [SerializeField] private float lightDamageRange = 15f;
    
    [Header("Detection")]
    [SerializeField] private float playerDetectionRange = 30f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Behavior")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private Vector2 idleDurationRange = new Vector2(1.5f, 3f);
    [SerializeField, Min(0.5f)] private float patrolRadius = 6f;
    [SerializeField, Min(0.1f)] private float patrolPointTolerance = 0.6f;
    [SerializeField, Min(0.1f)] private float regroupDuration = 2.5f;
    [SerializeField, Min(0.1f)] private float lostSightGracePeriod = 1.5f;

    [Header("Flocking")]
    [SerializeField] private bool enableFlocking = true;
    [SerializeField, Min(0.5f)] private float neighborRadius = 3f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 0.5f;
    [SerializeField, Min(0.05f)] private float steeringUpdateInterval = 0.25f;
    [SerializeField] private float maxSteeringForce = 4f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // State
    private enum MobState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Regroup
    }

    private bool isAlive = true;
    private bool hasDetectedPlayer = false;
    private Vector3 targetPosition;
    private NavMeshPath navMeshPath;
    private int currentPathCornerIndex;
    private float lastPathUpdateTime = float.NegativeInfinity;
    private bool hasNavMeshPath;
    private Vector3 desiredNavDestination;
    private bool navMeshPathIsPartial;
    private static readonly List<Mob> ActiveMobs = new List<Mob>();
    private MobState currentState = MobState.Idle;
    private float stateTimer;
    private float idleDuration;
    private Vector3 spawnPosition;
    private Vector3 patrolDestination;
    private Vector3 regroupDestination;
    private Vector3 lastKnownPlayerPosition;
    private float lastSeenPlayerTime = float.NegativeInfinity;
    private Vector3 steeringVelocity;
    private float nextSteeringSampleTime;
    
    // Optional health bar reference (cached)
    private MobHealthBarController healthBarController;
    
    // Public properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => isAlive;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    
    private void Awake()
    {
        // Get Rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // Configure Rigidbody for ground movement
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        
        currentHealth = maxHealth;
        navMeshPath = new NavMeshPath();
        targetPosition = transform.position;
        desiredNavDestination = transform.position;
        spawnPosition = transform.position;
        steeringVelocity = Vector3.zero;
        nextSteeringSampleTime = Time.time;

        if (!ActiveMobs.Contains(this))
        {
            ActiveMobs.Add(this);
        }

        SetState(MobState.Idle, resetPath: true, logStateChange: false);
        
        // Try to find health bar controller (optional)
        healthBarController = GetComponent<MobHealthBarController>();
    }

    private void OnValidate()
    {
        if (idleDurationRange.y < idleDurationRange.x)
        {
            idleDurationRange.y = idleDurationRange.x;
        }

        neighborRadius = Mathf.Max(0.5f, neighborRadius);
        steeringUpdateInterval = Mathf.Max(0.05f, steeringUpdateInterval);
        patrolPointTolerance = Mathf.Max(0.1f, patrolPointTolerance);
        regroupDuration = Mathf.Max(0.1f, regroupDuration);
        lostSightGracePeriod = Mathf.Max(0.1f, lostSightGracePeriod);
    }
    
    private void Start()
    {
        // Try to find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Mob: No player found! Make sure player has 'Player' tag.", this);
            }
        }
    }
    
    private void Update()
    {
        if (!isAlive)
        {
            return;
        }

        UpdateStateMachine();
    }
    
    private void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }

        UpdateSteering();
        MoveTowardsTarget();
    }
    
    private void UpdateStateMachine()
    {
        stateTimer += Time.deltaTime;

        bool playerAvailable = player != null;
        float distanceToPlayer = float.MaxValue;
        bool playerVisible = false;

        if (playerAvailable)
        {
            distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= playerDetectionRange && HasLineOfSight(player.position))
            {
                playerVisible = true;
                lastKnownPlayerPosition = player.position;
                lastSeenPlayerTime = Time.time;
            }
        }

        switch (currentState)
        {
            case MobState.Idle:
                HandleIdleState(playerVisible);
                break;
            case MobState.Patrol:
                HandlePatrolState(playerVisible);
                break;
            case MobState.Chase:
                HandleChaseState(playerVisible, playerAvailable, distanceToPlayer);
                break;
            case MobState.Attack:
                HandleAttackState(playerVisible, playerAvailable, distanceToPlayer);
                break;
            case MobState.Regroup:
                HandleRegroupState(playerVisible);
                break;
        }

        hasDetectedPlayer = currentState == MobState.Chase || currentState == MobState.Attack;
    }

    private void HandleIdleState(bool playerVisible)
    {
        if (playerVisible)
        {
            SetState(MobState.Chase, resetPath: true);
            return;
        }

        if (!enablePatrol)
        {
            return;
        }

        if (stateTimer >= idleDuration)
        {
            SetState(MobState.Patrol, resetPath: true);
        }
    }

    private void HandlePatrolState(bool playerVisible)
    {
        if (playerVisible)
        {
            SetState(MobState.Chase, resetPath: true);
            return;
        }

        if (!hasNavMeshPath)
        {
            if (!EnsureNavMeshPath(patrolDestination, forceRebuild: true, destinationIsPlayer: false))
            {
                if (!TrySelectPatrolDestination(out patrolDestination) ||
                    !EnsureNavMeshPath(patrolDestination, forceRebuild: true, destinationIsPlayer: false))
                {
                    SetState(MobState.Idle, resetPath: true);
                    return;
                }
            }
        }

        bool forceRebuild = Time.time - lastPathUpdateTime >= pathRecalculationInterval;
        if (!EnsureNavMeshPath(patrolDestination, forceRebuild, destinationIsPlayer: false))
        {
            SetState(MobState.Idle, resetPath: true);
            return;
        }

        float distanceToDestination = Vector3.Distance(transform.position, patrolDestination);
        if (distanceToDestination <= Mathf.Max(stoppingDistance, patrolPointTolerance))
        {
            SetState(MobState.Idle, resetPath: true);
        }
    }

    private void HandleChaseState(bool playerVisible, bool playerAvailable, float distanceToPlayer)
    {
        if (!playerAvailable)
        {
            TransitionToPostChaseState();
            return;
        }

        if (!playerVisible && Time.time - lastSeenPlayerTime > lostSightGracePeriod)
        {
            TransitionToPostChaseState();
            return;
        }

        bool forceRebuild = Time.time - lastPathUpdateTime >= pathRecalculationInterval ||
                            (desiredNavDestination - player.position).sqrMagnitude > navMeshSampleRadius * navMeshSampleRadius;

        if (!EnsureNavMeshPath(player.position, forceRebuild, destinationIsPlayer: true))
        {
            TransitionToPostChaseState();
            return;
        }

        if (navMeshPathIsPartial)
        {
            lastKnownPlayerPosition = navMeshPath.corners[navMeshPath.corners.Length - 1];
            TransitionToPostChaseState();
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            SetState(MobState.Attack, resetPath: false);
        }
    }

    private void HandleAttackState(bool playerVisible, bool playerAvailable, float distanceToPlayer)
    {
        if (!playerAvailable)
        {
            TransitionToPostChaseState();
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            TryAttackPlayer();
        }

        bool forceRebuild = Time.time - lastPathUpdateTime >= pathRecalculationInterval ||
                            (desiredNavDestination - player.position).sqrMagnitude > navMeshSampleRadius * navMeshSampleRadius;
        if (!EnsureNavMeshPath(player.position, forceRebuild, destinationIsPlayer: true))
        {
            TransitionToPostChaseState();
            return;
        }

        if (navMeshPathIsPartial)
        {
            lastKnownPlayerPosition = navMeshPath.corners[navMeshPath.corners.Length - 1];
            TransitionToPostChaseState();
            return;
        }

        float disengageDistance = attackRange + Mathf.Max(0.5f, stoppingDistance * 0.25f);
        if (distanceToPlayer > disengageDistance)
        {
            SetState(MobState.Chase, resetPath: false);
            return;
        }

        if (!playerVisible && Time.time - lastSeenPlayerTime > lostSightGracePeriod)
        {
            TransitionToPostChaseState();
        }
    }

    private void HandleRegroupState(bool playerVisible)
    {
        if (playerVisible)
        {
            SetState(MobState.Chase, resetPath: true);
            return;
        }

        if (!HasLastKnownPlayerPosition())
        {
            SetState(enablePatrol ? MobState.Patrol : MobState.Idle, resetPath: true);
            return;
        }

        bool forceRebuild = Time.time - lastPathUpdateTime >= pathRecalculationInterval ||
                            (desiredNavDestination - regroupDestination).sqrMagnitude > navMeshSampleRadius * navMeshSampleRadius;

        if (!EnsureNavMeshPath(regroupDestination, forceRebuild, destinationIsPlayer: false))
        {
            lastSeenPlayerTime = float.NegativeInfinity;
            SetState(enablePatrol ? MobState.Patrol : MobState.Idle, resetPath: true);
            return;
        }

        float distanceToRegroup = Vector3.Distance(transform.position, regroupDestination);
        if (distanceToRegroup <= Mathf.Max(stoppingDistance, patrolPointTolerance) || stateTimer >= regroupDuration)
        {
            lastSeenPlayerTime = float.NegativeInfinity;
            SetState(enablePatrol ? MobState.Patrol : MobState.Idle, resetPath: true);
        }
    }

    private void TransitionToPostChaseState()
    {
        if (HasLastKnownPlayerPosition())
        {
            regroupDestination = lastKnownPlayerPosition;
            SetState(MobState.Regroup, resetPath: true);
        }
        else
        {
            lastSeenPlayerTime = float.NegativeInfinity;
            SetState(enablePatrol ? MobState.Patrol : MobState.Idle, resetPath: true);
        }
    }

    private bool HasLastKnownPlayerPosition()
    {
        return !float.IsNegativeInfinity(lastSeenPlayerTime);
    }

    private void SetState(MobState newState, bool resetPath, bool logStateChange = true)
    {
        if (currentState == newState)
        {
            stateTimer = 0f;
            if (resetPath)
            {
                ClearPath();
            }
            OnEnterState(newState);
            return;
        }

        currentState = newState;
        stateTimer = 0f;

        if (resetPath)
        {
            ClearPath();
        }

        if (showDebugInfo && logStateChange)
        {
            Debug.Log($"Mob state changed to {currentState}", this);
        }

        OnEnterState(newState);
    }

    private void OnEnterState(MobState state)
    {
        switch (state)
        {
            case MobState.Idle:
                idleDuration = enablePatrol ? Mathf.Max(0.25f, Random.Range(idleDurationRange.x, idleDurationRange.y)) : float.PositiveInfinity;
                break;
            case MobState.Patrol:
                if (!TrySelectPatrolDestination(out patrolDestination))
                {
                    SetState(MobState.Idle, resetPath: true, logStateChange: false);
                }
                break;
            case MobState.Chase:
                break;
            case MobState.Attack:
                break;
            case MobState.Regroup:
                if (!HasLastKnownPlayerPosition())
                {
                    SetState(enablePatrol ? MobState.Patrol : MobState.Idle, resetPath: true, logStateChange: false);
                    return;
                }

                regroupDestination = lastKnownPlayerPosition;
                EnsureNavMeshPath(regroupDestination, forceRebuild: true, destinationIsPlayer: false);
                break;
        }
    }

    private bool TrySelectPatrolDestination(out Vector3 destination)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, navMeshAreaMask))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = spawnPosition;
        return false;
    }

    private void UpdateSteering()
    {
        if (!enableFlocking || ActiveMobs.Count <= 1)
        {
            steeringVelocity = Vector3.zero;
            return;
        }

        if (Time.time < nextSteeringSampleTime)
        {
            return;
        }

        nextSteeringSampleTime = Time.time + steeringUpdateInterval;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        float neighborRadiusSqr = neighborRadius * neighborRadius;

        foreach (Mob other in ActiveMobs)
        {
            if (other == null || other == this || !other.isAlive)
            {
                continue;
            }

            Vector3 offset = other.transform.position - transform.position;
            float sqrDistance = offset.x * offset.x + offset.z * offset.z;

            if (sqrDistance <= Mathf.Epsilon || sqrDistance > neighborRadiusSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            Vector3 horizontalOffset = new Vector3(offset.x, 0f, offset.z);
            Vector3 directionToOther = horizontalOffset / distance;

            separation -= directionToOther / Mathf.Max(distance, 0.1f);

            if (other.rb != null)
            {
                alignment += Vector3.ProjectOnPlane(other.rb.linearVelocity, Vector3.up);
            }

            cohesion += other.transform.position;
            neighborCount++;
        }

        if (neighborCount == 0)
        {
            steeringVelocity = Vector3.zero;
            return;
        }

        Vector3 alignmentForce = alignment / neighborCount;
        Vector3 cohesionVector = ((cohesion / neighborCount) - transform.position);
        cohesionVector.y = 0f;
        Vector3 combined = Vector3.zero;

        if (separation.sqrMagnitude > 0.0001f)
        {
            combined += separation.normalized * separationWeight;
        }

        if (alignmentForce.sqrMagnitude > 0.0001f)
        {
            Vector3 alignmentDir = alignmentForce.normalized * moveSpeed;
            alignmentDir -= Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            combined += alignmentDir * alignmentWeight;
        }

        if (cohesionVector.sqrMagnitude > 0.0001f)
        {
            combined += cohesionVector.normalized * cohesionWeight;
        }

        combined = Vector3.ClampMagnitude(combined, maxSteeringForce);

        steeringVelocity = Vector3.Lerp(steeringVelocity, combined, 0.6f);
    }
    
    private bool HasLineOfSight(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        float distance = direction.magnitude;
        
        // Simple raycast check - can be enhanced with more sophisticated detection
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction.normalized, out RaycastHit hit, distance, obstacleLayer))
        {
            // Hit an obstacle before reaching player
            return false;
        }
        
        return true;
    }
    
    private void MoveTowardsTarget()
    {
        bool stateRequiresNavigation = currentState == MobState.Patrol ||
                                       currentState == MobState.Chase ||
                                       currentState == MobState.Attack ||
                                       currentState == MobState.Regroup;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(steeringVelocity, Vector3.up);

        if (stateRequiresNavigation && hasNavMeshPath)
        {
            AdvancePathCornerIfNeeded();

            Vector3 planarTarget = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
            Vector3 toTarget = planarTarget - transform.position;
            float distanceToTarget = toTarget.magnitude;

            if (distanceToTarget > Mathf.Epsilon)
            {
                Vector3 direction = toTarget / distanceToTarget;
                direction.y = 0f;

                if (distanceToTarget > stoppingDistance)
                {
                    planarVelocity += direction * moveSpeed;
                }
            }
        }

        if (!stateRequiresNavigation && planarVelocity.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        planarVelocity = Vector3.ClampMagnitude(planarVelocity, moveSpeed);

        Vector3 finalVelocity = new Vector3(planarVelocity.x, rb.linearVelocity.y, planarVelocity.z);
        rb.linearVelocity = finalVelocity;

        Vector3 forward = new Vector3(finalVelocity.x, 0f, finalVelocity.z);
        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
    
    private void TryAttackPlayer()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        lastAttackTime = Time.time;
        
        // Try to damage player through health system
        var playerHealth = PlayerStatSystem.Instance;
        if (playerHealth != null)
        {
            int currentHP = playerHealth.CurrentHP.Value;
            int damage = Mathf.FloorToInt(attackDamage);
            int newHP = Mathf.Max(0, currentHP - damage);
            
            playerHealth.CurrentHP.Value = newHP;
            
            if (showDebugInfo)
            {
                Debug.Log($"Mob attacked player for {damage} damage! Player HP: {newHP}/{playerHealth.MaxHP.Value}");
            }
        }
    }
    
    /// <summary>
    /// Finds and damages the nearest light source (explosion with TimedObjectDestroyer)
    /// </summary>
    public void DamageNearestLightSource()
    {
        // Find all Light components in range, then check for TimedObjectDestroyer
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                
        if (allLights.Length == 0)
        {
            return;
        }
        
        // Find the nearest light that has TimedObjectDestroyer (on itself or parent)
        TimedObjectDestroyer nearestLightSource = null;
        float nearestDistance = float.MaxValue;
        Light nearestLight = null;
        
        foreach (Light light in allLights)
        {
            if (!light.enabled) continue;
            
            float distance = Vector3.Distance(transform.position, light.transform.position);
            
            // Check for TimedObjectDestroyer on the same object first, then parents
            TimedObjectDestroyer destroyer = light.GetComponent<TimedObjectDestroyer>();
            if (destroyer == null)
            {
                destroyer = light.GetComponentInParent<TimedObjectDestroyer>();
            }
            
            if (destroyer != null)
            {
                string hierarchyInfo = destroyer.gameObject == light.gameObject ? "same object" : $"parent: '{destroyer.name}'";
                
                if (distance < nearestDistance && distance <= lightDamageRange)
                {
                    nearestDistance = distance;
                    nearestLightSource = destroyer;
                    nearestLight = light;
                }
            }
        }
        
        // Reduce lifetime of the nearest light source
        if (nearestLightSource != null)
        {
            float oldLifetime = nearestLightSource.lifeTime;
            nearestLightSource.lifeTime = Mathf.Max(0.1f, nearestLightSource.lifeTime - lightLifetimeReduction);
            
            
            // If the light's lifetime is very low, give it a slight flicker effect
            if (nearestLightSource.lifeTime < 1f && nearestLight != null)
            {
                StartCoroutine(FlickerLight(nearestLight));
            }
        }
    }
    
    /// <summary>
    /// Helper method to get the full hierarchy path of a transform for debugging
    /// </summary>
    private string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// Creates a flicker effect on a light that's about to expire
    /// </summary>
    private System.Collections.IEnumerator FlickerLight(Light light)
    {
        if (light == null) yield break;
        
        float originalIntensity = light.intensity;
        float flickerDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < flickerDuration)
        {
            if (light == null) yield break;
            
            light.intensity = originalIntensity * Random.Range(0.5f, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (light != null)
        {
            light.intensity = originalIntensity;
        }
    }
    
    /// <summary>
    /// Apply damage to the mob. Called by MobLightDamage or other damage sources.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isAlive) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Trigger health bar flash effect if available
        if (healthBarController != null)
        {
            healthBarController.FlashDamage();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Mob took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;
        
        if (showDebugInfo)
        {
            Debug.Log("Mob died!");
        }
        
        // Spawn death particle effect
        SpawnDeathParticles();
        
        // Stop movement
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        ClearPath();
        
        // Disable components
        enabled = false;
        
        // Optional: Play death animation, spawn loot, etc.
        
        // Destroy after configurable delay
        Destroy(gameObject, despawnDelay);
    }

    /// <summary>
    /// Overrides NavMesh steering parameters when spawned dynamically.
    /// </summary>
    public void ApplyNavMeshOverrides(float sampleRadius, float pathInterval, float cornerThreshold, int areaMask)
    {
        navMeshSampleRadius = Mathf.Max(0.1f, sampleRadius);
        pathRecalculationInterval = Mathf.Max(0.05f, pathInterval);
        cornerArrivalThreshold = Mathf.Max(0.05f, cornerThreshold);
        navMeshAreaMask = areaMask;
        ClearPath();
    }

    private bool EnsureNavMeshPath(Vector3 destination, bool forceRebuild, bool destinationIsPlayer)
    {
        if (navMeshPath == null)
        {
            navMeshPath = new NavMeshPath();
        }

        bool destinationChanged = (desiredNavDestination - destination).sqrMagnitude > 0.01f;
        if (destinationChanged)
        {
            forceRebuild = true;
        }

        desiredNavDestination = destination;

        if (!forceRebuild && hasNavMeshPath && navMeshPath.corners != null && navMeshPath.corners.Length > 0)
        {
            float endDistanceSqr = (navMeshPath.corners[navMeshPath.corners.Length - 1] - destination).sqrMagnitude;
            if (endDistanceSqr <= navMeshSampleRadius * navMeshSampleRadius)
            {
                currentPathCornerIndex = Mathf.Clamp(currentPathCornerIndex, 0, navMeshPath.corners.Length - 1);
                targetPosition = navMeshPath.corners[currentPathCornerIndex];
                return true;
            }

            forceRebuild = true;
        }

        return RecalculateNavMeshPath(destination, destinationIsPlayer);
    }

    private bool RecalculateNavMeshPath(Vector3 destination, bool destinationIsPlayer)
    {
        lastPathUpdateTime = Time.time;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, navMeshSampleRadius, navMeshAreaMask))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"Mob: Unable to sample NavMesh at mob position. Radius: {navMeshSampleRadius}", this);
            }
            hasNavMeshPath = false;
            return false;
        }

        if (!NavMesh.SamplePosition(destination, out NavMeshHit endHit, navMeshSampleRadius, navMeshAreaMask))
        {
            if (showDebugInfo)
            {
                string destinationLabel = destinationIsPlayer ? "player position" : "destination";
                Debug.LogWarning($"Mob: Unable to sample NavMesh near {destinationLabel}. Radius: {navMeshSampleRadius}", this);
            }
            hasNavMeshPath = false;
            return false;
        }

        if (!NavMesh.CalculatePath(startHit.position, endHit.position, navMeshAreaMask, navMeshPath))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Mob: NavMesh.CalculatePath failed.", this);
            }
            hasNavMeshPath = false;
            navMeshPathIsPartial = false;
            return false;
        }

        if (navMeshPath.corners == null || navMeshPath.corners.Length == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Mob: NavMesh path returned with no corners.", this);
            }
            hasNavMeshPath = false;
            navMeshPathIsPartial = false;
            return false;
        }

        navMeshPathIsPartial = navMeshPath.status == NavMeshPathStatus.PathPartial;

        if (navMeshPath.status == NavMeshPathStatus.PathInvalid)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Mob: NavMesh path invalid.", this);
            }
            hasNavMeshPath = false;
            navMeshPathIsPartial = false;
            return false;
        }

        if (navMeshPathIsPartial && showDebugInfo)
        {
            Debug.Log($"Mob: Partial path calculated, using last reachable corner at {navMeshPath.corners[navMeshPath.corners.Length - 1]}", this);
        }

        currentPathCornerIndex = navMeshPath.corners.Length > 1 ? 1 : 0;
        targetPosition = navMeshPath.corners[currentPathCornerIndex];
        hasNavMeshPath = true;
        return true;
    }

    private void AdvancePathCornerIfNeeded()
    {
        if (!hasNavMeshPath || navMeshPath?.corners == null || navMeshPath.corners.Length == 0)
        {
            return;
        }

        Vector3 planarTarget = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        float sqrDistance = (planarTarget - transform.position).sqrMagnitude;
        float threshold = cornerArrivalThreshold * cornerArrivalThreshold;

        if (sqrDistance <= threshold && currentPathCornerIndex < navMeshPath.corners.Length - 1)
        {
            currentPathCornerIndex++;
            targetPosition = navMeshPath.corners[currentPathCornerIndex];
        }
        else if (currentPathCornerIndex >= navMeshPath.corners.Length - 1)
        {
            targetPosition = navMeshPath.corners[navMeshPath.corners.Length - 1];
        }
    }

    private void ClearPath()
    {
        hasNavMeshPath = false;
        currentPathCornerIndex = 0;
        targetPosition = transform.position;
        desiredNavDestination = transform.position;
        navMeshPathIsPartial = false;
        if (navMeshPath != null)
        {
            navMeshPath.ClearCorners();
        }
    }
    
    private void SpawnDeathParticles()
    {
        if (deathParticlePrefab == null) return;
        
        // Calculate spawn position with offset
        Vector3 spawnPosition = transform.position + particleOffset;
        
        // Instantiate the particle effect
        GameObject particleObject = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawned death particles at {spawnPosition}");
        }
        
        // Auto-destroy particles if enabled
        if (autoDestroyParticles)
        {
            // Try to get the ParticleSystem component
            ParticleSystem ps = particleObject.GetComponent<ParticleSystem>();
            
            if (ps != null)
            {
                // Calculate total duration (main duration + start lifetime)
                float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                
                // Destroy after the particle system finishes
                Destroy(particleObject, totalDuration);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Death particles will auto-destroy after {totalDuration:F2} seconds");
                }
            }
            else
            {
                // If no ParticleSystem found, destroy after a default time
                Debug.LogWarning("Death particle prefab has no ParticleSystem component. Using default cleanup time of 5 seconds.");
                Destroy(particleObject, 5f);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        
        // Draw stopping distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw line to player if detected
        if (Application.isPlaying && hasDetectedPlayer && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }
        
        // Draw particle spawn position
        if (deathParticlePrefab != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + particleOffset, 0.3f);
        }
    }

    private void OnEnable()
    {
        if (!ActiveMobs.Contains(this))
        {
            ActiveMobs.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveMobs.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveMobs.Remove(this);
    }
}
