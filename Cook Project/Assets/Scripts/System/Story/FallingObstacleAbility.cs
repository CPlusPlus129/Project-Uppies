using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the "Falling Obstacle" ability for the Boss Chase sequence.
/// Periodically drops objects from the sky near the player's predicted position.
/// </summary>
public class FallingObstacleAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("The mob that owns this ability. Used to find the player target.")]
    private Mob ownerMob;

    [SerializeField]
    [Tooltip("If Mob is not used, manually assign player here.")]
    private Transform explicitPlayerTarget;

    [Header("Simulation Settings")]
    [SerializeField]
    [Tooltip("Time in seconds between ability triggers.")]
    private float spawnInterval = 3.0f;

    [SerializeField]
    [Tooltip("How far ahead (in seconds) to predict the player's movement.")]
    private float predictionLookaheadTime = 1.5f;

    [SerializeField]
    [Tooltip("Maximum distance from the player to try and validate a spawn point.")]
    private float maxSpawnRadius = 10f;

    [Header("Obstacle Settings")]
    [SerializeField]
    [Tooltip("List of prefabs to drop randomly.")]
    private List<GameObject> obstaclesToDrop;

    [SerializeField]
    [Tooltip("How high above the ground the object spawns.")]
    private float dropHeight = 15f;

    [SerializeField]
    [Tooltip("Damage dealt to player on impact.")]
    private int damageOnHit = 15;

    [Header("Warning Indicator")]
    [SerializeField]
    [Tooltip("Time in seconds between the warning appearing and the object spawning.")]
    private float warningDuration = 1.5f;

    [SerializeField]
    [Tooltip("Radius of the warning circle.")]
    private float indicatorRadius = 1.5f;
    
    [SerializeField]
    [Tooltip("Width of the warning circle line.")]
    private float indicatorWidth = 0.2f;
    
    [SerializeField]
    [Tooltip("Color of the warning circle.")]
    private Color indicatorColor = Color.red;

    [Header("Visual Effects")]
    [SerializeField]
    [Tooltip("If true, falling objects will glow as they fall.")]
    private bool useGlowEffect = true;

    [SerializeField]
    [Tooltip("Color of the glow emission.")]
    private Color glowColor = new Color(1f, 0.2f, 0f); // Red-Orange

    [SerializeField]
    [Tooltip("Peak brightness of the glow just before impact.")]
    private float maxGlowIntensity = 4f;
    
    [SerializeField]
    [Tooltip("Scale of the glow effect mesh.")]
    private float glowScale = 1.1f;

    [SerializeField]
    [Tooltip("Fresnel power for the see-through effect.")]
    private float fresnelPower = 3f;

    [SerializeField]
    [Tooltip("How long the glow takes to fade out after impact.")]
    private float glowDissipateTime = 2.0f;

    [Header("Lifecycle")]
    [SerializeField]
    [Tooltip("How long after spawning should the object be destroyed?")]
    private float despawnTime = 10f;

    private bool isAbilityActive;
    private float nextSpawnTime;
    private Vector3 lastPlayerPosition;
    private Vector3 currentPlayerVelocity;
    
    // Shuffle bag for cycling through objects
    private List<GameObject> currentSpawnQueue = new List<GameObject>();

    // Cache for safety
    private Transform TargetPlayer
    {
        get
        {
            if (explicitPlayerTarget != null) return explicitPlayerTarget;
            if (ownerMob != null && ownerMob.PlayerTransform != null) return ownerMob.PlayerTransform;
            
            // Fallback: try to find player by tag if we haven't yet
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) return p.transform;
            
            return null;
        }
    }

    private void Awake()
    {
        if (ownerMob == null)
        {
            ownerMob = GetComponentInParent<Mob>();
        }
    }

    private void Update()
    {
        // Always track player velocity if possible, so we have valid data when the ability starts
        TrackPlayerVelocity();

        if (!isAbilityActive) return;

        if (Time.time >= nextSpawnTime)
        {
            AttemptSpawnSequence();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    /// <summary>
    /// activates the ability loop.
    /// </summary>
    public void Play()
    {
        if (obstaclesToDrop == null || obstaclesToDrop.Count == 0)
        {
            Debug.LogWarning($"[FallingObstacleAbility] No obstacles assigned in 'Obstacles To Drop'. Ability will not spawn anything.", this);
        }
        
        Debug.Log($"[FallingObstacleAbility] Play called. Spawning will start in {spawnInterval * 0.5f} seconds.", this);
        isAbilityActive = true;
        nextSpawnTime = Time.time + (spawnInterval * 0.5f); // mild delay on start
        
        // Reset queue on start
        currentSpawnQueue.Clear();
    }

    /// <summary>
    /// Deactivates the ability loop.
    /// </summary>
    public void Stop()
    {
        Debug.Log($"[FallingObstacleAbility] Stop called.", this);
        isAbilityActive = false;
    }

    private void TrackPlayerVelocity()
    {
        Transform player = TargetPlayer;
        if (player == null) return;

        // Simple finite difference for velocity
        // In a real production environment, we might want to smooth this value
        Vector3 displacement = player.position - lastPlayerPosition;
        if (Time.deltaTime > 0)
        {
            currentPlayerVelocity = Vector3.Lerp(currentPlayerVelocity, displacement / Time.deltaTime, Time.deltaTime * 5f);
        }
        lastPlayerPosition = player.position;
    }

    private GameObject GetNextObstacle()
    {
        if (obstaclesToDrop == null || obstaclesToDrop.Count == 0) return null;

        if (currentSpawnQueue.Count == 0)
        {
            // Refill
            currentSpawnQueue.AddRange(obstaclesToDrop);
            
            // Shuffle (Fisher-Yates)
            for (int i = 0; i < currentSpawnQueue.Count; i++)
            {
                GameObject temp = currentSpawnQueue[i];
                int randomIndex = Random.Range(i, currentSpawnQueue.Count);
                currentSpawnQueue[i] = currentSpawnQueue[randomIndex];
                currentSpawnQueue[randomIndex] = temp;
            }
        }

        if (currentSpawnQueue.Count == 0) return null; // Should not happen unless list is empty

        GameObject next = currentSpawnQueue[0];
        currentSpawnQueue.RemoveAt(0);
        return next;
    }

    private void AttemptSpawnSequence()
    {
        if (obstaclesToDrop == null || obstaclesToDrop.Count == 0) return;
        
        Transform player = TargetPlayer;
        if (player == null)
        {
            Debug.LogWarning($"[FallingObstacleAbility] Cannot spawn: Player target not found.", this);
            return;
        }

        // 1. Calculate Predicted Position
        Vector3 predictedPos = player.position + (currentPlayerVelocity * predictionLookaheadTime);

        // 2. Validate with NavMesh
        // We sample nearest position on NavMesh to ensure we don't drop things into the void or inside walls (assuming NavMesh is built correctly)
        NavMeshHit hit;
        Vector3 spawnTargetPos = predictedPos;
        
        if (NavMesh.SamplePosition(predictedPos, out hit, maxSpawnRadius, NavMesh.AllAreas))
        {
            spawnTargetPos = hit.position;
        }
        else
        {
            // Fallback to player position if prediction is off-mesh
            if (NavMesh.SamplePosition(player.position, out hit, maxSpawnRadius, NavMesh.AllAreas))
            {
                spawnTargetPos = hit.position;
            }
            else
            {
                // Failed to find valid ground
                Debug.LogWarning($"[FallingObstacleAbility] Could not find valid NavMesh position near player to drop obstacle.", this);
                return;
            }
        }

        // Prepare the object to drop
        GameObject prefabToSpawn = GetNextObstacle();
        if (prefabToSpawn != null)
        {
            StartCoroutine(SpawnRoutine(spawnTargetPos, prefabToSpawn));
        }
    }

    private IEnumerator SpawnRoutine(Vector3 groundPosition, GameObject prefabToSpawn)
    {
        // 1. Create Procedural Warning Circle
        GameObject indicatorObj = new GameObject("FallingObstacleIndicator");
        indicatorObj.transform.position = groundPosition + Vector3.up * 0.05f; // Slight offset to avoid z-fighting
        
        LineRenderer lr = indicatorObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = indicatorWidth;
        lr.endWidth = indicatorWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = indicatorColor;
        lr.endColor = indicatorColor;
        
        int segments = 40;
        lr.positionCount = segments;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * indicatorRadius;
            float z = Mathf.Sin(angle) * indicatorRadius;
            lr.SetPosition(i, new Vector3(x, 0f, z)); // Local space, flat on ground
        }

        yield return new WaitForSeconds(warningDuration);

        if (indicatorObj != null)
        {
            Destroy(indicatorObj);
        }

        // 2. Spawn Falling Object
        if (prefabToSpawn != null)
        {
            Vector3 skyPos = groundPosition + Vector3.up * dropHeight;
            Debug.Log($"[FallingObstacleAbility] Spawning '{prefabToSpawn.name}' at {skyPos}.");
            
            GameObject spawned = Instantiate(prefabToSpawn, skyPos, Random.rotation);
            
            if (spawned != null)
            {
                // Ensure physics are set up so the object actually falls
                Rigidbody rb = spawned.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogWarning($"[FallingObstacleAbility] Object '{spawned.name}' had no Rigidbody. Adding one automatically so it can fall.", spawned);
                    rb = spawned.AddComponent<Rigidbody>();
                    rb.mass = 50f; // Give it some heft by default
                }

                // Force physics on
                rb.useGravity = true;
                rb.isKinematic = false;

                // Ensure there's a collider so it can actually hit things
                // (Compound colliders on children are fine, but if we have NOTHING, we need a fallback)
                var colliders = spawned.GetComponentsInChildren<Collider>();
                if (colliders == null || colliders.Length == 0)
                {
                    Debug.LogWarning($"[FallingObstacleAbility] Object '{spawned.name}' had no Colliders. Adding a SphereCollider automatically.", spawned);
                    var col = spawned.AddComponent<SphereCollider>();
                    col.radius = 0.5f; // Reasonable default for a falling prop
                }

                // Add damage component
                var damageComp = spawned.AddComponent<FallingObstacleDamage>();
                if (damageComp != null)
                {
                    damageComp.damageAmount = damageOnHit;
                    damageComp.destroyOnHit = false; // Keep the obstacle around as terrain
                }
                else
                {
                    Debug.LogError($"[FallingObstacleAbility] Failed to add FallingObstacleDamage to {spawned.name}");
                }

                // Add Glow Effect
                if (useGlowEffect)
                {
                    var glow = spawned.AddComponent<FallingObjectGlow>();
                    glow.glowColor = glowColor;
                    glow.maxIntensity = maxGlowIntensity;
                    glow.dissipateDuration = glowDissipateTime;
                    glow.glowScale = glowScale;
                    glow.fresnelPower = fresnelPower;
                    
                    // Approximate fall time: t = sqrt(2h/g)
                    float gravity = Physics.gravity.magnitude > 0 ? Physics.gravity.magnitude : 9.81f;
                    glow.fallDuration = Mathf.Sqrt((2 * dropHeight) / gravity);
                }

                if (despawnTime > 0)
                {
                    Destroy(spawned, despawnTime);
                }

                Debug.Log($"[FallingObstacleAbility] Successfully spawned '{spawned.name}' (InstanceID: {spawned.GetInstanceID()}). Active: {spawned.activeInHierarchy}");
            }
            else
            {
                Debug.LogError($"[FallingObstacleAbility] Failed to instantiate object!");
            }
        }
        else
        {
            Debug.LogError($"[FallingObstacleAbility] Prefab reference was lost before spawn!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (TargetPlayer != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 predicted = TargetPlayer.position + (currentPlayerVelocity * predictionLookaheadTime);
            Gizmos.DrawLine(TargetPlayer.position, predicted);
            Gizmos.DrawWireSphere(predicted, 0.5f);
        }
    }
}
