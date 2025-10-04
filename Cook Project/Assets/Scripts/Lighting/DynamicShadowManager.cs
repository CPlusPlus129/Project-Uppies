using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// OPTIMIZED VERSION - Manages which dynamic lights cast shadows based on distance to player.
/// Performance improvements: No LINQ, sqrMagnitude, spatial culling, pre-allocated buffers.
/// </summary>
public class DynamicShadowManager : MonoBehaviour
{
    public static DynamicShadowManager Instance { get; private set; }

    [Header("Shadow Budget")]
    [Tooltip("Maximum number of dynamic lights that can cast shadows simultaneously")]
    [Range(1, 8)]
    public int maxShadowCastingLights = 3;

    [Header("Light Limit")]
    [Tooltip("Enable limiting the total number of active dynamic lights in the scene")]
    public bool enableLightLimit = true;

    [Tooltip("Maximum number of dynamic lights allowed at once. Oldest lights are removed when limit is reached.")]
    [Range(1, 100)]
    public int maxTotalLights = 20;

    [Tooltip("What to do with excess lights: Disable (keeps GameObject) or Destroy (removes from scene)")]
    public LightLimitMode lightLimitMode = LightLimitMode.Destroy;

    [Header("Player Reference")]
    [Tooltip("The player transform - shadows prioritize lights closest to this")]
    public Transform playerTransform;

    [Header("Performance")]
    [Tooltip("Update shadow priorities every N frames (higher = better performance, lower = more responsive)")]
    [Range(1, 10)]
    public int updateInterval = 2;

    [Header("Shadow Settings")]
    [Tooltip("Use Hard Shadows (faster) or Soft Shadows (better quality)")]
    public bool useHardShadows = true;

    [Header("Culling")]
    [Tooltip("Maximum distance for shadow casting. Lights beyond this distance won't cast shadows.")]
    public float maxShadowDistance = 30f;

    [Tooltip("Enable spatial culling (recommended for performance)")]
    public bool useSpatialCulling = true;

    // Use HashSet for O(1) lookups
    private HashSet<DynamicShadowLight> registeredLights = new HashSet<DynamicShadowLight>();
    
    // Queue to track light registration order (for FIFO removal)
    private Queue<DynamicShadowLight> lightQueue = new Queue<DynamicShadowLight>();
    
    // Pre-allocated buffer to avoid GC
    private List<LightDistancePair> sortBuffer = new List<LightDistancePair>();
    
    private int frameCounter = 0;

    // Enum for light limit behavior
    public enum LightLimitMode
    {
        Disable,  // Just disable the light component
        Destroy   // Destroy the entire GameObject
    }

    // Struct to avoid boxing when sorting
    private struct LightDistancePair
    {
        public DynamicShadowLight light;
        public float sqrDistance;

        public LightDistancePair(DynamicShadowLight light, float sqrDistance)
        {
            this.light = light;
            this.sqrDistance = sqrDistance;
        }
    }

    // Custom comparer to avoid lambda allocations
    private class DistanceComparer : IComparer<LightDistancePair>
    {
        public int Compare(LightDistancePair a, LightDistancePair b)
        {
            return a.sqrDistance.CompareTo(b.sqrDistance);
        }
    }
    private static readonly DistanceComparer distanceComparer = new DistanceComparer();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find player if not set
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("DynamicShadowManager: No player transform assigned and no GameObject with 'Player' tag found!");
            }
        }
    }

    private void LateUpdate()
    {
        frameCounter++;
        
        // Only update priorities every N frames for performance
        if (frameCounter >= updateInterval)
        {
            frameCounter = 0;
            UpdateShadowPriorities();
        }
    }

    /// <summary>
    /// Register a light to be managed by the shadow system
    /// </summary>
    public void RegisterLight(DynamicShadowLight light)
    {
        // Check if we're at the light limit
        if (enableLightLimit && lightQueue.Count >= maxTotalLights)
        {
            // Remove the oldest light (FIFO)
            // Keep dequeuing until we find a valid light or queue is empty
            DynamicShadowLight oldestLight = null;
            while (lightQueue.Count > 0 && oldestLight == null)
            {
                oldestLight = lightQueue.Dequeue();
            }
            
            if (oldestLight != null)
            {
                registeredLights.Remove(oldestLight);
                
                if (lightLimitMode == LightLimitMode.Destroy)
                {
                    // Destroy the entire explosion GameObject (traverse up to root if needed)
                    Transform rootTransform = oldestLight.transform.root;
                    Destroy(rootTransform.gameObject);
                }
                else
                {
                    // Just disable the light component
                    if (oldestLight.lightComponent != null)
                    {
                        oldestLight.lightComponent.enabled = false;
                    }
                }
            }
        }
        
        // HashSet.Add is O(1) instead of List.Contains O(n)
        if (registeredLights.Add(light))
        {
            // Only add to queue if it's a new light (Add returns true)
            lightQueue.Enqueue(light);
        }
    }

    /// <summary>
    /// Unregister a light when it's destroyed
    /// </summary>
    public void UnregisterLight(DynamicShadowLight light)
    {
        registeredLights.Remove(light);
        // Note: We don't remove from queue as it's FIFO and will be cleaned up naturally
    }

    /// <summary>
    /// Updates which lights should cast shadows based on distance to player (OPTIMIZED with partial sort)
    /// </summary>
    private void UpdateShadowPriorities()
    {
        if (playerTransform == null || registeredLights.Count == 0)
        {
            return;
        }

        // Clear buffer (reuse allocated memory)
        sortBuffer.Clear();

        Vector3 playerPos = playerTransform.position;
        float maxSqrDistance = maxShadowDistance * maxShadowDistance;

        // Manual iteration - no LINQ, no allocations
        foreach (var light in registeredLights)
        {
            // Skip null or inactive lights
            if (light == null || !light.isActiveAndEnabled || light.lightComponent == null)
            {
                continue;
            }

            // Calculate squared distance (no sqrt = much faster)
            float sqrDist = (light.transform.position - playerPos).sqrMagnitude;

            // Spatial culling: Skip lights beyond max distance
            if (useSpatialCulling && sqrDist > maxSqrDistance)
            {
                // Ensure distant lights have shadows disabled
                light.SetShadowsEnabled(false, useHardShadows);
                continue;
            }

            sortBuffer.Add(new LightDistancePair(light, sqrDist));
        }

        // Early exit if no active lights
        if (sortBuffer.Count == 0)
        {
            return;
        }

        // OPTIMIZATION: Use partial sort instead of full sort
        // We only need the N closest lights, so we use selection sort for just those
        int lightsToSort = Mathf.Min(maxShadowCastingLights, sortBuffer.Count);
        
        if (lightsToSort < sortBuffer.Count)
        {
            // Partial selection sort - only sort the first N elements
            for (int i = 0; i < lightsToSort; i++)
            {
                int minIndex = i;
                for (int j = i + 1; j < sortBuffer.Count; j++)
                {
                    if (sortBuffer[j].sqrDistance < sortBuffer[minIndex].sqrDistance)
                    {
                        minIndex = j;
                    }
                }
                
                if (minIndex != i)
                {
                    // Swap
                    var temp = sortBuffer[i];
                    sortBuffer[i] = sortBuffer[minIndex];
                    sortBuffer[minIndex] = temp;
                }
            }
        }
        else
        {
            // If we need to shadow all lights, just do a full sort (it's small anyway)
            sortBuffer.Sort(distanceComparer);
        }

        // Enable shadows on the closest N lights
        for (int i = 0; i < sortBuffer.Count; i++)
        {
            bool shouldCastShadows = i < maxShadowCastingLights;
            sortBuffer[i].light.SetShadowsEnabled(shouldCastShadows, useHardShadows);
        }
    }

    /// <summary>
    /// Clean up null references (manual cleanup, no lambda)
    /// </summary>
    public void CleanupNullReferences()
    {
        registeredLights.RemoveWhere(light => light == null);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw the player position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, 0.5f);

            // Draw shadow distance radius
            if (useSpatialCulling)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(playerTransform.position, maxShadowDistance);
            }
        }
    }
}
