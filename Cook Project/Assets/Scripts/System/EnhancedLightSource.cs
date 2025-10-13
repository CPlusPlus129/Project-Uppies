using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual light source that provides enhanced light recovery to the player when in proximity.
/// Attach this to light fixtures, lamps, or any object that should boost light recovery.
/// Also allows visual customization of the light's appearance.
/// Automatically creates a sphere collider and light bulb mesh if none exists.
/// 
/// IMPORTANT NOTE: If using a Unity Light component set to "Baked" mode, the Light component's
/// color and intensity cannot be changed at runtime due to Unity's baked lighting system.
/// Only the material's visual properties will update. Use "Mixed" or "Realtime" mode if you
/// need the Light component to update at runtime.
/// </summary>
public class EnhancedLightSource : MonoBehaviour
{
    [Header("Light Recovery Settings")]
    [Tooltip("Multiplier for light recovery rate when player is within range (e.g., 2.0 = 2x faster recovery)")]
    [SerializeField] [Range(1f, 10f)] private float recoveryMultiplier = 2f;
    
    [Tooltip("Effective range of this light source (radius of sphere collider)")]
    [SerializeField] private float effectiveRange = 3f;
    
    [Tooltip("If true, creates a sphere collider if none exists, or updates existing sphere collider radius")]
    [SerializeField] private bool autoCreateCollider = true;
    
    [Header("Visual Settings")]
    [Tooltip("If true, automatically creates a light bulb mesh if no MeshRenderer is assigned")]
    [SerializeField] private bool autoCreateLightBulb = true;
    
    [Tooltip("Size of the auto-created light bulb sphere (only used if Auto Create Light Bulb is enabled)")]
    [SerializeField] [Range(0.01f, 2f)] private float lightBulbSize = 0.15f;
    
    [Tooltip("Color of the light (affects material base color)")]
    [SerializeField] private Color lightColor = Color.white;
    
    [Tooltip("Enable emission glow effect")]
    [SerializeField] private bool enableEmission = true;
    
    [Tooltip("Emission color (can be different from base color for creative effects)")]
    [SerializeField] private Color emissionColor = new Color(1f, 0.95f, 0.8f); // Warm white
    
    [Tooltip("Emission intensity multiplier (higher = brighter glow)")]
    [SerializeField] [Range(0f, 10f)] private float emissionIntensity = 2f;
    
    [Tooltip("Reference to the MeshRenderer component (auto-created if not assigned and Auto Create Light Bulb is enabled)")]
    [SerializeField] private MeshRenderer meshRenderer;
    
    [Tooltip("Reference to Unity Light component (optional, for actual lighting)")]
    [SerializeField] private Light lightComponent;
    
    [Header("Advanced Settings")]
    [Tooltip("Tag to identify the player (default: 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("If true, light effect stacks with other light sources")]
    [SerializeField] private bool allowStacking = true;
    
    [Tooltip("Priority when multiple lights are nearby (higher = preferred). 0 = all lights are equal")]
    [SerializeField] [Range(0, 10)] private int priority = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0.5f, 0.3f);
    
    private Collider lightCollider;
    private Material materialInstance;
    private bool playerInRange = false;
    private bool hasWarnedAboutBakedLight = false;
    private bool materialsInitialized = false;
    private GameObject lightBulbObject;
    
    // Static tracking for all active light sources
    private static List<EnhancedLightSource> activeLightSources = new List<EnhancedLightSource>();
    
    /// <summary>
    /// Returns the highest priority light recovery multiplier from all active light sources.
    /// If stacking is allowed, returns the average of all active multipliers.
    /// </summary>
    public static float GetActiveLightBoostMultiplier()
    {
        if (activeLightSources.Count == 0)
            return 1f;
        
        // Check if any active lights allow stacking
        bool anyAllowStacking = false;
        foreach (var light in activeLightSources)
        {
            if (light != null && light.allowStacking)
            {
                anyAllowStacking = true;
                break;
            }
        }
        
        if (anyAllowStacking)
        {
            // Average all multipliers
            float totalMultiplier = 0f;
            int count = 0;
            foreach (var light in activeLightSources)
            {
                if (light != null)
                {
                    totalMultiplier += light.recoveryMultiplier;
                    count++;
                }
            }
            return count > 0 ? totalMultiplier / count : 1f;
        }
        else
        {
            // Find highest priority multiplier
            float highestMultiplier = 1f;
            int highestPriority = -1;
            
            foreach (var light in activeLightSources)
            {
                if (light != null)
                {
                    if (light.priority > highestPriority || 
                        (light.priority == highestPriority && light.recoveryMultiplier > highestMultiplier))
                    {
                        highestMultiplier = light.recoveryMultiplier;
                        highestPriority = light.priority;
                    }
                }
            }
            
            return highestMultiplier;
        }
    }
    
    /// <summary>
    /// Returns true if the player is within range of any enhanced light source.
    /// </summary>
    public static bool IsPlayerNearAnyLightSource()
    {
        // Clean up null references
        activeLightSources.RemoveAll(light => light == null);
        return activeLightSources.Count > 0;
    }
    
    private void Awake()
    {
        // Auto-detect Light component if present
        if (lightComponent == null)
        {
            lightComponent = GetComponent<Light>();
            
            if (lightComponent != null && showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Auto-detected Light component on {gameObject.name}");
            }
        }

#if UNITY_EDITOR
        // Check if Light is baked and warn user
        if (lightComponent != null && lightComponent.lightmapBakeType == LightmapBakeType.Baked && !hasWarnedAboutBakedLight)
        {
            Debug.LogWarning($"[EnhancedLightSource] Light component on '{gameObject.name}' is set to Baked mode. " +
                           $"Baked lights cannot have their visual properties changed at runtime. " +
                           $"Only the material's appearance will update. " +
                           $"Consider using 'Mixed' or 'Realtime' mode if you need runtime light updates.", this);
            hasWarnedAboutBakedLight = true;
        }
#endif

        // Setup or create collider
        SetupCollider();
        
        // Setup or create light bulb mesh
        SetupLightBulb();
        
        // Initialize materials
        InitializeMaterials();
    }
    
    private void SetupCollider()
    {
        lightCollider = GetComponent<Collider>();
        
        if (autoCreateCollider)
        {
            if (lightCollider == null)
            {
                // No collider exists - create a sphere collider
                SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
                sphereCol.radius = effectiveRange;
                lightCollider = sphereCol;
                
                if (showDebugMessages)
                {
                    Debug.Log($"[EnhancedLightSource] Created sphere collider with radius {effectiveRange} on {gameObject.name}");
                }
            }
            else if (lightCollider is SphereCollider existingSphere)
            {
                // Sphere collider exists - update it
                existingSphere.isTrigger = true;
                existingSphere.radius = effectiveRange;
            }
            else
            {
                // Non-sphere collider exists - just ensure it's a trigger
                lightCollider.isTrigger = true;
                
                if (showDebugMessages)
                {
                    Debug.LogWarning($"[EnhancedLightSource] Non-sphere collider detected on {gameObject.name}. Using existing collider but effective range may not match.");
                }
            }
        }
        else if (lightCollider != null)
        {
            // Auto-create disabled, but collider exists - just ensure it's a trigger
            lightCollider.isTrigger = true;
        }
        else
        {
            // Auto-create disabled and no collider exists - error
            Debug.LogError($"[EnhancedLightSource] No collider found on {gameObject.name} and Auto Create Collider is disabled! Please add a collider component or enable Auto Create Collider.", this);
        }
    }
    
    private void SetupLightBulb()
    {
        // Check if MeshRenderer is already assigned
        if (meshRenderer != null)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] MeshRenderer already assigned on {gameObject.name}");
            }
            return;
        }
        
        // Try to find MeshRenderer on this GameObject
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer != null)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Found MeshRenderer on {gameObject.name}");
            }
            return;
        }
        
        // Check if auto-create is enabled
        if (!autoCreateLightBulb)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[EnhancedLightSource] No MeshRenderer found on {gameObject.name} and Auto Create Light Bulb is disabled. Visual updates will not work.");
            }
            return;
        }
        
        // Check if we already created a light bulb (could happen on reset)
        lightBulbObject = transform.Find("LightBulb")?.gameObject;
        
        if (lightBulbObject != null)
        {
            meshRenderer = lightBulbObject.GetComponent<MeshRenderer>();
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Found existing LightBulb child on {gameObject.name}");
            }
            return;
        }
        
        // Create a new light bulb mesh
        CreateLightBulb();
    }
    
    private void CreateLightBulb()
    {
        // Create a sphere as a child
        lightBulbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightBulbObject.name = "LightBulb";
        lightBulbObject.transform.SetParent(transform);
        lightBulbObject.transform.localPosition = Vector3.zero;
        lightBulbObject.transform.localRotation = Quaternion.identity;
        lightBulbObject.transform.localScale = Vector3.one * lightBulbSize;
        
        // Remove the sphere collider (we don't need it, parent already has one)
        Collider bulbCollider = lightBulbObject.GetComponent<Collider>();
        if (bulbCollider != null)
        {
            DestroyImmediate(bulbCollider);
        }
        
        // Get the mesh renderer
        meshRenderer = lightBulbObject.GetComponent<MeshRenderer>();
        
        // Create a new emissive material
        Material bulbMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bulbMaterial.name = "LightBulb_Material";
        
        // Set up the material properties
        bulbMaterial.SetColor("_BaseColor", lightColor);
        bulbMaterial.EnableKeyword("_EMISSION");
        bulbMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        bulbMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        // Optional: Make it slightly transparent for a nicer glow effect
        // Uncomment these lines if you want transparency:
        // bulbMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        // bulbMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        // bulbMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        // bulbMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // bulbMaterial.SetFloat("_ZWrite", 0);
        // bulbMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        
        meshRenderer.material = bulbMaterial;
        
        if (showDebugMessages)
        {
            Debug.Log($"[EnhancedLightSource] Created light bulb mesh on {gameObject.name} with size {lightBulbSize}");
        }
    }
    
    private void InitializeMaterials()
    {
        if (materialsInitialized)
            return;
            
        // Create material instance for this light
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            // Create an instance of the material
            materialInstance = meshRenderer.material;
            
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Created material instance for {gameObject.name}. Shader: {materialInstance.shader.name}");
            }
            
            // Verify the material supports the properties we need
            if (!materialInstance.HasProperty("_BaseColor"))
            {
                Debug.LogWarning($"[EnhancedLightSource] Material on {gameObject.name} does not have '_BaseColor' property. " +
                               $"Visual updates may not work correctly. Shader: {materialInstance.shader.name}", this);
            }
            
            if (!materialInstance.HasProperty("_EmissionColor"))
            {
                Debug.LogWarning($"[EnhancedLightSource] Material on {gameObject.name} does not have '_EmissionColor' property. " +
                               $"Emission updates will not work. Shader: {materialInstance.shader.name}", this);
            }
            
            materialsInitialized = true;
        }
        else
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[EnhancedLightSource] No MeshRenderer or material found on {gameObject.name}. Visual updates will not work.");
            }
        }
    }
    
    private void Start()
    {
        // Ensure materials are initialized
        if (!materialsInitialized)
        {
            InitializeMaterials();
        }
        
        // Apply initial visual settings
        UpdateVisuals();
        
        if (showDebugMessages)
        {
            Debug.Log($"[EnhancedLightSource] Initialized on {gameObject.name}. " +
                     $"Recovery Multiplier: {recoveryMultiplier}x, " +
                     $"Emission Enabled: {enableEmission}, " +
                     $"Material Instance: {(materialInstance != null ? "Created" : "None")}");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
        
        // Remove from active list
        if (playerInRange)
        {
            activeLightSources.Remove(this);
        }
    }
    
    /// <summary>
    /// Update the visual appearance of the light based on current settings.
    /// Call this after changing color or emission settings at runtime.
    /// </summary>
    public void UpdateVisuals()
    {
        // Ensure materials are initialized first
        if (!materialsInitialized)
        {
            InitializeMaterials();
        }
        
        if (materialInstance == null)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[EnhancedLightSource] Cannot update visuals on {gameObject.name} - no material instance available.");
            }
            return;
        }
        
        bool updated = false;
        
        // Set base color (URP uses _BaseColor)
        if (materialInstance.HasProperty("_BaseColor"))
        {
            materialInstance.SetColor("_BaseColor", lightColor);
            updated = true;
            
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Updated _BaseColor to {lightColor} on {gameObject.name}");
            }
        }
        
        // Set emission
        if (materialInstance.HasProperty("_EmissionColor"))
        {
            if (enableEmission)
            {
                // Enable the emission keyword for URP
                materialInstance.EnableKeyword("_EMISSION");
                
                // Calculate final emission color with intensity
                // In URP, emission intensity is baked into the color value
                Color finalEmission = emissionColor * emissionIntensity;
                materialInstance.SetColor("_EmissionColor", finalEmission);
                
                // Also set the global illumination flags to ensure emission works
                materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                
                updated = true;
                
                if (showDebugMessages)
                {
                    Debug.Log($"[EnhancedLightSource] Enabled emission with color {finalEmission} (base: {emissionColor}, intensity: {emissionIntensity}) on {gameObject.name}");
                }
            }
            else
            {
                materialInstance.DisableKeyword("_EMISSION");
                materialInstance.SetColor("_EmissionColor", Color.black);
                
                updated = true;
                
                if (showDebugMessages)
                {
                    Debug.Log($"[EnhancedLightSource] Disabled emission on {gameObject.name}");
                }
            }
        }

#if UNITY_EDITOR
        // Update Unity Light component if present and not baked
        if (lightComponent != null)
        {
            if (lightComponent.lightmapBakeType != LightmapBakeType.Baked)
            {
                lightComponent.color = emissionColor;
                lightComponent.intensity = emissionIntensity;
                updated = true;
                
                if (showDebugMessages)
                {
                    Debug.Log($"[EnhancedLightSource] Updated Light component color and intensity on {gameObject.name}");
                }
            }
            else if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Skipped Light component update on {gameObject.name} (Baked mode)");
            }
        }
#endif

        if (!updated && showDebugMessages)
        {
            Debug.LogWarning($"[EnhancedLightSource] No properties were updated on {gameObject.name}. Check material setup.");
        }
    }
    
    /// <summary>
    /// Set the light color and update visuals.
    /// </summary>
    public void SetLightColor(Color color)
    {
        lightColor = color;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set the emission color and update visuals.
    /// </summary>
    public void SetEmissionColor(Color color, float intensity = -1f)
    {
        emissionColor = color;
        if (intensity >= 0f)
        {
            emissionIntensity = intensity;
        }
        UpdateVisuals();
    }
    
    /// <summary>
    /// Enable or disable emission glow.
    /// </summary>
    public void SetEmissionEnabled(bool enabled)
    {
        enableEmission = enabled;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set the recovery multiplier at runtime.
    /// </summary>
    public void SetRecoveryMultiplier(float multiplier)
    {
        recoveryMultiplier = Mathf.Clamp(multiplier, 1f, 10f);
    }
    
    /// <summary>
    /// Set the light bulb size at runtime (only works if auto-created light bulb exists).
    /// </summary>
    public void SetLightBulbSize(float size)
    {
        lightBulbSize = Mathf.Clamp(size, 0.01f, 2f);
        if (lightBulbObject != null)
        {
            lightBulbObject.transform.localScale = Vector3.one * lightBulbSize;
        }
    }
    
    /// <summary>
    /// Force re-initialization of materials. Use if material changes after Start().
    /// </summary>
    public void ReinitializeMaterials()
    {
        materialsInitialized = false;
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
        InitializeMaterials();
        UpdateVisuals();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            
            // Add to active list
            if (!activeLightSources.Contains(this))
            {
                activeLightSources.Add(this);
            }
            
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Player entered range of light source: {gameObject.name} (Multiplier: {recoveryMultiplier}x, Active lights: {activeLightSources.Count})");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            
            // Remove from active list
            activeLightSources.Remove(this);
            
            if (showDebugMessages)
            {
                Debug.Log($"[EnhancedLightSource] Player exited range of light source: {gameObject.name} (Active lights: {activeLightSources.Count})");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        // Draw range indicator
        Color drawColor = gizmoColor;
        if (Application.isPlaying && playerInRange)
        {
            // Yellow when player is in range
            drawColor = new Color(1f, 1f, 0f, 0.5f);
        }
        
        Gizmos.color = drawColor;
        Gizmos.DrawWireSphere(transform.position, effectiveRange);
        
        // Draw filled sphere for better visibility
        Gizmos.color = new Color(drawColor.r, drawColor.g, drawColor.b, drawColor.a * 0.3f);
        Gizmos.DrawSphere(transform.position, effectiveRange);
        
        #if UNITY_EDITOR
        // Draw label
        string statusText = $"Light Source\n{recoveryMultiplier}x Recovery";
        if (Application.isPlaying && playerInRange)
        {
            statusText += "\n(ACTIVE)";
        }
        if (priority > 0)
        {
            statusText += $"\nPriority: {priority}";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * (effectiveRange + 0.5f), statusText);
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Draw a brighter range indicator when selected
        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, effectiveRange);
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Update collider when effective range changes in editor
        if (autoCreateCollider && Application.isPlaying)
        {
            Collider col = GetComponent<Collider>();
            if (col is SphereCollider sphereCol)
            {
                sphereCol.radius = effectiveRange;
            }
        }
        
        // Update light bulb size if it exists
        if (lightBulbObject != null)
        {
            lightBulbObject.transform.localScale = Vector3.one * lightBulbSize;
        }
        
        // Update visuals when properties change in inspector (only in play mode)
        if (Application.isPlaying)
        {
            // Ensure materials are initialized before updating
            if (!materialsInitialized)
            {
                InitializeMaterials();
            }
            
            if (materialInstance != null)
            {
                UpdateVisuals();
            }
        }
    }
    
    /// <summary>
    /// Context menu item to manually update visuals in the editor
    /// </summary>
    [ContextMenu("Force Update Visuals")]
    private void ForceUpdateVisuals()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[EnhancedLightSource] Force Update Visuals only works in Play mode.");
            return;
        }
        
        if (!materialsInitialized)
        {
            InitializeMaterials();
        }
        
        UpdateVisuals();
        Debug.Log($"[EnhancedLightSource] Forced visual update on {gameObject.name}");
    }
    
    /// <summary>
    /// Context menu item to reinitialize materials
    /// </summary>
    [ContextMenu("Reinitialize Materials")]
    private void ForceReinitializeMaterials()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[EnhancedLightSource] Reinitialize Materials only works in Play mode.");
            return;
        }
        
        ReinitializeMaterials();
        Debug.Log($"[EnhancedLightSource] Reinitialized materials on {gameObject.name}");
    }
    
    /// <summary>
    /// Context menu item to recreate the light bulb mesh
    /// </summary>
    [ContextMenu("Recreate Light Bulb")]
    private void RecreateLightBulb()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[EnhancedLightSource] Recreate Light Bulb only works in Play mode.");
            return;
        }
        
        // Destroy old light bulb if it exists
        if (lightBulbObject != null)
        {
            DestroyImmediate(lightBulbObject);
            lightBulbObject = null;
        }
        
        // Clear mesh renderer reference
        meshRenderer = null;
        materialsInitialized = false;
        
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
        
        // Recreate
        SetupLightBulb();
        InitializeMaterials();
        UpdateVisuals();
        
        Debug.Log($"[EnhancedLightSource] Recreated light bulb on {gameObject.name}");
    }
    #endif
}
