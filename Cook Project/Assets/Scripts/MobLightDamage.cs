using UnityEngine;

/// <summary>
/// Damages the mob when they are in bright light (HIGH light level).
/// Opposite of the player's darkness damage system.
/// Uses the same light detection approach as PlayerLightMeter2.
/// </summary>
public class MobLightDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Mob mob;
    
    [Header("Light Detection Settings")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private bool includeAmbientLight = true;
    
    [Header("Sensitivity Settings")]
    [SerializeField] [Range(0.01f, 5f)] private float lightSensitivity = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float ambientStrength = 0.1f;
    [SerializeField] private bool useRealisticFalloff = true;
    
    [Header("Damage Settings")]
    [Tooltip("Light level threshold above which damage starts (0-1). Mob takes damage in BRIGHT areas.")]
    [SerializeField] [Range(0f, 1f)] private float damageThreshold = 0.5f;
    [Tooltip("Damage per second when in bright light")]
    [SerializeField] private float damagePerSecond = 5f;
    [Tooltip("Time in seconds before damage starts after entering bright area")]
    [SerializeField] private float damageDelay = 0.5f;
    
    [Header("Performance")]
    [SerializeField] [Range(1, 64)] private int updatesPerSecond = 30;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showDetailedLightInfo = false;
    [SerializeField] private bool showPerFrameUpdates = false;
    [SerializeField] private Color damageGizmoColor = Color.yellow;
    [SerializeField] private Color lightGizmoColor = Color.cyan;
    
    // Current light level (0-1 range, where 0 is dark and 1 is bright)
    private float currentLightLevel = 0f;
    private float nextUpdateTime = 0f;
    private int nearbyLightCount = 0;
    private float rawLightIntensity = 0f;
    
    // Damage state
    private float timeInBrightLight = 0f;
    private bool isTakingDamage = false;
    private float accumulatedDamage = 0f;
    private float lastDebugTime = 0f;
    
    // Public properties
    public float LightLevel => currentLightLevel;
    public bool IsTakingDamage => isTakingDamage;
    public float TimeInBrightLight => timeInBrightLight;
    
    private void Start()
    {
        // Auto-find mob component if not assigned
        if (mob == null)
        {
            mob = GetComponent<Mob>();
            if (mob == null)
            {
                Debug.LogError("MobLightDamage: No Mob component found! Please assign one or add it to the mob.", this);
                enabled = false;
                return;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MobLightDamage] Initialized on {gameObject.name}");
            Debug.Log($"  - Damage Threshold: {damageThreshold} (damage above this)");
            Debug.Log($"  - Damage Per Second: {damagePerSecond}");
            Debug.Log($"  - Detection Radius: {detectionRadius}");
            Debug.Log($"  - Light Sensitivity: {lightSensitivity}");
            Debug.Log($"  - Updates Per Second: {updatesPerSecond}");
        }
    }
    
    private void Update()
    {
        // Calculate update interval from updates per second
        float updateInterval = 1f / updatesPerSecond;
        
        // Update light level based on interval
        if (Time.time >= nextUpdateTime)
        {
            CalculateLightLevel();
            nextUpdateTime = Time.time + updateInterval;
            
            if (showPerFrameUpdates)
            {
                Debug.Log($"[MobLight] Frame {Time.frameCount} - Light: {currentLightLevel:F3} | Threshold: {damageThreshold} | Above Threshold: {currentLightLevel > damageThreshold}");
            }
        }
        
        // Check for light damage every frame for smooth damage application
        CheckLightDamage();
        
        // Periodic detailed status report
        if (showDebugInfo && Time.time - lastDebugTime >= 2f)
        {
            LogDetailedStatus();
            lastDebugTime = Time.time;
        }
    }
    
    private void CalculateLightLevel()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        float totalIntensity = 0f;
        nearbyLightCount = 0;
        
        if (showDetailedLightInfo)
        {
            Debug.Log($"[MobLight] Checking {allLights.Length} lights in scene...");
        }
        
        // Add ambient light with reduced strength
        if (includeAmbientLight)
        {
            float ambientContribution = RenderSettings.ambientIntensity * ambientStrength;
            totalIntensity += ambientContribution;
            
            if (showDetailedLightInfo)
            {
                Debug.Log($"  Ambient: {ambientContribution:F4} (intensity: {RenderSettings.ambientIntensity}, strength: {ambientStrength})");
            }
        }
        
        foreach (Light light in allLights)
        {
            if (!light.enabled) continue;
            
            float contribution = 0f;
            Vector3 toLight = light.transform.position - transform.position;
            float distance = toLight.magnitude;
            
            // Skip lights that are too far away
            if (light.type != LightType.Directional && distance > detectionRadius)
            {
                if (showDetailedLightInfo)
                {
                    Debug.Log($"  {light.name} ({light.type}): TOO FAR ({distance:F1}m > {detectionRadius}m)");
                }
                continue;
            }
            
            switch (light.type)
            {
                case LightType.Directional:
                    // Scale down directional lights as they're typically very bright
                    contribution = light.intensity * 0.1f;
                    if (showDetailedLightInfo)
                    {
                        Debug.Log($"  {light.name} (Directional): contribution={contribution:F4}");
                    }
                    break;
                    
                case LightType.Point:
                    if (distance <= light.range)
                    {
                        if (useRealisticFalloff)
                        {
                            // Realistic inverse square law
                            float distanceFactor = distance / light.range;
                            float attenuation = 1.0f / (1.0f + 25.0f * distanceFactor * distanceFactor);
                            contribution = light.intensity * attenuation * 0.01f;
                        }
                        else
                        {
                            // Linear falloff (more predictable)
                            float attenuation = 1.0f - (distance / light.range);
                            contribution = light.intensity * attenuation * 0.05f;
                        }
                        
                        if (showDetailedLightInfo)
                        {
                            Debug.Log($"  {light.name} (Point): dist={distance:F1}m, range={light.range:F1}m, intensity={light.intensity}, contribution={contribution:F4}");
                        }
                    }
                    else if (showDetailedLightInfo)
                    {
                        Debug.Log($"  {light.name} (Point): OUT OF RANGE ({distance:F1}m > {light.range:F1}m)");
                    }
                    break;
                    
                case LightType.Spot:
                    if (distance <= light.range)
                    {
                        Vector3 dirToPoint = -toLight.normalized;
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
                            
                            float angleNormalized = angle / (light.spotAngle / 2f);
                            float spotEffect = Mathf.Pow(1.0f - angleNormalized, 2f);
                            
                            contribution = light.intensity * distanceAttenuation * spotEffect * 0.05f;
                            
                            if (showDetailedLightInfo)
                            {
                                Debug.Log($"  {light.name} (Spot): dist={distance:F1}m, angle={angle:F1}°, contribution={contribution:F4}");
                            }
                        }
                        else if (showDetailedLightInfo)
                        {
                            Debug.Log($"  {light.name} (Spot): OUTSIDE CONE (angle={angle:F1}° > {light.spotAngle/2f:F1}°)");
                        }
                    }
                    else if (showDetailedLightInfo)
                    {
                        Debug.Log($"  {light.name} (Spot): OUT OF RANGE ({distance:F1}m > {light.range:F1}m)");
                    }
                    break;
            }
            
            if (contribution > 0)
            {
                totalIntensity += contribution;
                nearbyLightCount++;
            }
        }
        
        // Store raw intensity before sensitivity
        rawLightIntensity = totalIntensity;
        
        // Apply sensitivity multiplier
        totalIntensity *= lightSensitivity;
        
        // Smooth clamping to avoid harsh cutoff at 1.0
        currentLightLevel = Mathf.Clamp01(1.0f - Mathf.Exp(-totalIntensity * 2f));
        
        if (showDetailedLightInfo)
        {
            Debug.Log($"[MobLight] FINAL: Raw={rawLightIntensity:F4}, AfterSensitivity={totalIntensity:F4}, Level={currentLightLevel:F3}, Lights={nearbyLightCount}");
        }
    }
    
    private void CheckLightDamage()
    {
        if (!mob.IsAlive) return;
        
        // Check if mob is in BRIGHT light (opposite of player taking damage from darkness)
        bool inBrightLight = currentLightLevel > damageThreshold;
        
        if (inBrightLight)
        {
            // Accumulate time in bright light
            timeInBrightLight += Time.deltaTime;
            
            // Start damaging after delay
            if (timeInBrightLight >= damageDelay)
            {
                if (!isTakingDamage)
                {
                    isTakingDamage = true;
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"[MobLightDamage] {gameObject.name} is NOW TAKING DAMAGE! Light: {currentLightLevel:F3} > Threshold: {damageThreshold}");
                    }
                }
                
                // Apply damage
                ApplyLightDamage();
            }
            else
            {
                if (showPerFrameUpdates)
                {
                    Debug.Log($"[MobLight] In bright light, waiting... {timeInBrightLight:F2}s / {damageDelay:F2}s");
                }
            }
        }
        else
        {
            // Reset when entering darkness
            if (timeInBrightLight > 0f)
            {
                timeInBrightLight = 0f;
                isTakingDamage = false;
                accumulatedDamage = 0f;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[MobLightDamage] {gameObject.name} entered DARK area. Light: {currentLightLevel:F3} <= Threshold: {damageThreshold}");
                }
            }
        }
    }
    
    private void ApplyLightDamage()
    {
        // Accumulate fractional damage
        float damageThisFrame = damagePerSecond * Time.deltaTime;
        accumulatedDamage += damageThisFrame;
        
        // Only apply damage when we have at least 1 full HP worth
        if (accumulatedDamage >= 1f)
        {
            int damageToApply = Mathf.FloorToInt(accumulatedDamage);
            accumulatedDamage -= damageToApply;
            
            // Apply to mob health
            mob.TakeDamage(damageToApply);
            
            // Damage nearest light source when mob takes damage from light
            mob.DamageNearestLightSource();
            
            if (showDebugInfo)
            {
                Debug.Log($"[MobLightDamage] Applied {damageToApply} damage! Mob HP: {mob.CurrentHealth}/{mob.MaxHealth} (accumulated: {accumulatedDamage:F2})");
            }
        }
    }
    
    private void LogDetailedStatus()
    {
        string status = isTakingDamage ? "TAKING DAMAGE" : 
                       (currentLightLevel > damageThreshold ? "BRIGHT (in delay)" : "SAFE (dark)");
        
        Debug.Log($"[MobLightDamage] STATUS for {gameObject.name}:");
        Debug.Log($"  Light Level: {currentLightLevel:F3} | Threshold: {damageThreshold} | Raw Intensity: {rawLightIntensity:F4}");
        Debug.Log($"  Nearby Lights: {nearbyLightCount} | Detection Radius: {detectionRadius}");
        Debug.Log($"  Status: {status} | Time in Bright: {timeInBrightLight:F2}s");
        Debug.Log($"  Mob Health: {mob.CurrentHealth}/{mob.MaxHealth} | Accumulated Damage: {accumulatedDamage:F2}");
    }
    
    // Visualize damage state in editor
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw light level indicator sphere above mob
        Vector3 indicatorPos = transform.position + Vector3.up * 2.5f;
        Gizmos.color = new Color(currentLightLevel, currentLightLevel, currentLightLevel, 1f);
        Gizmos.DrawSphere(indicatorPos, 0.3f);
        
        // Draw damage state
        if (isTakingDamage)
        {
            Gizmos.color = damageGizmoColor;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Draw rays to nearby lights
            Gizmos.color = Color.red;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (!light.enabled) continue;
                float dist = Vector3.Distance(transform.position, light.transform.position);
                if (light.type != LightType.Directional && dist <= detectionRadius)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up, light.transform.position);
                }
            }
        }
        
        // Draw detection radius
        Gizmos.color = new Color(lightGizmoColor.r, lightGizmoColor.g, lightGizmoColor.b, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw threshold indicator
        Gizmos.color = Color.yellow;
        float sphereSize = 0.5f + (currentLightLevel - damageThreshold) * 2f;
        if (sphereSize > 0)
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, Mathf.Max(0.1f, sphereSize));
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw even more detail when selected
        if (!Application.isPlaying) return;
        
        // Draw light contribution from each nearby light
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (!light.enabled) continue;
            
            float dist = Vector3.Distance(transform.position, light.transform.position);
            
            if (light.type == LightType.Directional || dist <= detectionRadius)
            {
                // Color based on contribution
                float alpha = Mathf.Min(dist / detectionRadius, 1f);
                Gizmos.color = new Color(1f, 1f, 0f, 1f - alpha);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, light.transform.position);
                
                // Draw light's effective range
                if (light.type == LightType.Point)
                {
                    Gizmos.color = new Color(light.color.r, light.color.g, light.color.b, 0.1f);
                    Gizmos.DrawWireSphere(light.transform.position, light.range);
                }
            }
        }
    }
}
