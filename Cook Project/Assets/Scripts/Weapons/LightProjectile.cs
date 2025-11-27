using UnityEngine;

/// <summary>
/// A persistent light projectile that sticks to surfaces, deals damage to enemies,
/// and has a finite lifetime (health) that fades over time.
/// </summary>
public class LightProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactRadius = 3f;

    public float GetBaseLifetime() => lifetime;

    public float GetRemainingLifetime()
    {
        return currentLifetime;
    }

    public bool HasLanded() => hasLanded;
    
    /// <summary>
    /// Reduces the remaining lifetime of the projectile by the specified amount.
    /// Used by mobs to attack light sources.
    /// </summary>
    /// <param name="reductionAmount">Amount to reduce lifetime by</param>
    public void ReduceLifetime(float reductionAmount)
    {
        currentLifetime = Mathf.Max(0.1f, currentLifetime - reductionAmount);
        
        // Update the base lifetime so fade curve works correctly
        lifetime = currentLifetime;
        
        Debug.Log($"[LightProjectile] Lifetime reduced by {reductionAmount}. Remaining: {currentLifetime}");
    }
    
    [Header("Light Settings")]
    [SerializeField] private float lightDecayRate = 1f; // Intensity loss per second
    [SerializeField] private AnimationCurve lightFadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageInterval = 0.5f;
    
    private Rigidbody rb;
    private Light pointLight;
    private Collider projCollider;
    
    private float initialIntensity;
    private float initialRange;
    private float currentLifetime;
    private float nextDamageTime;
    private float currentDamage; // Scaled by charge
    
    private bool hasLanded = false;
    private Collider[] hitCollidersBuffer = new Collider[32]; // Pre-allocated buffer

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pointLight = GetComponentInChildren<Light>();
        projCollider = GetComponent<Collider>();
    }

    public void Initialize(Vector3 velocity, float damage, float intensity, float sizeScale, Collider sourceCollider = null, float overrideLifetime = -1f)
    {
        // Ensure components are assigned
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (pointLight == null) pointLight = GetComponentInChildren<Light>();
        if (projCollider == null) projCollider = GetComponentInChildren<Collider>(); // Changed to GetComponentInChildren

        Debug.Log($"[LightProjectile] Initialized. Velocity: {velocity}, Damage: {damage}, Lifetime: {overrideLifetime}");

        if (rb != null)
        {
            rb.isKinematic = false; // Ensure physics is enabled
            rb.useGravity = true;   // Ensure gravity is normal (unless we want 0g)
            rb.linearVelocity = velocity;
        }
        else
        {
            Debug.LogError($"[LightProjectile] Missing Rigidbody on {gameObject.name}", gameObject);
        }

        currentDamage = damage;
        
        // Scale transform
        transform.localScale = Vector3.one * sizeScale;
        
        // Setup light
        if (pointLight != null)
        {
            pointLight.intensity = intensity;
            pointLight.range = sizeScale * 8f; 
            initialIntensity = intensity;
            initialRange = pointLight.range;
        }

        // Ignore collision with source (player)
        if (sourceCollider != null)
        {
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            foreach (var c in myColliders)
            {
                Physics.IgnoreCollision(c, sourceCollider);
            }
        }
        
        // Use override lifetime if provided
        if (overrideLifetime > 0)
        {
            currentLifetime = overrideLifetime;
            lifetime = overrideLifetime; // Update base lifetime so fade curve works correctly
        }
        else
        {
            currentLifetime = lifetime;
        }
    }

    private void Update()
    {
        // Handle lifetime/health decay
        if (currentLifetime > 0)
        {
            currentLifetime -= Time.deltaTime;
            
            // Fade light
            if (pointLight != null)
            {
                float lifeRatio = 1f - (currentLifetime / lifetime);
                float fadeMult = lightFadeCurve.Evaluate(lifeRatio);
                
                pointLight.intensity = initialIntensity * fadeMult;
                pointLight.range = initialRange * fadeMult;
            }
            
            if (currentLifetime <= 0)
            {
                Debug.Log("[LightProjectile] Lifetime expired. Fizzling out.");
                FizzleOut();
            }
        }

        // If landed, deal damage to nearby enemies over time
        if (hasLanded && Time.time >= nextDamageTime)
        {
            DealAreaDamage();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;
        
        // Ignore player collision
        if (collision.gameObject.CompareTag("Player")) return;

        Debug.Log($"[LightProjectile] Collided with {collision.gameObject.name} at {collision.contacts[0].point}");

        // Stick to surface
        hasLanded = true;
        
        // Stop physics movement FIRST
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Lock it
        }
        
        // ... rest of collision logic ...
        // Then make kinematic to lock it in place
        // Check if we hit something static or the ground
        if (collision.gameObject.isStatic || collision.gameObject.layer == LayerMask.NameToLayer("Default") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Align to surface normal (Face Up)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, collision.contacts[0].normal);

            // Just stay in place
            if (collision.rigidbody != null)
             // Parent to dynamic objects so it sticks
             transform.SetParent(collision.transform);
        }

        // Initial Impact Effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
        }
        
        // Initial burst damage on impact
        DealAreaDamage(true);
    }

    private void DealAreaDamage(bool isImpact = false)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, impactRadius, hitCollidersBuffer, collisionLayers);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = hitCollidersBuffer[i];
            
            if (hitCollider.gameObject == gameObject) continue;
            if (hitCollider.CompareTag("Player")) continue;

            // Calculate damage based on whether it's initial impact or tick damage
            float damageToDeal = isImpact ? currentDamage : (damagePerSecond * damageInterval);

            // Try to find Mob component first (Fast Path)
            Mob mob = hitCollider.GetComponentInParent<Mob>();
            if (mob != null)
            {
                mob.TakeDamage((int)damageToDeal);
            }
            else
            {
                // Fallback for non-Mob damageables (e.g. breakables)
                hitCollider.SendMessageUpwards("TakeDamage", (int)damageToDeal, SendMessageOptions.DontRequireReceiver);
                hitCollider.SendMessageUpwards("ChangeHealth", -damageToDeal, SendMessageOptions.DontRequireReceiver);
            }
            
            if (isImpact && hitCollider.attachedRigidbody != null)
            {
                hitCollider.attachedRigidbody.AddExplosionForce(5f * initialIntensity, transform.position, impactRadius);
            }
        }
    }

    private void FizzleOut()
    {
        // Maybe spawn a poof effect here if desired
        Destroy(gameObject);
    }
}
