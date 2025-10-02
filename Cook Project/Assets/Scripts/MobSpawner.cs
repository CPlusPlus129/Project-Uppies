using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns mobs at regular intervals or on demand.
/// Can spawn at specific points or around the spawner position.
/// </summary>
public class MobSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject mobPrefab;
    [Tooltip("Maximum number of mobs this spawner can have alive at once")]
    [SerializeField] private int maxMobs = 5;
    [Tooltip("Time in seconds between spawn attempts")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool autoSpawn = true;
    
    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 5f;
    [Tooltip("If true, spawns at random positions within radius. If false, spawns at exact position.")]
    [SerializeField] private bool randomizePosition = true;
    [Tooltip("Height offset from spawner position")]
    [SerializeField] private float spawnHeight = 0.5f;
    
    [Header("Spawn Points (Optional)")]
    [Tooltip("If assigned, will spawn at these points instead of using spawn radius")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool cycleSpawnPoints = false;
    private int currentSpawnPointIndex = 0;
    
    [Header("Mob Setup")]
    [Tooltip("Automatically assign player reference to spawned mobs")]
    [SerializeField] private bool autoAssignPlayer = true;
    [SerializeField] private Transform playerTransform;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color spawnAreaColor = Color.red;
    
    // Internal state
    private List<GameObject> spawnedMobs = new List<GameObject>();
    private float nextSpawnTime = 0f;
    
    // Public properties
    public int CurrentMobCount => spawnedMobs.Count;
    public int MaxMobs => maxMobs;
    public bool CanSpawn => spawnedMobs.Count < maxMobs;
    
    private void Start()
    {
        // Validate mobPrefab
        if (mobPrefab == null)
        {
            Debug.LogError("MobSpawner: No mob prefab assigned!", this);
            enabled = false;
            return;
        }
        
        // Try to find player if needed
        if (autoAssignPlayer && playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
        
        // Spawn initial mobs
        if (spawnOnStart)
        {
            SpawnInitialMobs();
        }
        
        // Set next spawn time
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    private void Update()
    {
        if (!autoSpawn) return;
        
        // Clean up destroyed mobs from list
        CleanupDestroyedMobs();
        
        // Check if it's time to spawn
        if (Time.time >= nextSpawnTime && CanSpawn)
        {
            SpawnMob();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void SpawnInitialMobs()
    {
        int mobsToSpawn = Mathf.Min(maxMobs, spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints.Length : 1);
        
        for (int i = 0; i < mobsToSpawn; i++)
        {
            SpawnMob();
        }
    }
    
    /// <summary>
    /// Spawns a single mob at the next available position.
    /// </summary>
    public void SpawnMob()
    {
        if (!CanSpawn)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"Cannot spawn mob - at max capacity ({maxMobs})");
            }
            return;
        }
        
        Vector3 spawnPosition = GetSpawnPosition();
        GameObject mob = Instantiate(mobPrefab, spawnPosition, Quaternion.identity);
        
        // Set up the mob
        SetupMob(mob);
        
        // Add to tracked list
        spawnedMobs.Add(mob);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawned mob at {spawnPosition}. Total mobs: {spawnedMobs.Count}/{maxMobs}");
        }
    }
    
    private Vector3 GetSpawnPosition()
    {
        // Use spawn points if available
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint;
            
            if (cycleSpawnPoints)
            {
                // Cycle through spawn points
                spawnPoint = spawnPoints[currentSpawnPointIndex];
                currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Length;
            }
            else
            {
                // Pick random spawn point
                spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            }
            
            return spawnPoint.position;
        }
        
        // Otherwise use spawner position with optional randomization
        Vector3 position = transform.position;
        
        if (randomizePosition)
        {
            // Random position within radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            position += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        position.y += spawnHeight;
        return position;
    }
    
    private void SetupMob(GameObject mob)
    {
        // Auto-assign player reference if enabled
        if (autoAssignPlayer && playerTransform != null)
        {
            Mob mobComponent = mob.GetComponent<Mob>();
            if (mobComponent != null)
            {
                // Use reflection to set the private player field
                var playerField = typeof(Mob).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playerField != null)
                {
                    playerField.SetValue(mobComponent, playerTransform);
                }
            }
        }
    }
    
    private void CleanupDestroyedMobs()
    {
        // Remove null entries (destroyed mobs)
        spawnedMobs.RemoveAll(mob => mob == null);
    }
    
    /// <summary>
    /// Force spawn a mob regardless of spawn limit.
    /// </summary>
    public void ForceSpawnMob()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        GameObject mob = Instantiate(mobPrefab, spawnPosition, Quaternion.identity);
        SetupMob(mob);
        spawnedMobs.Add(mob);
        
        if (showDebugInfo)
        {
            Debug.Log($"Force spawned mob at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// Destroy all spawned mobs.
    /// </summary>
    public void DestroyAllMobs()
    {
        foreach (GameObject mob in spawnedMobs)
        {
            if (mob != null)
            {
                Destroy(mob);
            }
        }
        spawnedMobs.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("Destroyed all spawned mobs");
        }
    }
    
    /// <summary>
    /// Reset spawner and spawn initial mobs.
    /// </summary>
    public void ResetSpawner()
    {
        DestroyAllMobs();
        SpawnInitialMobs();
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw spawn radius
        Gizmos.color = spawnAreaColor;
        
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Draw spawn points
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }
        else
        {
            // Draw spawn radius
            Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, spawnRadius);
            
            // Draw spawn center
            Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, Vector3.one * 0.5f);
        }
        
        // Draw spawner icon
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Show more detailed info when selected
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            foreach (GameObject mob in spawnedMobs)
            {
                if (mob != null)
                {
                    Gizmos.DrawLine(transform.position, mob.transform.position);
                }
            }
        }
    }
}
