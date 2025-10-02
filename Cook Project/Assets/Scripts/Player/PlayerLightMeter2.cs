using UnityEngine;

public class PlayerLightMeter2 : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private bool includeAmbientLight = true;
    
    [Header("Sensitivity Settings")]
    [SerializeField] [Range(0.01f, 5f)] private float lightSensitivity = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float ambientStrength = 0.1f;
    [SerializeField] private bool useRealisticFalloff = true;
    
    [Header("Performance")]
    [SerializeField] [Range(1, 64)] private int updatesPerSecond = 30;
    
    [Header("Output")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Current light level (0-1 range, where 0 is dark and 1 is bright)
    private float currentLightLevel = 0f;
    private Color currentLightColor = Color.black;
    private float nextUpdateTime = 0f;
    
    // Public properties
    public float LightLevel => currentLightLevel;
    public Color LightColor => currentLightColor;
    public bool IsInDarkness => currentLightLevel < 0.2f;
    public bool IsInBrightLight => currentLightLevel > 0.7f;
    
    private void Update()
    {
        // Calculate update interval from updates per second
        float updateInterval = 1f / updatesPerSecond;
        
        // Update light level based on interval
        if (Time.time >= nextUpdateTime)
        {
            CalculateLightLevel();
            nextUpdateTime = Time.time + updateInterval;
            
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }
    }
    
    private void CalculateLightLevel()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        float totalIntensity = 0f;
        Color totalLight = Color.black;
        
        // Add ambient light with reduced strength
        if (includeAmbientLight)
        {
            float ambientContribution = RenderSettings.ambientIntensity * ambientStrength;
            totalLight += RenderSettings.ambientLight * ambientContribution;
            totalIntensity += ambientContribution;
        }
        
        foreach (Light light in allLights)
        {
            if (!light.enabled) continue;
            
            float contribution = 0f;
            Vector3 toLight = light.transform.position - transform.position;
            float distance = toLight.magnitude;
            
            // Skip lights that are too far away
            if (light.type != LightType.Directional && distance > detectionRadius)
                continue;
            
            switch (light.type)
            {
                case LightType.Directional:
                    // Scale down directional lights as they're typically very bright
                    contribution = light.intensity * 0.1f;
                    break;
                    
                case LightType.Point:
                    if (distance <= light.range)
                    {
                        if (useRealisticFalloff)
                        {
                            // Realistic inverse square law
                            float distanceFactor = distance / light.range;
                            float attenuation = 1.0f / (1.0f + 25.0f * distanceFactor * distanceFactor);
                            contribution = light.intensity * attenuation * 0.01f; // Scale down
                        }
                        else
                        {
                            // Linear falloff (more predictable)
                            float attenuation = 1.0f - (distance / light.range);
                            contribution = light.intensity * attenuation * 0.05f;
                        }
                    }
                    break;
                    
                case LightType.Spot:
                    if (distance <= light.range)
                    {
                        Vector3 dirToPoint = -toLight.normalized; // Direction from light to point
                        Vector3 lightDir = light.transform.forward;
                        float angle = Vector3.Angle(lightDir, dirToPoint);
                        
                        if (angle < light.spotAngle / 2f)
                        {
                            float distanceAttenuation;
                            if (useRealisticFalloff)
                            {
                                float distanceFactor = distance / light.range;
                                distanceAttenuation = 1.0f / (1.0f + 25.0f * distanceFactor * distanceFactor);
                            }
                            else
                            {
                                distanceAttenuation = 1.0f - (distance / light.range);
                            }
                            
                            // Smooth spot angle falloff
                            float angleNormalized = angle / (light.spotAngle / 2f);
                            float spotEffect = Mathf.Pow(1.0f - angleNormalized, 2f);
                            
                            contribution = light.intensity * distanceAttenuation * spotEffect * 0.05f;
                        }
                    }
                    break;
            }
            
            if (contribution > 0)
            {
                totalLight += light.color * contribution;
                totalIntensity += contribution;
            }
        }
        
        // Apply sensitivity multiplier
        totalIntensity *= lightSensitivity;
        totalLight *= lightSensitivity;
        
        // Smooth clamping to avoid harsh cutoff at 1.0
        currentLightLevel = Mathf.Clamp01(1.0f - Mathf.Exp(-totalIntensity * 2f));
        currentLightColor = totalLight;
    }
    
    private void DisplayDebugInfo()
    {
        string status = IsInDarkness ? "DARK" : IsInBrightLight ? "BRIGHT" : "MEDIUM";
        
        // Show detailed debug info
        int lightCount = FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
        Debug.Log($"[{Time.frameCount}] Light Level: {currentLightLevel:F3} ({status}) | " +
                  $"Color: RGBA({currentLightColor.r:F3}, {currentLightColor.g:F3}, {currentLightColor.b:F3}, {currentLightColor.a:F3}) | " +
                  $"Lights in scene: {lightCount}");
    }
    
    // Optional: Visualize detection area
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(currentLightLevel, currentLightLevel, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
    
    // Public methods for gameplay use
    public bool IsPlayerVisible(float visibilityThreshold = 0.3f)
    {
        return currentLightLevel > visibilityThreshold;
    }
    
    public float GetLightIntensity()
    {
        return currentLightLevel;
    }
}