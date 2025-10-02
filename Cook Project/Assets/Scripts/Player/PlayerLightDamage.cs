using UnityEngine;

/// <summary>
/// Damages the player when they are in darkness (low light), unless they're in a safe zone.
/// </summary>
public class PlayerLightDamage : MonoBehaviour
{
    [Header("Light Detection")]
    [SerializeField] private PlayerLightMeter2 lightMeter;
    [Tooltip("Light level threshold below which damage starts (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float damageThreshold = 0.3f;
    
    [Header("Damage Settings")]
    [Tooltip("Damage per second when in darkness")]
    [SerializeField] private float damagePerSecond = 2f;
    [Tooltip("Time in seconds before damage starts after entering darkness")]
    [SerializeField] private float damageDelay = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showWarningMessages = true;
    [SerializeField] private Color damageGizmoColor = Color.red;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Internal state
    private bool isInSafeZone = false;
    private float timeInDarkness = 0f;
    private bool isTakingDamage = false;
    private float lastDamageTime = 0f;
    private float accumulatedDamage = 0f; // Fix: Accumulate fractional damage
    
    // Public properties
    public bool IsInSafeZone => isInSafeZone;
    public bool IsTakingDamage => isTakingDamage;
    public float TimeInDarkness => timeInDarkness;
    
    private void Start()
    {
        // Auto-find light meter if not assigned
        if (lightMeter == null)
        {
            lightMeter = GetComponent<PlayerLightMeter2>();
            if (lightMeter == null)
            {
                Debug.LogError("PlayerLightDamage: No PlayerLightMeter2 found! Please assign one or add it to the player.", this);
                enabled = false;
                return;
            }
        }
        
        // Verify PlayerStatSystem is available
        var healthSystem = PlayerStatSystem.Instance;
        if (healthSystem == null)
        {
            Debug.LogError("PlayerLightDamage: PlayerStatSystem.Instance is NULL! Make sure PlayerStatSystem exists in the scene.", this);
            enabled = false;
            return;
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"PlayerLightDamage: Successfully found PlayerStatSystem. Current HP: {healthSystem.CurrentHP.Value}/{healthSystem.MaxHP.Value}");
            }
        }
    }
    
    private void Update()
    {
        CheckLightDamage();
    }
    
    private void CheckLightDamage()
    {
        // Don't damage if in safe zone
        if (isInSafeZone)
        {
            timeInDarkness = 0f;
            isTakingDamage = false;
            accumulatedDamage = 0f; // Reset accumulated damage
            return;
        }
        
        // Check if player is in darkness (LOW light level)
        float currentLightLevel = lightMeter.LightLevel;
        bool inDarkness = currentLightLevel < damageThreshold;
        
        if (inDarkness)
        {
            // Accumulate time in darkness
            timeInDarkness += Time.deltaTime;
            
            // Start damaging after delay
            if (timeInDarkness >= damageDelay)
            {
                if (!isTakingDamage)
                {
                    isTakingDamage = true;
                    if (showWarningMessages)
                    {
                        Debug.LogWarning($"Player is taking darkness damage! Light Level: {currentLightLevel:F3} (threshold: {damageThreshold})");
                    }
                }
                
                // Apply damage
                ApplyLightDamage();
            }
            else if (showDebugInfo)
            {
                Debug.Log($"In darkness ({currentLightLevel:F3}), waiting for delay... {timeInDarkness:F2}s / {damageDelay:F2}s");
            }
        }
        else
        {
            // Reset when entering light
            if (timeInDarkness > 0f)
            {
                timeInDarkness = 0f;
                isTakingDamage = false;
                accumulatedDamage = 0f; // Reset accumulated damage
                
                if (showDebugInfo)
                {
                    Debug.Log($"Player entered lit area. Light Level: {currentLightLevel:F3}");
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
            accumulatedDamage -= damageToApply; // Keep the remainder
            
            // Apply to health system
            var healthSystem = PlayerStatSystem.Instance;
            if (healthSystem == null)
            {
                Debug.LogError("PlayerStatSystem.Instance is NULL when trying to apply damage!");
                return;
            }
            
            int currentHP = healthSystem.CurrentHP.Value;
            int newHP = Mathf.Max(0, currentHP - damageToApply);
            
            if (showDebugInfo)
            {
                Debug.Log($"[DAMAGE] Applying {damageToApply} damage (accumulated: {accumulatedDamage:F2}). HP: {currentHP} -> {newHP}");
            }
            
            healthSystem.CurrentHP.Value = newHP;
            
            // Log damage periodically
            if (Time.time - lastDamageTime >= 1f)
            {
                if (showWarningMessages)
                {
                    Debug.LogWarning($"Darkness damage! HP: {newHP}/{healthSystem.MaxHP.Value}");
                }
                lastDamageTime = Time.time;
            }
            
            // Check for death
            if (newHP <= 0)
            {
                OnPlayerDeath();
            }
        }
    }
    
    private void OnPlayerDeath()
    {
        Debug.LogError("Player died from darkness exposure!");
        // You can add death handling here (respawn, game over screen, etc.)
        enabled = false; // Stop taking damage
    }
    
    /// <summary>
    /// Called by SafeZone when the player enters.
    /// </summary>
    public void EnterSafeZone()
    {
        isInSafeZone = true;
        timeInDarkness = 0f;
        isTakingDamage = false;
        accumulatedDamage = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log("Player entered SAFE ZONE - protected from darkness damage!");
        }
    }
    
    /// <summary>
    /// Called by SafeZone when the player exits.
    /// </summary>
    public void ExitSafeZone()
    {
        isInSafeZone = false;
        
        if (showDebugInfo)
        {
            Debug.Log("Player left safe zone - vulnerable to darkness damage!");
        }
    }
    
    // Visualize damage state in editor
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (isTakingDamage && !isInSafeZone)
        {
            Gizmos.color = damageGizmoColor;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
        else if (isInSafeZone)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
