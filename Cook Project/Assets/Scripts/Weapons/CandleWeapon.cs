using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CandleWeapon : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private LightProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float minForce = 10f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float inheritedVelocityScale = 0.5f;

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
    [SerializeField] private GameObject targetingReticle;
    [SerializeField] private GameObject chargingOrbVisual;
    [SerializeField] private Vector3 chargingVisualOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private Vector3 chargeRotationSpeed = new Vector3(0, 360f, 0);
    [SerializeField] private float minChargeScale = 1f;
    [SerializeField] private float maxChargeScale = 1.5f;
    [SerializeField] private float projectileSpawnScale = 0.5f;

    [Header("Trajectory")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private Gradient trajectoryColor;
    [SerializeField] private float trajectoryWidth = 0.05f;
    [SerializeField] private int trajectoryResolution = 30;
    [SerializeField] private float trajectoryTimeStep = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private float currentChargeTime;
    private bool isCharging;
    private InputAction fireAction;
    private InputAction reloadAction;
    
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
    private Rigidbody weaponRigidbody;
    private Collider[] cachedWeaponColliders;
    private MaterialPropertyBlock propBlock;
    private int emissionColorId;

    // Optimization: Reusable list
    private List<Vector3> trajectoryPoints = new List<Vector3>();

    private void Awake()
    {
        if (groundLayer.value == 0) 
        {
            groundLayer = (1 << 0) | (1 << 6) | (1 << 12); 
        }

        // Sync base intensity from prefab
        if (projectilePrefab != null)
        {
            Light prefabLight = projectilePrefab.GetComponentInChildren<Light>();
            if (prefabLight != null) baseIntensity = prefabLight.intensity;
            baseLifetime = projectilePrefab.GetBaseLifetime();
        }
        else
        {
            Debug.LogError("CandleWeapon: No LightProjectile prefab assigned!");
            enabled = false;
            return;
        }

        SetupChargingOrb();
        SetupTrajectoryLine();

        if (targetingReticle != null) 
        {
            targetingReticle.SetActive(false);
            reticleOriginalScale = targetingReticle.transform.localScale;
            if (reticleOriginalScale == Vector3.zero) reticleOriginalScale = Vector3.one; 
        }

        // Find player collider and systems
        playerCollider = GetComponentInParent<CharacterController>();
        if (playerCollider == null) playerCollider = GetComponentInParent<Collider>();

        if (playerCollider != null)
            lightRecoverySystem = playerCollider.GetComponent<LightRecoverySystem>() ?? playerCollider.GetComponentInParent<LightRecoverySystem>();
        else
            lightRecoverySystem = GetComponentInParent<LightRecoverySystem>();

        cachedWeaponColliders = GetComponentsInChildren<Collider>();
        propBlock = new MaterialPropertyBlock();
        emissionColorId = Shader.PropertyToID("_EmissionColor");
        weaponRigidbody = GetComponentInParent<Rigidbody>();
    }

    private void SetupTrajectoryLine()
    {
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.startWidth = trajectoryWidth;
            trajectoryLine.endWidth = trajectoryWidth;
            trajectoryLine.useWorldSpace = true;

            // Ensure material exists
            if (trajectoryLine.sharedMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null)
                    trajectoryLine.material = new Material(spriteShader);
            }

            // Apply default gradient if null
            if (trajectoryColor == null)
            {
                trajectoryColor = new Gradient();
                trajectoryColor.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
            }
            
            trajectoryLine.colorGradient = trajectoryColor;
        }
    }

    private void SetupChargingOrb()
    {
        // 1. If assigned scene object
        if (chargingOrbVisual != null && chargingOrbVisual.scene.IsValid())
        {
             visualInitialScale = chargingOrbVisual.transform.localScale;
        }
        // 2. If assigned prefab or needs creation
        else 
        {
            GameObject prefabToUse = chargingOrbVisual != null ? chargingOrbVisual : (projectilePrefab != null ? projectilePrefab.gameObject : null);
            
            if (prefabToUse != null)
            {
                chargingOrbVisual = Instantiate(prefabToUse, firePoint);
                chargingOrbVisual.transform.localPosition = chargingVisualOffset;
                chargingOrbVisual.transform.localRotation = Quaternion.identity;

                // Strip components
                Destroy(chargingOrbVisual.GetComponent<LightProjectile>());
                Destroy(chargingOrbVisual.GetComponent<Rigidbody>());
                Destroy(chargingOrbVisual.GetComponent<ExplosionLifetimeBarController>());
                foreach (var c in chargingOrbVisual.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var c in chargingOrbVisual.GetComponentsInChildren<Canvas>()) Destroy(c.gameObject);
                
                visualInitialScale = chargingOrbVisual.transform.localScale;
            }
        }

        if (chargingOrbVisual != null)
        {
            chargingOrbVisual.SetActive(false);
            chargingOrbLight = chargingOrbVisual.GetComponentInChildren<Light>(true);
            if (chargingOrbLight != null) visualInitialLightIntensity = chargingOrbLight.intensity;

            chargingOrbRenderer = chargingOrbVisual.GetComponentInChildren<Renderer>();
            if (chargingOrbRenderer != null)
            {
                Material mat = chargingOrbRenderer.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    baseEmissionColor = mat.GetColor("_EmissionColor");
                    hasEmission = true;
                }
            }
        }
    }

    private void Start()
    {
        fireAction = InputSystem.actions.FindAction("Attack");
        reloadAction = InputSystem.actions.FindAction("Reload");
    }

    private void Update()
    {
        if (reloadAction != null && reloadAction.WasPressedThisFrame())
        {
            ResetRotation();
        }

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
                chargingOrbRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(emissionColorId, baseEmissionColor * intensityMult);
                chargingOrbRenderer.SetPropertyBlock(propBlock);
            }
        }

        UpdateTrajectory(chargeRatio);
    }

    private void UpdateTrajectory(float chargeRatio)
    {
        float currentForce = Mathf.Lerp(minForce, maxForce, chargeRatio);
        Vector3 velocity = GetLaunchVelocity(currentForce);
        
        if (weaponRigidbody != null)
        {
            velocity += weaponRigidbody.GetPointVelocity(firePoint.position) * inheritedVelocityScale;
        }

        trajectoryPoints.Clear();
        // Start from the orb if available
        Vector3 currentPos = chargingOrbVisual != null ? chargingOrbVisual.transform.position : firePoint.position;
        Vector3 currentVel = velocity;
        
        trajectoryPoints.Add(currentPos);

        Vector3 lastPos = currentPos;
        bool hitGround = false;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            currentVel += Physics.gravity * trajectoryTimeStep;
            currentPos += currentVel * trajectoryTimeStep;
            
            // Raycast - Ignore triggers
            if (Physics.Linecast(lastPos, currentPos, out RaycastHit hit, groundLayer, QueryTriggerInteraction.Ignore))
            {
                trajectoryPoints.Add(hit.point);
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
            
            trajectoryPoints.Add(currentPos);
            lastPos = currentPos;
        }

        if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = trajectoryPoints.Count;
            trajectoryLine.SetPositions(trajectoryPoints.ToArray());
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
                 // Feedback for not enough light
                 if (targetingReticle != null && !targetingReticle.activeSelf)
                 {
                      // Flash reticle logic could go here, for now just ensure it's off
                 }
                 Debug.Log("Not enough light to fire!");
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

        Vector3 spawnPos = chargingOrbVisual != null ? chargingOrbVisual.transform.position : firePoint.position;
        Vector3 velocity = GetLaunchVelocity(finalForce);

        if (weaponRigidbody != null)
        {
            velocity += weaponRigidbody.GetPointVelocity(spawnPos) * inheritedVelocityScale;
        }

        if (projectilePrefab != null)
        {
            LightProjectile proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            
            // Ignore all weapon colliders to prevent immediate collision
            Collider[] projColliders = proj.GetComponentsInChildren<Collider>();
            
            if (projColliders != null && cachedWeaponColliders != null)
            {
                foreach (var pc in projColliders)
                {
                    foreach (var wc in cachedWeaponColliders)
                    {
                        if (wc != null) // Check null in case something was destroyed
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

    private void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
}
