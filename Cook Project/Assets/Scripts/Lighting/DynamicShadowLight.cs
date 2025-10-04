using UnityEngine;

/// <summary>
/// Add this component to any light that should participate in dynamic shadow management.
/// The light will only cast shadows when it's one of the closest lights to the player.
/// </summary>
[RequireComponent(typeof(Light))]
public class DynamicShadowLight : MonoBehaviour
{
    [HideInInspector]
    public Light lightComponent;

    [Header("Light Priority")]
    [Tooltip("Higher priority lights are more likely to cast shadows when at equal distance")]
    [Range(0, 10)]
    public int priority = 5;

    [Header("Shadow Settings")]
    [Tooltip("Shadow strength when this light is casting shadows")]
    [Range(0f, 1f)]
    public float shadowStrength = 1f;

    [Tooltip("Shadow bias - lower values = tighter shadows, may cause artifacts")]
    public float shadowBias = 0.05f;

    [Tooltip("Shadow normal bias - helps prevent shadow acne")]
    public float shadowNormalBias = 0.4f;

    private bool currentlyCastingShadows = false;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        
        // Ensure shadows are disabled by default
        if (lightComponent != null)
        {
            lightComponent.shadows = LightShadows.None;
        }
    }

    private void OnEnable()
    {
        // Register with the manager
        if (DynamicShadowManager.Instance != null)
        {
            DynamicShadowManager.Instance.RegisterLight(this);
        }
        else
        {
            Debug.LogWarning($"DynamicShadowLight on {gameObject.name}: No DynamicShadowManager found in scene!");
        }
    }

    private void OnDisable()
    {
        // Unregister from the manager
        if (DynamicShadowManager.Instance != null)
        {
            DynamicShadowManager.Instance.UnregisterLight(this);
        }

        // Ensure shadows are disabled when the light is disabled
        if (lightComponent != null)
        {
            lightComponent.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// Called by DynamicShadowManager to enable/disable shadows on this light
    /// </summary>
    public void SetShadowsEnabled(bool enabled, bool useHardShadows)
    {
        if (lightComponent == null) return;

        currentlyCastingShadows = enabled;

        if (enabled)
        {
            // Enable shadows with specified quality
            lightComponent.shadows = useHardShadows ? LightShadows.Hard : LightShadows.Soft;
            lightComponent.shadowStrength = shadowStrength;
            lightComponent.shadowBias = shadowBias;
            lightComponent.shadowNormalBias = shadowNormalBias;
        }
        else
        {
            // Disable shadows
            lightComponent.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// Check if this light is currently casting shadows
    /// </summary>
    public bool IsCastingShadows()
    {
        return currentlyCastingShadows;
    }

    private void OnDrawGizmos()
    {
        // Visual indicator in scene view
        if (currentlyCastingShadows)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
