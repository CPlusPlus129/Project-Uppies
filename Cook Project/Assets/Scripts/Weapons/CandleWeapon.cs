using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CandleWeapon : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private LightProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float minForce = 10f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField] private float maxChargeTime = 2f;

    [Header("Light Settings")]
    [SerializeField] private float baseLightCost = 5f;
    [SerializeField] private float maxChargeLightCostMult = 3f;
    [SerializeField] private float baseIntensity = 2f;
    [SerializeField] private float maxChargeIntensityMult = 3f;
    [SerializeField] private float maxChargeLifetimeMult = 2f;
    private float baseLifetime = 5f; 

    [Header("Charge Drain Settings")]
    [SerializeField] private bool drainLightWhileCharging = false;
    [SerializeField] private float startDrainRate = 1f;
    [SerializeField] private float endDrainRate = 5f;

    [Header("Visuals")]
    [SerializeField] private float chargingLightStartMult = 0.5f;
    [SerializeField] private float chargingLightEndMult = 2.0f;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private GameObject targetingReticle;
    [SerializeField] private GameObject chargingOrbVisual;
    [SerializeField] private Vector3 chargingVisualOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private Vector3 chargeRotationSpeed = new Vector3(0, 360f, 0);
    [SerializeField] private float minChargeScale = 1f;
    [SerializeField] private float maxChargeScale = 1.5f;
    [SerializeField] private float projectileSpawnScale = 0.5f;
    [SerializeField] private int trajectoryResolution = 30;
    [SerializeField] private float trajectoryTimeStep = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private float currentChargeTime;
    private bool isCharging;
    private InputAction fireAction;
    
    private Vector3 reticleOriginalScale;
    private Vector3 visualInitialScale;
    private Collider playerCollider;
    private GameObject spawnedProjectileVisual;
    private LightRecoverySystem lightRecoverySystem;
    private Light chargingOrbLight;
    private float visualInitialLightIntensity;
    private Renderer chargingOrbRenderer;
    private Color baseEmissionColor;
    private bool hasEmission;

    private void Awake()
    {
        if (groundLayer.value == 0) 
        {
            groundLayer = 1; // Default layer
        }

        // Sync base intensity from prefab if available, so the fired projectile matches the prefab settings
        if (projectilePrefab != null)
        {
            Light prefabLight = projectilePrefab.GetComponentInChildren<Light>();
            if (prefabLight != null)
            {
                baseIntensity = prefabLight.intensity;
            }

            baseLifetime = projectilePrefab.GetBaseLifetime();
        }
        else
        {
            Debug.LogError("CandleWeapon: No LightProjectile prefab assigned! Please assign one in the inspector.");
            enabled = false;
            return;
        }

        if (chargingOrbVisual != null) 
        {
            // If the user assigned a Prefab Asset (scene.name is null), instantiate it.
            if (chargingOrbVisual.scene.name == null)
            {
                GameObject instance = Instantiate(chargingOrbVisual, firePoint);
                instance.transform.localPosition = chargingVisualOffset;
                instance.transform.localRotation = Quaternion.identity;
                
                // Strip physics and logic from the visual only
                Destroy(instance.GetComponent<LightProjectile>());
                Destroy(instance.GetComponent<Rigidbody>());
                Destroy(instance.GetComponent<ExplosionLifetimeBarController>());
                foreach (var c in instance.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var c in instance.GetComponentsInChildren<Canvas>()) Destroy(c.gameObject);
                
                chargingOrbVisual = instance;
                // Capture scale after instantiation
                visualInitialScale = chargingOrbVisual.transform.localScale;
            }
            else
            {
                // It's a scene object, capture its scale
                visualInitialScale = chargingOrbVisual.transform.localScale;
            }
            chargingOrbVisual.SetActive(false);
        }
        
        if (projectilePrefab != null)
        {
            // Create a visual representation if none exists
            if (chargingOrbVisual == null)
            {
                spawnedProjectileVisual = Instantiate(projectilePrefab.gameObject, firePoint);
                spawnedProjectileVisual.transform.localPosition = chargingVisualOffset;
                spawnedProjectileVisual.transform.localRotation = Quaternion.identity;
                
                // Strip physics and logic
                Destroy(spawnedProjectileVisual.GetComponent<LightProjectile>());
                Destroy(spawnedProjectileVisual.GetComponent<Rigidbody>());
                Destroy(spawnedProjectileVisual.GetComponent<ExplosionLifetimeBarController>());
                foreach (var c in spawnedProjectileVisual.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var c in spawnedProjectileVisual.GetComponentsInChildren<Canvas>()) Destroy(c.gameObject);
                
                chargingOrbVisual = spawnedProjectileVisual;
                chargingOrbVisual.SetActive(false);
            }
        }

        if (chargingOrbVisual != null)
        {
            // We handled capture in the blocks above to ensure we get it regardless of creation method
            // But just in case we missed a path (e.g. assigned scene object but didn't enter the prefab instantiation block)
            // Actually I added the else block for scene object above.
            // So this block is redundant or potentially dangerous if it runs after SetActive(false) on a newly created object?
            // SetActive(false) shouldn't affect localScale reading.
            // Let's remove this redundant block to avoid confusion and potential overwrites if logic changes.
        }
        else if (projectilePrefab != null)
        {
            // If we are going to create one later, we can't get scale yet.
            // But wait, if chargingOrbVisual is NULL, we enter the next block.
        }
        
        if (projectilePrefab != null)
        {
            // Create a visual representation if none exists
            if (chargingOrbVisual == null)
            {
                spawnedProjectileVisual = Instantiate(projectilePrefab.gameObject, firePoint);
                spawnedProjectileVisual.transform.localPosition = chargingVisualOffset;
                spawnedProjectileVisual.transform.localRotation = Quaternion.identity;
                
                // Strip physics and logic
                Destroy(spawnedProjectileVisual.GetComponent<LightProjectile>());
                Destroy(spawnedProjectileVisual.GetComponent<Rigidbody>());
                Destroy(spawnedProjectileVisual.GetComponent<ExplosionLifetimeBarController>());
                foreach (var c in spawnedProjectileVisual.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var c in spawnedProjectileVisual.GetComponentsInChildren<Canvas>()) Destroy(c.gameObject);
                
                chargingOrbVisual = spawnedProjectileVisual;
                chargingOrbVisual.SetActive(false);
                
                // Capture scale NOW that it exists
                visualInitialScale = chargingOrbVisual.transform.localScale;
            }
        }

        if (chargingOrbVisual != null)
        {
            chargingOrbLight = chargingOrbVisual.GetComponentInChildren<Light>(true);
            if (chargingOrbLight != null)
            {
                visualInitialLightIntensity = chargingOrbLight.intensity;
            }

            chargingOrbRenderer = chargingOrbVisual.GetComponentInChildren<Renderer>();
            if (chargingOrbRenderer != null)
            {
                // Accessing .material creates an instance, ensuring we don't modify the asset
                Material mat = chargingOrbRenderer.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    baseEmissionColor = mat.GetColor("_EmissionColor");
                    hasEmission = true;
                }
            }
        }

        if (targetingReticle != null) 
        {
            targetingReticle.SetActive(false);
            reticleOriginalScale = targetingReticle.transform.localScale;
            if (reticleOriginalScale == Vector3.zero) reticleOriginalScale = Vector3.one; 
        }
        if (trajectoryLine != null) 
        {
            trajectoryLine.enabled = false;
            // Ensure reasonable width so it doesn't look like a giant plane if unassigned
            if (trajectoryLine.startWidth > 0.2f) trajectoryLine.startWidth = 0.05f;
            if (trajectoryLine.endWidth > 0.2f) trajectoryLine.endWidth = 0.05f;
        }

        // Find player collider
        playerCollider = GetComponentInParent<CharacterController>();
        if (playerCollider == null)
        {
            playerCollider = GetComponentInParent<Collider>();
        }

        if (playerCollider != null)
        {
            lightRecoverySystem = playerCollider.GetComponent<LightRecoverySystem>();
            if (lightRecoverySystem == null)
            {
                // Fallback: check parent directly if collider was on a child
                lightRecoverySystem = playerCollider.GetComponentInParent<LightRecoverySystem>();
            }
        }
        else
        {
            // Fallback search
            lightRecoverySystem = GetComponentInParent<LightRecoverySystem>();
        }
    }

    private void Start()
    {
        fireAction = InputSystem.actions.FindAction("Attack");
    }

    private void Update()
    {
        if (fireAction == null) return;

        if (fireAction.IsPressed())
        {
            if (!isCharging)
            {
                StartCharging();
            }
            
            UpdateCharging();
        }
        else if (isCharging)
        {
            Fire();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        currentChargeTime = 0f;
        
        if (chargingOrbVisual != null) chargingOrbVisual.SetActive(true);
        if (trajectoryLine != null) trajectoryLine.enabled = true;
    }

    private void UpdateCharging()
    {
        currentChargeTime += Time.deltaTime;
        currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
        
        float chargeRatio = currentChargeTime / maxChargeTime;

        if (drainLightWhileCharging)
        {
            float currentDrain = Mathf.Lerp(startDrainRate, endDrainRate, chargeRatio);
            float drainAmount = currentDrain * Time.deltaTime;

            if (LightRecoverySystem.HasEnoughLight(drainAmount))
            {
                if (lightRecoverySystem != null)
                {
                    lightRecoverySystem.PreventRecovery(Time.deltaTime * 2f);
                }
                LightRecoverySystem.DrainLight(drainAmount);
            }
            else
            {
                Fire();
                return;
            }
        }

        if (chargingOrbVisual != null)
        {
            float scaleMult = Mathf.Lerp(minChargeScale, maxChargeScale, chargeRatio);
            chargingOrbVisual.transform.localScale = visualInitialScale * scaleMult;
            chargingOrbVisual.transform.Rotate(chargeRotationSpeed * Time.deltaTime, Space.Self);

            if (chargingOrbLight != null)
            {
                float intensityMult = Mathf.Lerp(chargingLightStartMult, chargingLightEndMult, chargeRatio);
                chargingOrbLight.intensity = visualInitialLightIntensity * intensityMult;
            }

            if (hasEmission && chargingOrbRenderer != null)
            {
                float intensityMult = Mathf.Lerp(chargingLightStartMult, chargingLightEndMult, chargeRatio);
                chargingOrbRenderer.material.SetColor("_EmissionColor", baseEmissionColor * intensityMult);
            }
        }

        UpdateTrajectory(chargeRatio);
    }

    private void UpdateTrajectory(float chargeRatio)
    {
        float currentForce = Mathf.Lerp(minForce, maxForce, chargeRatio);
        Vector3 velocity = GetLaunchVelocity(currentForce);
        
        List<Vector3> points = new List<Vector3>();
        Vector3 currentPos = firePoint.position;
        Vector3 currentVel = velocity;
        
        points.Add(currentPos);

        Vector3 lastPos = currentPos;
        bool hitGround = false;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            currentVel += Physics.gravity * trajectoryTimeStep;
            currentPos += currentVel * trajectoryTimeStep;
            
            // Raycast - Ignore triggers
            if (Physics.Linecast(lastPos, currentPos, out RaycastHit hit, groundLayer, QueryTriggerInteraction.Ignore))
            {
                points.Add(hit.point);
                hitGround = true;
                
                if (targetingReticle != null)
                {
                    if (!targetingReticle.activeSelf) targetingReticle.SetActive(true);

                    targetingReticle.transform.position = hit.point + hit.normal * 0.05f;
                    targetingReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    
                    float scaleMult = Mathf.Lerp(1f, maxChargeIntensityMult, chargeRatio);
                    targetingReticle.transform.localScale = reticleOriginalScale * scaleMult;
                }
                break;
            }
            
            points.Add(currentPos);
            lastPos = currentPos;
        }

        if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = points.Count;
            trajectoryLine.SetPositions(points.ToArray());
        }
        
        if (!hitGround && targetingReticle != null && targetingReticle.activeSelf)
        {
            targetingReticle.SetActive(false);
        }
    }

    private void Fire()
    {
        isCharging = false;
        
        if (chargingOrbVisual != null) chargingOrbVisual.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (targetingReticle != null) targetingReticle.SetActive(false);

        float chargeRatio = currentChargeTime / maxChargeTime;
        float finalCost = baseLightCost + (baseLightCost * (maxChargeLightCostMult - 1f) * chargeRatio);

        if (PlayerStatSystem.Instance != null)
        {
             if (!LightRecoverySystem.HasEnoughLight(finalCost))
             {
                 return;
             }
             LightRecoverySystem.DrainLight(finalCost);
        }

        float finalForce = Mathf.Lerp(minForce, maxForce, chargeRatio);
        float finalDamage = 10f * (1f + chargeRatio);
        float finalIntensity = Mathf.Lerp(baseIntensity, baseIntensity * maxChargeIntensityMult, chargeRatio);
        float finalLifetime = Mathf.Lerp(baseLifetime, baseLifetime * maxChargeLifetimeMult, chargeRatio);
        // Use fixed spawn scale for the projectile itself
        float sizeScale = projectileSpawnScale;

        Vector3 velocity = GetLaunchVelocity(finalForce);

        if (projectilePrefab != null)
        {
            LightProjectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // Ignore all weapon colliders to prevent immediate collision
            Collider[] weaponColliders = GetComponentsInChildren<Collider>();
            Collider[] projColliders = proj.GetComponentsInChildren<Collider>();
            
            if (projColliders != null && weaponColliders != null)
            {
                foreach (var pc in projColliders)
                {
                    foreach (var wc in weaponColliders)
                    {
                        Physics.IgnoreCollision(pc, wc);
                    }
                }
            }

            proj.gameObject.SetActive(true); // Ensure it's active
            proj.Initialize(velocity, finalDamage, finalIntensity, sizeScale, playerCollider, finalLifetime);
        }
    }

    private Vector3 GetLaunchVelocity(float force)
    {
        return firePoint.forward * force;
    }
}
