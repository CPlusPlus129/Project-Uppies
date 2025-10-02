using UnityEngine;

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
    
    [Header("Detection")]
    [SerializeField] private float playerDetectionRange = 30f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // State
    private bool isAlive = true;
    private bool hasDetectedPlayer = false;
    private Vector3 targetPosition;
    
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
        
        // Try to find health bar controller (optional)
        healthBarController = GetComponent<MobHealthBarController>();
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
        if (!isAlive || player == null) return;
        
        UpdateAI();
    }
    
    private void FixedUpdate()
    {
        if (!isAlive || player == null) return;
        
        if (hasDetectedPlayer)
        {
            MoveTowardsTarget();
        }
    }
    
    private void UpdateAI()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= playerDetectionRange)
        {
            // Optional: Check line of sight
            if (HasLineOfSight(player.position))
            {
                hasDetectedPlayer = true;
                targetPosition = player.position;
                
                // Check if in attack range
                if (distanceToPlayer <= attackRange)
                {
                    TryAttackPlayer();
                }
            }
        }
        else
        {
            hasDetectedPlayer = false;
        }
        
        if (showDebugInfo && hasDetectedPlayer)
        {
            Debug.Log($"Mob tracking player. Distance: {distanceToPlayer:F2}m");
        }
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
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep movement on horizontal plane
        
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Stop if too close
        if (distanceToTarget <= stoppingDistance)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
        
        // Move towards target
        Vector3 moveVelocity = direction * moveSpeed;
        moveVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity for gravity
        rb.linearVelocity = moveVelocity;
        
        // Rotate to face target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
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
        
        // Disable components
        enabled = false;
        
        // Optional: Play death animation, spawn loot, etc.
        
        // Destroy after configurable delay
        Destroy(gameObject, despawnDelay);
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
}
