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

    [Header("Shadow Override")]
    [Tooltip("Optional light to use exclusively for shadow casting (e.g. a spotlight companion). Leave empty to use the attached light.")]
    [SerializeField] private Light shadowCasterOverride;

    [Header("Light Priority")]
    [Tooltip("Higher priority lights are more likely to cast shadows when at equal distance")]
    [Range(0, 10)]
    public int priority = 5;

    public enum LightLimitCategory
    {
        Unrestricted = 0,
        PlayerExplosion = 1
    }

    [Header("Light Limit")]
    [Tooltip("Only lights in PlayerExplosion category count toward DynamicShadowManager's light budget.")]
    [SerializeField] private LightLimitCategory lightLimitCategory = LightLimitCategory.Unrestricted;

    [Header("Shadow Settings")]
    [Tooltip("Shadow strength when this light is casting shadows")]
    [Range(0f, 1f)]
    public float shadowStrength = 1f;

    [Tooltip("Shadow bias - lower values = tighter shadows, may cause artifacts")]
    public float shadowBias = 0.05f;

    [Tooltip("Shadow normal bias - helps prevent shadow acne")]
    public float shadowNormalBias = 0.4f;

    private bool currentlyCastingShadows = false;
    private Light activeShadowLight;

    public bool IsManagedByShadowSystem => lightLimitCategory == LightLimitCategory.PlayerExplosion;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        activeShadowLight = shadowCasterOverride != null ? shadowCasterOverride : lightComponent;

        DisableShadowsImmediate(lightComponent);
        if (shadowCasterOverride != null)
        {
            DisableShadowsImmediate(shadowCasterOverride);
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
        DisableShadowsImmediate(lightComponent);
        if (shadowCasterOverride != null)
        {
            DisableShadowsImmediate(shadowCasterOverride);
        }
    }

    /// <summary>
    /// Called by DynamicShadowManager to enable/disable shadows on this light
    /// </summary>
    public void SetShadowsEnabled(bool enabled, bool useHardShadows)
    {
        if (activeShadowLight == null)
        {
            return;
        }

        currentlyCastingShadows = enabled;

        if (enabled)
        {
            activeShadowLight.shadows = useHardShadows ? LightShadows.Hard : LightShadows.Soft;
            activeShadowLight.shadowStrength = shadowStrength;
            activeShadowLight.shadowBias = shadowBias;
            activeShadowLight.shadowNormalBias = shadowNormalBias;
        }
        else
        {
            activeShadowLight.shadows = LightShadows.None;
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

    private static void DisableShadowsImmediate(Light light)
    {
        if (light == null)
        {
            return;
        }

        light.shadows = LightShadows.None;
    }
}
