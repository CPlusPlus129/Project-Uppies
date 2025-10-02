using UnityEngine;

/// <summary>
/// Helper script to ensure projectiles have proper physics settings for reliable collision detection.
/// Attach this to projectile prefabs to automatically configure rigidbody settings on spawn.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectilePhysicsFixer : MonoBehaviour
{
    [Header("Collision Detection")]
    [Tooltip("Use Continuous Dynamic for fast-moving projectiles to prevent tunneling")]
    [SerializeField] private CollisionDetectionMode collisionMode = CollisionDetectionMode.ContinuousDynamic;
    
    [Header("Physics Settings")]
    [Tooltip("Interpolation helps smooth out physics movement")]
    [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
    
    [Tooltip("Increase this if the projectile is still passing through thin objects")]
    [SerializeField] private float minColliderSize = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigurePhysics();
    }
    
    private void ConfigurePhysics()
    {
        if (rb == null) return;
        
        // Set collision detection mode to prevent tunneling
        rb.collisionDetectionMode = collisionMode;
        
        // Set interpolation for smoother movement
        rb.interpolation = interpolation;
        
        // Ensure collider is large enough
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is SphereCollider sphereCol)
            {
                if (sphereCol.radius < minColliderSize)
                {
                    sphereCol.radius = minColliderSize;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Increased sphere collider radius to {minColliderSize} for better hit detection");
                    }
                }
            }
            else if (col is BoxCollider boxCol)
            {
                Vector3 size = boxCol.size;
                bool changed = false;
                
                if (size.x < minColliderSize)
                {
                    size.x = minColliderSize;
                    changed = true;
                }
                if (size.y < minColliderSize)
                {
                    size.y = minColliderSize;
                    changed = true;
                }
                if (size.z < minColliderSize)
                {
                    size.z = minColliderSize;
                    changed = true;
                }
                
                if (changed)
                {
                    boxCol.size = size;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Adjusted box collider size to minimum {minColliderSize} for better hit detection");
                    }
                }
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                if (capsuleCol.radius < minColliderSize)
                {
                    capsuleCol.radius = minColliderSize;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Increased capsule collider radius to {minColliderSize} for better hit detection");
                    }
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Projectile physics configured: Collision Mode = {collisionMode}, Interpolation = {interpolation}");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Projectile hit: {collision.gameObject.name} at {collision.contacts[0].point}");
        }
    }
}
