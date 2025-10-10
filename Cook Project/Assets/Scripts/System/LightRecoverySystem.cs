using UnityEngine;
using R3;

/// <summary>
/// Handles passive light recovery for the player.
/// Attach this to the Player GameObject to enable automatic light regeneration.
/// </summary>
public class LightRecoverySystem : MonoBehaviour
{
    [Header("Recovery Settings")]
    [SerializeField] private bool enablePassiveRecovery = true;
    [SerializeField] private float recoveryDelay = 0f; // Delay before recovery starts (in seconds)

    [SerializeField] [Range(0f, 100f)] private float rechargeSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private PlayerStatSystem playerStatSystem;
    private float timeSinceLastDrain = 0f;
    private bool canRecover = true;
    
    private void Start()
    {
        playerStatSystem = PlayerStatSystem.Instance;
        
        if (playerStatSystem == null)
        {
            Debug.LogError("LightRecoverySystem: PlayerStatSystem not found!");
            enabled = false;
            return;
        }
        
        // Subscribe to light changes to track when light is drained
        playerStatSystem.CurrentLight.Subscribe(light =>
        {
            // Reset recovery timer when light is drained
            if (light < playerStatSystem.MaxLight.Value)
            {
                timeSinceLastDrain = 0f;
                canRecover = false;
            }
        }).AddTo(this);
    }
    
    private void Update()
    {
        if (!enablePassiveRecovery || playerStatSystem == null) return;
        
        float currentLight = playerStatSystem.CurrentLight.Value;
        float maxLight = playerStatSystem.MaxLight.Value;
        float recoverySpeed = rechargeSpeed;
        
        // Check if we're at max light already
        if (currentLight >= maxLight)
        {
            canRecover = true;
            return;
        }
        
        // Handle recovery delay
        if (!canRecover)
        {
            timeSinceLastDrain += Time.deltaTime;
            if (timeSinceLastDrain >= recoveryDelay)
            {
                canRecover = true;
            }
        }
        
        // Recover light if allowed
        if (canRecover)
        {
            float newLight = currentLight + (recoverySpeed * Time.deltaTime);
            newLight = Mathf.Min(newLight, maxLight);
            playerStatSystem.CurrentLight.Value = newLight;
            
            if (showDebugInfo)
            {
                Debug.Log($"Light Recovery: {currentLight:F2} -> {newLight:F2} (Rate: {recoverySpeed}/s)");
            }
        }
    }
    
    /// <summary>
    /// Manually drain light (use this method from other systems like Weapon.cs)
    /// </summary>
    public static void DrainLight(float amount)
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        if (playerStatSystem != null)
        {
            float newLight = Mathf.Max(0f, playerStatSystem.CurrentLight.Value - amount);
            playerStatSystem.CurrentLight.Value = newLight;
        }
    }
    
    /// <summary>
    /// Check if player has enough light for an action
    /// </summary>
    public static bool HasEnoughLight(float requiredAmount)
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        if (playerStatSystem != null)
        {
            return playerStatSystem.CurrentLight.Value >= requiredAmount;
        }
        return false;
    }
    
    /// <summary>
    /// Set whether passive recovery is enabled
    /// </summary>
    public void SetPassiveRecovery(bool enabled)
    {
        enablePassiveRecovery = enabled;
    }
    
    /// <summary>
    /// Set the recovery delay
    /// </summary>
    public void SetRecoveryDelay(float delay)
    {
        recoveryDelay = Mathf.Max(0f, delay);
    }
}
