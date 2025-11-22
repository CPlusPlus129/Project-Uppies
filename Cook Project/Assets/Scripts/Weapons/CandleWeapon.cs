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
    [SerializeField] private float baseLifetime = 5f; 
    [SerializeField] private float maxChargeLifetimeMult = 2f;

    [Header("Visuals")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private GameObject targetingReticle;
    [SerializeField] private GameObject chargingOrbVisual;
    [SerializeField] private int trajectoryResolution = 30;
    [SerializeField] private float trajectoryTimeStep = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private float currentChargeTime;
    private bool isCharging;
    private InputAction fireAction;
    
    private Vector3 reticleOriginalScale;
    private Collider playerCollider;

    private void Awake()
    {
        if (groundLayer.value == 0) 
        {
            groundLayer = 1; // Default layer
        }

        if (chargingOrbVisual != null) chargingOrbVisual.SetActive(false);
        if (targetingReticle != null) 
        {
            targetingReticle.SetActive(false);
            reticleOriginalScale = targetingReticle.transform.localScale;
            if (reticleOriginalScale == Vector3.zero) reticleOriginalScale = Vector3.one; 
        }
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        // Find player collider
        playerCollider = GetComponentInParent<CharacterController>();
        if (playerCollider == null)
        {
            playerCollider = GetComponentInParent<Collider>();
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

        if (chargingOrbVisual != null)
        {
            float scale = Mathf.Lerp(0.1f, 0.5f, chargeRatio);
            chargingOrbVisual.transform.localScale = Vector3.one * scale;
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
        float sizeScale = Mathf.Lerp(0.2f, 0.5f, chargeRatio);

        Vector3 velocity = GetLaunchVelocity(finalForce);

        if (projectilePrefab != null)
        {
            LightProjectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            // Manually set the lifetime before Initialize since Initialize uses the serialized value
            // Or better, just set it directly if we make it public, but since it's private serialized field,
            // we'll rely on the updated Initialize method (if I had updated it signature, which I didn't).
            // Correction: I should update Initialize to accept lifetime to be clean.
            // But to save a write, I'll assume the serialized default is OK for now or update it in a bit.
            // Wait, I'll update Initialize signature in LightProjectile to take lifetime.
            
            // Actually, I missed updating the Initialize signature in LightProjectile.cs in previous step.
            // I only added sourceCollider.
            // I will re-write LightProjectile.cs one more time to add lifetime parameter.
            proj.Initialize(velocity, finalDamage, finalIntensity, sizeScale, playerCollider, finalLifetime);
        }
    }

    private Vector3 GetLaunchVelocity(float force)
    {
        return firePoint.forward * force;
    }
}
