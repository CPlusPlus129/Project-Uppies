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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pointLight = GetComponentInChildren<Light>();
        projCollider = GetComponent<Collider>();
    }

    public void Initialize(Vector3 velocity, float damage, float intensity, float sizeScale, Collider sourceCollider = null, float overrideLifetime = -1f)
    {
        rb.linearVelocity = velocity;
        currentDamage = damage;
        
        // Scale transform
        transform.localScale = Vector3.one * sizeScale;
        
        // Setup light
        if (pointLight != null)
        {
            pointLight.intensity = intensity;
            pointLight.range = sizeScale * 8f; // Increased range multiplier for better coverage
            initialIntensity = intensity;
            initialRange = pointLight.range;
        }

        // Ignore collision with source (player)
        if (sourceCollider != null && projCollider != null)
        {
            Physics.IgnoreCollision(projCollider, sourceCollider);
        }
        
        // Use override lifetime if provided
        currentLifetime = overrideLifetime > 0 ? overrideLifetime : lifetime;
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

        // Stick to surface
        hasLanded = true;
        
        // Stop physics movement FIRST
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, impactRadius, collisionLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            if (hitCollider.CompareTag("Player")) continue;

            // Calculate damage based on whether it's initial impact or tick damage
            float damageToDeal = isImpact ? currentDamage : (damagePerSecond * damageInterval);

            // Apply damage using SendMessage for compatibility
            hitCollider.SendMessageUpwards("TakeDamage", (int)damageToDeal, SendMessageOptions.DontRequireReceiver);
            hitCollider.SendMessageUpwards("ChangeHealth", -damageToDeal, SendMessageOptions.DontRequireReceiver);
            
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
