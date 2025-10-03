/// <summary>
/// Projectile.cs
/// Modernized projectile system for Unity 6.2
/// Supports standard projectiles, seeking missiles, and cluster bombs
/// Features explosion spawning, damage application, and target tracking
/// </summary>

using UnityEngine;
using System.Collections.Generic;

public enum ProjectileType
{
    Standard,
    Seeker,
    ClusterBomb
}

public enum DamageType
{
    Direct,
    Explosion
}

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    #region Projectile Type & Behavior
    [Header("Projectile Configuration")]
    [Tooltip("Type of projectile behavior")]
    public ProjectileType projectileType = ProjectileType.Standard;
    
    [Tooltip("How damage is applied - Direct from projectile or from explosion")]
    public DamageType damageType = DamageType.Direct;
    
    [Tooltip("Maximum lifetime in seconds before auto-destruction")]
    [Range(1f, 120f)]
    public float lifetime = 30f;
    #endregion

    #region Movement Settings
    [Header("Movement")]
    [Tooltip("Projectile movement speed (m/s)")]
    [Range(1f, 200f)]
    public float speed = 50f;
    
    [Tooltip("Use initial force impulse instead of constant velocity")]
    public bool useInitialForce = false;
    
    [Tooltip("Initial force magnitude (only if Use Initial Force is enabled)")]
    [Range(100f, 10000f)]
    public float initialForce = 1000f;
    #endregion

    #region Damage Settings
    [Header("Damage (Direct Type Only)")]
    [Tooltip("Damage amount for Direct damage type")]
    [Range(1f, 1000f)]
    public float damage = 100f;
    
    [Tooltip("Support for Bloody Mess system (if installed)")]
    public int weaponType = 0;
    #endregion

    #region Explosion Settings
    [Header("Explosion")]
    [Tooltip("Explosion prefab to spawn on impact/destruction")]
    public GameObject explosionPrefab;
    
    [Tooltip("Scale multiplier for spawned explosion")]
    [Range(0.1f, 10f)]
    public float explosionScale = 1f;
    
    [Tooltip("Align explosion with surface normal on impact")]
    public bool alignToSurface = true;
    #endregion

    #region Seeker Settings
    [Header("Seeker Missile (Seeker Type Only)")]
    [Tooltip("Tag to search for targets")]
    public string seekTag = "Enemy";
    
    [Tooltip("How aggressively the projectile turns toward target")]
    [Range(0.1f, 20f)]
    public float seekRate = 5f;
    
    [Tooltip("How often to update the target list (seconds)")]
    [Range(0.1f, 5f)]
    public float targetUpdateInterval = 0.5f;
    
    [Tooltip("Maximum range to detect targets (0 = unlimited)")]
    [Range(0f, 200f)]
    public float seekRange = 50f;
    #endregion

    #region Cluster Bomb Settings
    [Header("Cluster Bomb (ClusterBomb Type Only)")]
    [Tooltip("Cluster bomb projectile prefab")]
    public GameObject clusterBombPrefab;
    
    [Tooltip("Number of cluster bombs to spawn")]
    [Range(1, 20)]
    public int clusterBombCount = 6;
    
    [Tooltip("Spread radius for cluster bombs")]
    [Range(0.5f, 10f)]
    public float clusterSpreadRadius = 3f;
    #endregion

    #region Private Variables
    private Rigidbody rb;
    private float lifeTimer = 0f;
    private float targetUpdateTimer = 0f;
    private List<GameObject> cachedTargets = new List<GameObject>();
    private bool hasExploded = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Validate settings
        ValidateSettings();
    }

    private void Start()
    {
        // Apply initial force if enabled
        if (useInitialForce)
        {
            rb.AddRelativeForce(Vector3.forward * initialForce, ForceMode.Impulse);
        }
        
        // Initialize target list for seekers
        if (projectileType == ProjectileType.Seeker)
        {
            UpdateTargetList();
        }
    }

    private void Update()
    {
        // Update lifetime
        lifeTimer += Time.deltaTime;
        
        // Auto-destruct after lifetime expires
        if (lifeTimer >= lifetime)
        {
            Explode(transform.position, Vector3.up);
            return;
        }
        
        // Handle constant velocity movement (if not using initial force)
        if (!useInitialForce && rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        
        // Handle seeking behavior
        if (projectileType == ProjectileType.Seeker)
        {
            UpdateSeeking();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        HandleImpact(collision);
    }
    #endregion

    #region Seeking Logic
    private void UpdateSeeking()
    {
        targetUpdateTimer += Time.deltaTime;
        
        // Update target list periodically
        if (targetUpdateTimer >= targetUpdateInterval)
        {
            UpdateTargetList();
            targetUpdateTimer = 0f;
        }
        
        // Find best target and rotate toward it
        GameObject bestTarget = FindBestTarget();
        if (bestTarget != null)
        {
            Vector3 targetDirection = (bestTarget.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, seekRate * Time.deltaTime);
        }
    }

    private void UpdateTargetList()
    {
        cachedTargets.Clear();
        
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(seekTag);
        
        foreach (GameObject target in potentialTargets)
        {
            if (target == null) continue;
            
            // Check range if specified
            if (seekRange > 0f)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > seekRange) continue;
            }
            
            cachedTargets.Add(target);
        }
    }

    private GameObject FindBestTarget()
    {
        if (cachedTargets.Count == 0) return null;
        
        GameObject bestTarget = null;
        float bestDot = -1f;
        
        // Find target most aligned with current direction
        foreach (GameObject target in cachedTargets)
        {
            if (target == null) continue;
            
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(directionToTarget, transform.forward);
            
            if (dot > bestDot)
            {
                bestDot = dot;
                bestTarget = target;
            }
        }
        
        return bestTarget;
    }
    #endregion

    #region Impact & Damage
    private void HandleImpact(Collision collision)
    {
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 surfaceNormal = collision.contacts[0].normal;
        
        // Apply direct damage if configured
        if (damageType == DamageType.Direct)
        {
            ApplyDirectDamage(collision.collider.gameObject, impactPoint);
        }
        
        // Explode at impact point
        Explode(impactPoint, surfaceNormal);
    }

    private void ApplyDirectDamage(GameObject target, Vector3 impactPoint)
    {
        // Try standard health system
        target.SendMessageUpwards("ChangeHealth", -damage, SendMessageOptions.DontRequireReceiver);
        
        // Support for Bloody Mess system (if using limb-based damage)
        if (target.layer == LayerMask.NameToLayer("Limb"))
        {
            Vector3 shotDirection = (target.transform.position - transform.position).normalized;
            
            // Bloody Mess integration would go here
            // Commented out as it requires external package
            /*
            if (target.TryGetComponent<Limb>(out var limb))
            {
                GameObject parent = limb.parent;
                if (parent.TryGetComponent<CharacterSetup>(out var character))
                {
                    character.ApplyDamage(damage, target, weaponType, shotDirection, Camera.main.transform.position);
                }
            }
            */
        }
    }
    #endregion

    #region Explosion
    private void Explode(Vector3 position, Vector3 surfaceNormal)
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            // Calculate rotation to align explosion's UP with surface normal
            Quaternion rotation = Quaternion.identity;
            
            if (alignToSurface && surfaceNormal != Vector3.zero)
            {
                // Rotate the explosion so its local up aligns with the surface normal
                rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            }
            
            GameObject explosion = Instantiate(explosionPrefab, position, rotation);
            
            // Apply scale
            if (explosionScale != 1f)
            {
                explosion.transform.localScale = Vector3.one * explosionScale;
            }
        }
        
        // Handle cluster bombs
        if (projectileType == ProjectileType.ClusterBomb && clusterBombPrefab != null)
        {
            SpawnClusterBombs(position);
        }
        
        // Destroy projectile
        Destroy(gameObject);
    }

    private void SpawnClusterBombs(Vector3 origin)
    {
        for (int i = 0; i < clusterBombCount; i++)
        {
            // Random spread pattern
            Vector3 randomOffset = Random.insideUnitSphere * clusterSpreadRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // Keep above origin
            
            Vector3 spawnPosition = origin + randomOffset;
            Quaternion randomRotation = Random.rotation;
            
            GameObject cluster = Instantiate(clusterBombPrefab, spawnPosition, randomRotation);
            
            // Add random force to cluster bombs
            if (cluster.TryGetComponent<Rigidbody>(out var clusterRb))
            {
                Vector3 randomForce = Random.insideUnitSphere * 300f;
                randomForce.y = Mathf.Abs(randomForce.y) * 0.5f; // Slight upward bias
                clusterRb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }
    #endregion

    #region Validation & Utilities
    private void ValidateSettings()
    {
        // Ensure rigidbody is configured properly
        if (rb != null)
        {
            rb.useGravity = useInitialForce; // Use gravity only with initial force
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Validate seeker settings
        if (projectileType == ProjectileType.Seeker && string.IsNullOrEmpty(seekTag))
        {
            Debug.LogWarning($"[Projectile] Seeker type requires a seek tag! GameObject: {gameObject.name}", this);
        }
        
        // Validate cluster bomb settings
        if (projectileType == ProjectileType.ClusterBomb && clusterBombPrefab == null)
        {
            Debug.LogWarning($"[Projectile] ClusterBomb type requires a cluster bomb prefab! GameObject: {gameObject.name}", this);
        }
    }

    /// <summary>
    /// Multiply the damage amount (useful for power-ups)
    /// </summary>
    public void MultiplyDamage(float multiplier)
    {
        damage *= multiplier;
    }

    /// <summary>
    /// Multiply the initial force (useful for power-ups)
    /// </summary>
    public void MultiplyInitialForce(float multiplier)
    {
        initialForce *= multiplier;
    }

    /// <summary>
    /// Multiply the speed (useful for power-ups)
    /// </summary>
    public void MultiplySpeed(float multiplier)
    {
        speed *= multiplier;
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw seek range for seeker missiles
        if (projectileType == ProjectileType.Seeker && seekRange > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, seekRange);
        }
        
        // Draw lines to cached targets
        if (projectileType == ProjectileType.Seeker && cachedTargets != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject target in cachedTargets)
            {
                if (target != null)
                {
                    Gizmos.DrawLine(transform.position, target.transform.position);
                }
            }
        }
        
        // Draw cluster bomb spawn radius
        if (projectileType == ProjectileType.ClusterBomb)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, clusterSpreadRadius);
        }
    }
    #endregion
}
