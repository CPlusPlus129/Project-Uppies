using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using R3;

/// <summary>
/// Spawns mobs at regular intervals or on demand.
/// Can spawn at specific points or around the spawner position.
/// </summary>
public class MobSpawner : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Spawn Settings")]
    [SerializeField] private GameObject mobPrefab;
    [Tooltip("Maximum number of mobs this spawner can have alive at once")]
    [SerializeField] private int maxMobs = 5;
    [Tooltip("Time in seconds between spawn attempts")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool autoSpawn = true;
    [Tooltip("Automatically pause spawning while the game is in After Shift state.")]
    [SerializeField] private bool suspendDuringAfterShift = true;
    
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
    [Tooltip("Apply NavMesh overrides to spawned mobs so they use a consistent surface configuration")]
    [SerializeField] private bool applyNavMeshOverrides = false;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1.5f;
    [SerializeField, Min(0.05f)] private float navMeshPathInterval = 0.4f;
    [SerializeField, Min(0.05f)] private float navMeshCornerThreshold = 0.35f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
    
    [Header("Visual Customization")]
    [Tooltip("Randomize mob color on spawn")]
    [SerializeField] private bool randomizeColor = true;
    [Tooltip("Use a predefined color palette instead of fully random colors")]
    [SerializeField] private bool useColorPalette = false;
    [Tooltip("Color palette to choose from (only used if useColorPalette is true)")]
    [SerializeField] private Color[] colorPalette = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),    // Red
        new Color(0.3f, 0.3f, 1f),    // Blue
        new Color(0.3f, 1f, 0.3f),    // Green
        new Color(1f, 1f, 0.3f),      // Yellow
        new Color(1f, 0.3f, 1f),      // Magenta
        new Color(0.3f, 1f, 1f),      // Cyan
        new Color(1f, 0.6f, 0.2f),    // Orange
        new Color(0.6f, 0.3f, 1f)     // Purple
    };
    [Tooltip("Brightness range for random colors (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float minBrightness = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float maxBrightness = 1f;
    [Tooltip("Saturation range for random colors (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float minSaturation = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float maxSaturation = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color spawnAreaColor = Color.red;

    // Internal state
    private List<GameObject> spawnedMobs = new List<GameObject>();
    private float nextSpawnTime = 0f;
    private MaterialPropertyBlock colorPropertyBlock;
    private IShiftSystem shiftSystem;
    private System.IDisposable shiftStateSubscription;
    private bool spawnSuspended;
    private bool requestedShiftAwareness;
    private bool afterShiftActive;

    // Public properties
    public int CurrentMobCount => spawnedMobs.Count;
    public int MaxMobs => maxMobs;
    public bool CanSpawn => !spawnSuspended && spawnedMobs.Count < maxMobs;
    
    private void Awake()
    {
        colorPropertyBlock = new MaterialPropertyBlock();
    }

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

        if (suspendDuringAfterShift)
        {
            InitializeShiftAwarenessAsync().Forget();
        }
    }

    private void OnEnable()
    {
        Mob.MobDied += OnMobDied;
    }

    private void OnDisable()
    {
        Mob.MobDied -= OnMobDied;
    }

    private void OnDestroy()
    {
        shiftStateSubscription?.Dispose();
        shiftStateSubscription = null;
    }

    private void OnMobDied(Mob mob)
    {
        if (mob != null && mob.gameObject != null)
        {
            spawnedMobs.Remove(mob.gameObject);
        }
    }

    private void Update()
    {
        if (!autoSpawn || spawnSuspended) return;

        // Check if it's time to spawn
        if (Time.time >= nextSpawnTime && CanSpawn)
        {
            SpawnMob();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnInitialMobs()
    {
        int mobsToSpawn = maxMobs;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            mobsToSpawn = Mathf.Min(maxMobs, spawnPoints.Length);
        }

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
                var reason = spawnSuspended ? "spawning temporarily disabled" : $"at max capacity ({maxMobs})";
                Debug.LogWarning($"Cannot spawn mob - {reason}");
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

    private async UniTaskVoid InitializeShiftAwarenessAsync()
    {
        if (requestedShiftAwareness)
            return;

        requestedShiftAwareness = true;

        await UniTask.WaitUntil(() => GameFlow.Instance != null && GameFlow.Instance.IsInitialized);

        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        if (shiftSystem == null)
            return;

        shiftStateSubscription?.Dispose();
        shiftStateSubscription = shiftSystem.currentState.Subscribe(OnShiftStateChanged);
        OnShiftStateChanged(shiftSystem.currentState.Value);
    }

    private void OnShiftStateChanged(ShiftSystem.ShiftState state)
    {
        if (!suspendDuringAfterShift)
            return;

        bool shouldSuspend = state == ShiftSystem.ShiftState.AfterShift;

        if (shouldSuspend && !afterShiftActive)
        {
            DestroyAllMobs();
        }

        afterShiftActive = shouldSuspend;
        spawnSuspended = shouldSuspend;
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
                spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            }
            
            return spawnPoint.position;
        }
        
        // Otherwise use spawner position with optional randomization
        Vector3 position = transform.position;
        
        if (randomizePosition)
        {
            // Random position within radius
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            position += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        position.y += spawnHeight;
        return position;
    }
    
    private void SetupMob(GameObject mob)
    {
        Mob mobComponent = mob.GetComponent<Mob>();

        // Auto-assign player reference if enabled
        if (autoAssignPlayer && playerTransform != null && mobComponent != null)
        {
            mobComponent.AssignPlayer(playerTransform);
        }

        if (applyNavMeshOverrides && mobComponent != null)
        {
            mobComponent.ApplyNavMeshOverrides(navMeshSampleRadius, navMeshPathInterval, navMeshCornerThreshold, navMeshAreaMask);
        }
        
        // Randomize color if enabled
        if (randomizeColor)
        {
            RandomizeMobColor(mob);
        }
    }
    
    /// <summary>
    /// Applies a random color to the mob's renderer(s).
    /// </summary>
    private void RandomizeMobColor(GameObject mob)
    {
        Color randomColor;
        
        if (useColorPalette && colorPalette != null && colorPalette.Length > 0)
        {
            // Pick a random color from the palette
            randomColor = colorPalette[UnityEngine.Random.Range(0, colorPalette.Length)];
        }
        else
        {
            // Generate a fully random color with brightness and saturation constraints
            randomColor = GenerateRandomColor();
        }
        
        // Apply the color to all renderers on the mob
        Renderer[] renderers = mob.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"MobSpawner: No renderers found on mob {mob.name}. Cannot apply color.", this);
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            int materialCount = sharedMaterials != null ? sharedMaterials.Length : 0;
            materialCount = Mathf.Max(1, materialCount);

            for (int subMesh = 0; subMesh < materialCount; subMesh++)
            {
                Material mat = sharedMaterials != null && subMesh < sharedMaterials.Length ? sharedMaterials[subMesh] : null;
                colorPropertyBlock.Clear();
                renderer.GetPropertyBlock(colorPropertyBlock, subMesh);
                colorPropertyBlock.SetColor(ColorId, randomColor);
                colorPropertyBlock.SetColor(BaseColorId, randomColor);

                if (mat != null && mat.HasProperty(EmissionColorId))
                {
                    colorPropertyBlock.SetColor(EmissionColorId, randomColor * 0.2f);
                }

                renderer.SetPropertyBlock(colorPropertyBlock, subMesh);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"MobSpawner: Applied color {randomColor} to mob {mob.name}");
        }
    }
    
    /// <summary>
    /// Generates a random color with controlled brightness and saturation.
    /// </summary>
    private Color GenerateRandomColor()
    {
        // Generate random hue (0-1 for full color wheel)
        float hue = UnityEngine.Random.value;
        
        // Use the brightness and saturation ranges
        float saturation = UnityEngine.Random.Range(minSaturation, maxSaturation);
        float brightness = UnityEngine.Random.Range(minBrightness, maxBrightness);
        
        // Convert HSV to RGB
        Color color = Color.HSVToRGB(hue, saturation, brightness);
        
        return color;
    }
    

    
    /// <summary>
    /// Force spawn a mob regardless of spawn limit.
    /// </summary>
    public void ForceSpawnMob()
    {
        if (spawnSuspended)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Force spawn blocked while After Shift is active.");
            }
            return;
        }

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
