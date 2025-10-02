using UnityEngine;

/// <summary>
/// Defines a 3D area where the player is protected from light damage and can heal over time.
/// Uses a trigger collider to detect player entry/exit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [Header("Safe Zone Settings")]
    [Tooltip("Tag to identify the player (default: 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Healing Settings")]
    [SerializeField] private bool enableHealing = true;
    [Tooltip("HP restored per second while in safe zone")]
    [SerializeField] private float healingPerSecond = 5f;
    [Tooltip("Delay before healing starts after entering safe zone")]
    [SerializeField] private float healingDelay = 1f;
    [Tooltip("Can heal above max HP (usually false)")]
    [SerializeField] private bool canOverheal = false;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color gizmoWireColor = new Color(0f, 1f, 0f, 0.8f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    private Collider safeZoneCollider;
    private bool playerInZone = false;
    private PlayerLightDamage currentPlayerDamage;
    private float timeInZone = 0f;
    private float accumulatedHealing = 0f;
    private bool isHealing = false;
    
    private void Awake()
    {
        // Get the collider and ensure it's a trigger
        safeZoneCollider = GetComponent<Collider>();
        if (safeZoneCollider != null)
        {
            safeZoneCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("SafeZone: No collider found! Please add a collider component.", this);
        }
    }
    
    private void Update()
    {
        // Apply healing if player is in zone
        if (playerInZone && enableHealing)
        {
            timeInZone += Time.deltaTime;
            
            // Start healing after delay
            if (timeInZone >= healingDelay)
            {
                if (!isHealing)
                {
                    isHealing = true;
                    if (showDebugMessages)
                    {
                        Debug.Log($"Player started healing in Safe Zone: {gameObject.name}");
                    }
                }
                
                ApplyHealing();
            }
        }
    }
    
    private void ApplyHealing()
    {
        var healthSystem = PlayerStatSystem.Instance;
        if (healthSystem == null) return;
        
        int currentHP = healthSystem.CurrentHP.Value;
        int maxHP = healthSystem.MaxHP.Value;
        
        // Don't heal if already at max HP (unless overheal is enabled)
        if (!canOverheal && currentHP >= maxHP)
        {
            return;
        }
        
        // Accumulate fractional healing (same technique as damage)
        float healingThisFrame = healingPerSecond * Time.deltaTime;
        accumulatedHealing += healingThisFrame;
        
        // Only apply healing when we have at least 1 full HP worth
        if (accumulatedHealing >= 1f)
        {
            int healingToApply = Mathf.FloorToInt(accumulatedHealing);
            accumulatedHealing -= healingToApply; // Keep the remainder
            
            int newHP;
            if (canOverheal)
            {
                newHP = currentHP + healingToApply;
            }
            else
            {
                newHP = Mathf.Min(maxHP, currentHP + healingToApply);
            }
            
            healthSystem.CurrentHP.Value = newHP;
            
            if (showDebugMessages)
            {
                Debug.Log($"[HEAL] Applied {healingToApply} healing. HP: {currentHP} -> {newHP}");
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;
            timeInZone = 0f;
            isHealing = false;
            accumulatedHealing = 0f;
            
            // Notify the player's light damage script
            currentPlayerDamage = other.GetComponent<PlayerLightDamage>();
            if (currentPlayerDamage != null)
            {
                currentPlayerDamage.EnterSafeZone();
                
                if (showDebugMessages)
                {
                    Debug.Log($"Player entered Safe Zone: {gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("SafeZone: Player doesn't have PlayerLightDamage component!");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the player exited
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
            timeInZone = 0f;
            isHealing = false;
            accumulatedHealing = 0f;
            
            // Notify the player's light damage script
            if (currentPlayerDamage != null)
            {
                currentPlayerDamage.ExitSafeZone();
                currentPlayerDamage = null;
                
                if (showDebugMessages)
                {
                    Debug.Log($"Player exited Safe Zone: {gameObject.name}");
                }
            }
        }
    }
    
    // Draw the safe zone in the editor
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        // Set colors based on whether player is in zone (only in play mode)
        Color fillColor = gizmoColor;
        Color wireColor = gizmoWireColor;
        
        if (Application.isPlaying && playerInZone)
        {
            // Cyan when player is inside
            fillColor = new Color(0f, 1f, 1f, 0.5f);
            wireColor = Color.cyan;
            
            // Pulsing effect if healing
            if (isHealing)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 0.3f) + 0.5f;
                fillColor.a = pulse;
            }
        }
        
        // Draw based on collider type
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (col is BoxCollider boxCol)
        {
            Gizmos.color = fillColor;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
        }
        else if (col is CapsuleCollider capsuleCol)
        {
            // Approximate capsule with spheres
            Gizmos.color = fillColor;
            Vector3 center = capsuleCol.center;
            float radius = capsuleCol.radius;
            float height = capsuleCol.height;
            
            // Draw top and bottom spheres
            Gizmos.DrawSphere(center + Vector3.up * (height / 2 - radius), radius);
            Gizmos.DrawSphere(center - Vector3.up * (height / 2 - radius), radius);
            
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(center + Vector3.up * (height / 2 - radius), radius);
            Gizmos.DrawWireSphere(center - Vector3.up * (height / 2 - radius), radius);
        }
        else if (col is MeshCollider)
        {
            // For mesh colliders, just draw the bounds
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        
        Gizmos.matrix = oldMatrix;
        
        // Draw a label
        #if UNITY_EDITOR
        string statusText = "Safe Zone";
        if (Application.isPlaying && isHealing)
        {
            statusText += " (HEALING)";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{statusText}\n{gameObject.name}");
        #endif
    }
    
    // Optional: Draw in scene view when selected
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Draw a brighter version when selected
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (col is BoxCollider boxCol)
        {
            Gizmos.DrawCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
        }
        
        Gizmos.matrix = oldMatrix;
    }
}
