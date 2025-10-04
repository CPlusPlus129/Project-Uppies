using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controls fog intensity when player enters/exits a trigger zone.
/// Attach this to a GameObject with a Box Collider (Is Trigger = true).
/// </summary>
public class FogZoneController : MonoBehaviour
{
    [Header("Fog Zone Settings")]
    [Tooltip("The volume profile to use when inside this fog zone")]
    [SerializeField] private VolumeProfile fogProfile;
    
    [Tooltip("Transition duration when entering fog zone")]
    [SerializeField] private float fadeInDuration = 2f;
    
    [Tooltip("Transition duration when exiting fog zone")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    
    [Tooltip("Tag of the player object (usually 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Fog Settings")]
    [Tooltip("Enable Unity's built-in fog when player is in zone")]
    [SerializeField] private bool useBuiltInFog = true;
    
    [Tooltip("Fog density when active")]
    [SerializeField, Range(0f, 1f)] private float fogDensity = 0.01f;
    
    [Tooltip("Fog color")]
    [SerializeField] private Color fogColor = new Color(0.02f, 0.02f, 0.05f);
    
    // Private variables
    private Volume zoneVolume;
    private float targetWeight = 0f;
    private float currentWeight = 0f;
    private bool playerInZone = false;
    
    // Store original fog settings
    private bool originalFogEnabled;
    private float originalFogDensity;
    private Color originalFogColor;
    private FogMode originalFogMode;

    private void Awake()
    {
        // Get or create Volume component
        zoneVolume = GetComponent<Volume>();
        if (zoneVolume == null)
        {
            zoneVolume = gameObject.AddComponent<Volume>();
        }
        
        // Configure volume
        zoneVolume.isGlobal = false;
        zoneVolume.weight = 0f;
        zoneVolume.priority = 10;  // Higher priority to override default volume profile
        
        // EXCLUDE UI LAYER - Volume will not affect UI elements
        // Create layer mask that includes everything EXCEPT UI layer
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer != -1)
        {
            // Create mask with all layers enabled, then remove UI layer
            LayerMask excludeUI = ~0 & ~(1 << uiLayer);
            // Note: In Unity 6, there's no direct volumeMask property on Volume.
            // Instead, we'll handle this through Camera's Volume Layer Mask setting.
            // See instructions below for camera setup.
        }
        
        // Assign profile if provided
        if (fogProfile != null)
        {
            zoneVolume.profile = fogProfile;
        }
        
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"FogZoneController on {gameObject.name}: Collider is not set as trigger. Setting it now.");
            col.isTrigger = true;
        }
        
        // Store original fog settings
        if (useBuiltInFog)
        {
            originalFogEnabled = RenderSettings.fog;
            originalFogDensity = RenderSettings.fogDensity;
            originalFogColor = RenderSettings.fogColor;
            originalFogMode = RenderSettings.fogMode;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;
            targetWeight = 1f;
            
            if (useBuiltInFog)
            {
                EnableBuiltInFog();
            }
            
            Debug.Log($"Player entered fog zone: {gameObject.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
            targetWeight = 0f;
            
            Debug.Log($"Player exited fog zone: {gameObject.name}");
        }
    }

    private void Update()
    {
        // Smooth linear transition
        if (Mathf.Abs(currentWeight - targetWeight) > 0.001f)
        {
            // Calculate speed based on whether we're fading in or out
            float duration = targetWeight > currentWeight ? fadeInDuration : fadeOutDuration;
            float speed = 1f / duration;
            
            // Move towards target at constant speed for linear interpolation
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, speed * Time.deltaTime);
            
            if (zoneVolume != null)
            {
                zoneVolume.weight = currentWeight;
            }
            
            // Update built-in fog settings smoothly based on current weight
            if (useBuiltInFog)
            {
                // Ensure fog is enabled during transition
                if (!RenderSettings.fog && currentWeight > 0.001f)
                {
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                }
                
                // Keep fog color constant at target color for more subtle fade
                // Only animate the density to make the transition less jarring
                if (playerInZone || currentWeight > 0.001f)
                {
                    RenderSettings.fogColor = fogColor;
                }
                
                // Lerp fog density based on current weight
                RenderSettings.fogDensity = Mathf.Lerp(originalFogDensity, fogDensity, currentWeight);
            }
        }
        else if (useBuiltInFog && !playerInZone && currentWeight <= 0.001f)
        {
            // Transition complete - restore original fog settings
            currentWeight = 0f;
            if (zoneVolume != null)
            {
                zoneVolume.weight = 0f;
            }
            RestoreOriginalFogSettings();
        }
    }

    private void EnableBuiltInFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
    }

    private void DisableBuiltInFog()
    {
        // This method is no longer called immediately on exit
        // Fog settings are now restored gradually in Update()
        RestoreOriginalFogSettings();
    }
    
    private void RestoreOriginalFogSettings()
    {
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;
    }

    private void OnDrawGizmos()
    {
        // Visualize fog zone in editor
        Gizmos.color = new Color(0.5f, 0.5f, 0.8f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            
            Gizmos.color = new Color(0.5f, 0.5f, 0.8f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }

    private void OnValidate()
    {
        // Update volume reference if it exists
        if (zoneVolume == null)
        {
            zoneVolume = GetComponent<Volume>();
        }
    }
}
