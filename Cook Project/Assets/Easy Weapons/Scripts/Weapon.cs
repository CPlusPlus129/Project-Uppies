/// <summary>
/// Weapon.cs - Modernized for Unity 6.2
/// Comprehensive weapon system supporting raycast, projectile, and beam weapons
/// Features include warmup mechanics, recoil, ammo management, and visual effects
/// </summary>

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public enum WeaponType
{
    Raycast,
    Projectile,
    Beam
}

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst
}

public enum BulletHoleDetection
{
    Tag,
    Material,
    PhysicMaterial
}

[System.Serializable]
public class BulletHoleMapping
{
    public string tag = "Untagged";
    public Material material;
    public PhysicsMaterial physicMaterial;
    public BulletHolePool bulletHolePool;
}

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour
{
    #region Weapon Configuration
    [Header("Weapon Type & Mode")]
    [Tooltip("Type of weapon system to use")]
    public WeaponType weaponType = WeaponType.Raycast;
    
    [Tooltip("Fire mode - Semi, Full Auto, or Burst")]
    public FireMode fireMode = FireMode.FullAuto;
    
    [Tooltip("Is this a player weapon (vs AI weapon)")]
    public bool isPlayerWeapon = true;
    #endregion

    #region References
    [Header("References")]
    [Tooltip("Visual model of the weapon")]
    public GameObject weaponModel;
    
    [Tooltip("Start position for raycasts and beams")]
    public Transform raycastOrigin;
    
    [Tooltip("Spawn point for projectiles")]
    public Transform projectileSpawnPoint;
    
    [Tooltip("Position for muzzle effects")]
    public Transform muzzleEffectPoint;
    
    [Tooltip("Position where shells are ejected")]
    public Transform shellEjectPoint;
    #endregion

    #region Fire Rate & Timing
    [Header("Fire Rate")]
    [Tooltip("Rounds per second")]
    [Range(0.1f, 30f)]
    public float roundsPerSecond = 10f;
    
    [Tooltip("Delay before firing (useful for AI)")]
    [Range(0f, 2f)]
    public float fireDelay = 0f;
    
    [Header("Burst Fire (Burst Mode Only)")]
    [Tooltip("Number of shots per burst")]
    [Range(2, 10)]
    public int burstCount = 3;
    
    [Tooltip("Pause between bursts")]
    [Range(0f, 2f)]
    public float burstPause = 0.2f;
    #endregion

    #region Ammo System
    [Header("Ammo")]
    [Tooltip("Unlimited ammunition")]
    public bool infiniteAmmo = false;
    
    [Tooltip("Magazine capacity")]
    [Range(1, 999)]
    public int magazineCapacity = 30;
    
    [Tooltip("Projectiles per shot (for shotguns)")]
    [Range(1, 20)]
    public int pelletsPerShot = 1;
    
    [Tooltip("Reload time in seconds")]
    [Range(0.1f, 10f)]
    public float reloadTime = 2f;
    
    [Tooltip("Auto-reload when empty")]
    public bool autoReload = true;
    
    private int currentAmmo;
    #endregion

    #region Damage & Power
    [Header("Damage (Raycast/Beam)")]
    [Tooltip("Damage per hit")]
    [Range(1f, 1000f)]
    public float damage = 50f;
    
    [Tooltip("Force applied to rigidbodies")]
    [Range(1f, 100f)]
    public float impactForce = 10f;
    
    [Tooltip("Beam damage per second")]
    [Range(0.1f, 100f)]
    public float beamDamagePerSecond = 20f;
    
    [Tooltip("Weapon range")]
    [Range(1f, 1000f)]
    public float range = 100f;
    #endregion

    #region Projectile Settings
    [Header("Projectile (Projectile Type Only)")]
    [Tooltip("Projectile prefab to spawn")]
    public GameObject projectilePrefab;
    #endregion

    #region Beam Settings
    [Header("Beam (Beam Type Only)")]
    [Tooltip("Maximum beam duration before overheat")]
    [Range(0.1f, 10f)]
    public float maxBeamDuration = 3f;
    
    [Tooltip("Unlimited beam (no overheat)")]
    public bool infiniteBeam = false;
    
    [Tooltip("Beam reflects off surfaces")]
    public bool beamReflects = true;
    
    [Tooltip("Material that reflects beam (null = all surfaces)")]
    public Material reflectiveMaterial;
    
    [Tooltip("Maximum reflections")]
    [Range(1, 10)]
    public int maxReflections = 5;
    
    [Header("Beam Visuals")]
    [Tooltip("Beam material (line renderer)")]
    public Material beamMaterial;
    
    [Tooltip("Beam color tint")]
    public Color beamColor = Color.red;
    
    [Tooltip("Beam start width")]
    [Range(0.01f, 2f)]
    public float beamStartWidth = 0.1f;
    
    [Tooltip("Beam end width")]
    [Range(0.01f, 2f)]
    public float beamEndWidth = 0.2f;
    
    private float beamHeat = 0f;
    private bool beamCoolingDown = false;
    private GameObject beamObject;
    private bool isBeaming = false;
    #endregion

    #region Accuracy System
    [Header("Accuracy")]
    [Tooltip("Base accuracy (0-100)")]
    [Range(0f, 100f)]
    public float baseAccuracy = 90f;
    
    [Tooltip("Accuracy loss per shot")]
    [Range(0f, 10f)]
    public float accuracyDecayPerShot = 1f;
    
    [Tooltip("Accuracy recovery speed")]
    [Range(0.1f, 100f)]
    public float accuracyRecoveryRate = 5f;
    
    private float currentAccuracy;
    #endregion

    #region Warmup System
    [Header("Warmup (Optional)")]
    [Tooltip("Enable charge-up mechanic")]
    public bool enableWarmup = false;
    
    [Tooltip("Maximum warmup duration")]
    [Range(0.1f, 10f)]
    public float maxWarmupTime = 2f;
    
    [Tooltip("Multiply damage by warmup")]
    public bool warmupAffectsDamage = false;
    
    [Tooltip("Multiply projectile force by warmup")]
    public bool warmupAffectsForce = true;
    
    [Tooltip("Damage multiplier at max warmup")]
    [Range(1f, 10f)]
    public float warmupDamageMultiplier = 2f;
    
    [Tooltip("Force multiplier at max warmup")]
    [Range(1f, 10f)]
    public float warmupForceMultiplier = 3f;
    
    [Tooltip("Allow canceling warmup")]
    public bool allowWarmupCancel = false;
    
    private float warmupCharge = 0f;
    #endregion

    #region Recoil System
    [Header("Recoil")]
    [Tooltip("Enable weapon recoil")]
    public bool enableRecoil = true;
    
    [Tooltip("Minimum backward kick")]
    [Range(0f, 1f)]
    public float recoilKickMin = 0.05f;
    
    [Tooltip("Maximum backward kick")]
    [Range(0f, 1f)]
    public float recoilKickMax = 0.15f;
    
    [Tooltip("Minimum rotation kick")]
    [Range(0f, 5f)]
    public float recoilRotationMin = 0.5f;
    
    [Tooltip("Maximum rotation kick")]
    [Range(0f, 5f)]
    public float recoilRotationMax = 1.5f;
    
    [Tooltip("Recoil recovery speed")]
    [Range(1f, 100f)]
    public float recoilRecoverySpeed = 10f;
    #endregion

    #region Visual Effects
    [Header("Visual Effects")]
    [Tooltip("Enable muzzle flash")]
    public bool enableMuzzleFlash = true;
    
    [Tooltip("Muzzle flash prefabs (random)")]
    public GameObject[] muzzleFlashPrefabs;
    
    [Tooltip("Enable impact effects")]
    public bool enableImpactEffects = true;
    
    [Tooltip("Impact effect prefabs (random)")]
    public GameObject[] impactEffectPrefabs;
    
    [Tooltip("Enable shell ejection")]
    public bool ejectShells = false;
    
    [Tooltip("Shell casing prefab")]
    public GameObject shellPrefab;
    
    [Tooltip("Shell ejection force")]
    [Range(1f, 50f)]
    public float shellEjectionForce = 10f;
    
    [Tooltip("Shell ejection force variance")]
    [Range(0f, 10f)]
    public float shellForceVariance = 2f;
    
    [Tooltip("Shell rotation torque")]
    public Vector3 shellTorque = new Vector3(100f, 100f, 0f);
    
    [Tooltip("Shell torque variance")]
    [Range(0f, 50f)]
    public float shellTorqueVariance = 20f;
    #endregion

    #region Bullet Holes
    [Header("Bullet Holes")]
    [Tooltip("Enable bullet hole decals")]
    public bool enableBulletHoles = true;
    
    [Tooltip("Detection method for bullet holes")]
    public BulletHoleDetection bulletHoleDetection = BulletHoleDetection.Tag;
    
    [Tooltip("Custom bullet hole mappings")]
    public List<BulletHoleMapping> bulletHoleMappings = new List<BulletHoleMapping>();
    
    [Tooltip("Default bullet holes (fallback)")]
    public List<BulletHolePool> defaultBulletHoles = new List<BulletHolePool>();
    
    [Tooltip("Surfaces that don't get bullet holes")]
    public List<BulletHoleMapping> bulletHoleExclusions = new List<BulletHoleMapping>();
    #endregion

    #region Audio
    [Header("Audio")]
    [Tooltip("Fire sound")]
    public AudioClip fireSound;
    
    [Tooltip("Reload sound")]
    public AudioClip reloadSound;
    
    [Tooltip("Empty/dry fire sound")]
    public AudioClip dryFireSound;
    
    private AudioSource audioSource;
    #endregion

    #region Private State
    private float fireTimer = 0f;
    private float fireInterval;
    private int burstCounter = 0;
    private float burstTimer = 0f;
    private bool canFire = true;
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ValidateReferences();
        InitializeWeapon();
    }

    private void Start()
    {
        currentAccuracy = baseAccuracy;
        fireInterval = roundsPerSecond > 0 ? 1f / roundsPerSecond : 0.1f;
        currentAmmo = magazineCapacity;
        
        if (weaponModel != null)
        {
            originalWeaponPosition = weaponModel.transform.localPosition;
            originalWeaponRotation = weaponModel.transform.localRotation;
        }
    }

    private void Update()
    {
        UpdateTimers();
        RecoverAccuracy();
        RecoverRecoil();
        HandleBeamCooldown();
        
        if (isPlayerWeapon)
        {
            HandlePlayerInput();
        }
        
        if (!isBeaming && weaponType == WeaponType.Beam)
        {
            StopBeam();
        }
        isBeaming = false;
    }
    #endregion

    #region Initialization & Validation
    private void ValidateReferences()
    {
        if (raycastOrigin == null) raycastOrigin = transform;
        if (projectileSpawnPoint == null) projectileSpawnPoint = transform;
        if (muzzleEffectPoint == null) muzzleEffectPoint = transform;
        if (weaponModel == null) weaponModel = gameObject;
    }

    private void InitializeWeapon()
    {
        // Validate weapon-specific requirements
        if (weaponType == WeaponType.Projectile && projectilePrefab == null)
        {
            Debug.LogWarning($"[Weapon] Projectile type requires a projectile prefab! {gameObject.name}", this);
        }
        
        if (weaponType == WeaponType.Beam && beamMaterial == null)
        {
            Debug.LogWarning($"[Weapon] Beam type requires a beam material! {gameObject.name}", this);
        }
    }
    #endregion

    #region Timers & Recovery
    private void UpdateTimers()
    {
        fireTimer += Time.deltaTime;
        
        if (burstCounter >= burstCount)
        {
            burstTimer += Time.deltaTime;
            if (burstTimer >= burstPause)
            {
                burstCounter = 0;
                burstTimer = 0f;
            }
        }
    }

    private void RecoverAccuracy()
    {
        currentAccuracy = Mathf.Lerp(currentAccuracy, baseAccuracy, accuracyRecoveryRate * Time.deltaTime);
    }

    private void RecoverRecoil()
    {
        if (enableRecoil && weaponModel != null && weaponType != WeaponType.Beam)
        {
            weaponModel.transform.localPosition = Vector3.Lerp(
                weaponModel.transform.localPosition, 
                originalWeaponPosition, 
                recoilRecoverySpeed * Time.deltaTime
            );
            weaponModel.transform.localRotation = Quaternion.Lerp(
                weaponModel.transform.localRotation, 
                originalWeaponRotation, 
                recoilRecoverySpeed * Time.deltaTime
            );
        }
    }

    private void HandleBeamCooldown()
    {
        if (weaponType == WeaponType.Beam && !isBeaming)
        {
            beamHeat = Mathf.Max(0f, beamHeat - Time.deltaTime);
            
            if (beamHeat >= maxBeamDuration)
            {
                beamCoolingDown = true;
            }
            else if (beamHeat <= maxBeamDuration * 0.5f)
            {
                beamCoolingDown = false;
            }
        }
    }
    #endregion

    #region Player Input
    private void HandlePlayerInput()
    {
        InputAction fireAction = InputSystem.actions.FindAction("Attack");
        InputAction reloadAction = InputSystem.actions.FindAction("Reload");
        InputAction cancelAction = InputSystem.actions.FindAction("Crouch");
        
        bool isFiring = fireAction != null && fireAction.IsPressed();
        bool wantsReload = reloadAction != null && reloadAction.IsPressed();
        bool wantsCancel = cancelAction != null && cancelAction.IsPressed();
        
        // Handle firing based on weapon type
        if (weaponType == WeaponType.Beam)
        {
            if (isFiring && beamHeat < maxBeamDuration && !beamCoolingDown)
            {
                FireBeam();
            }
        }
        else
        {
            if (CanFireNow() && isFiring)
            {
                if (enableWarmup && fireMode == FireMode.SemiAuto)
                {
                    if (warmupCharge < maxWarmupTime)
                    {
                        warmupCharge += Time.deltaTime;
                    }
                }
                else if (!enableWarmup)
                {
                    TriggerFire();
                }
            }
            else if (enableWarmup && !isFiring && warmupCharge > 0f)
            {
                if (allowWarmupCancel && wantsCancel)
                {
                    warmupCharge = 0f;
                }
                else
                {
                    TriggerFire();
                }
            }
        }
        
        // Handle reload
        if (wantsReload)
        {
            Reload();
        }
        
        // Auto-reload
        if (autoReload && currentAmmo <= 0 && weaponType != WeaponType.Beam)
        {
            Reload();
        }
        
        // Reset fire permission for semi-auto
        if (!isFiring && fireMode == FireMode.SemiAuto)
        {
            canFire = true;
        }
    }

    private bool CanFireNow()
    {
        return fireTimer >= fireInterval && 
               burstCounter < burstCount && 
               canFire;
    }

    private void TriggerFire()
    {
        if (weaponType == WeaponType.Raycast)
        {
            StartCoroutine(FireWithDelay(FireRaycast));
        }
        else if (weaponType == WeaponType.Projectile)
        {
            StartCoroutine(FireWithDelay(LaunchProjectile));
        }
    }

    private IEnumerator FireWithDelay(System.Action fireAction)
    {
        fireTimer = 0f;
        burstCounter++;
        
        if (fireMode == FireMode.SemiAuto)
        {
            canFire = false;
        }
        
        SendMessageUpwards("OnWeaponFire", SendMessageOptions.DontRequireReceiver);
        
        if (fireDelay > 0f)
        {
            yield return new WaitForSeconds(fireDelay);
        }
        
        fireAction?.Invoke();
    }
    #endregion

    #region Public Fire Methods
    public void AIFire()
    {
        if (weaponType == WeaponType.Beam)
        {
            if (beamHeat < maxBeamDuration && !beamCoolingDown)
            {
                FireBeam();
            }
        }
        else if (CanFireNow())
        {
            TriggerFire();
        }
    }
    #endregion

    #region Raycast Firing
    private void FireRaycast()
    {
        if (currentAmmo <= 0)
        {
            PlayDryFire();
            return;
        }
        
        if (!infiniteAmmo)
        {
            currentAmmo--;
        }
        
        float warmupMultiplier = enableWarmup ? (warmupCharge / maxWarmupTime) : 1f;
        float finalDamage = warmupAffectsDamage ? damage * (1f + warmupMultiplier * (warmupDamageMultiplier - 1f)) : damage;
        warmupCharge = 0f;
        
        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector3 direction = CalculateAccurateDirection();
            Ray ray = new Ray(raycastOrigin.position, direction);
            
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                ApplyDamage(hit.collider.gameObject, finalDamage);
                ApplyImpact(hit, ray.direction);
                CreateBulletHole(hit);
                CreateImpactEffect(hit.point, hit.normal);
            }
            
            currentAccuracy -= accuracyDecayPerShot;
            currentAccuracy = Mathf.Max(0f, currentAccuracy);
        }
        
        ApplyRecoil();
        PlayMuzzleFlash();
        EjectShell();
        PlayFireSound();
    }

    private Vector3 CalculateAccurateDirection()
    {
        float spread = (100f - currentAccuracy) / 1000f;
        Vector3 direction = raycastOrigin.forward;
        direction.x += Random.Range(-spread, spread);
        direction.y += Random.Range(-spread, spread);
        direction.z += Random.Range(-spread, spread);
        return direction.normalized;
    }
    #endregion

    #region Projectile Firing
    private void LaunchProjectile()
    {
        if (currentAmmo <= 0)
        {
            PlayDryFire();
            return;
        }
        
        if (!infiniteAmmo)
        {
            currentAmmo--;
        }
        
        float warmupMultiplier = enableWarmup ? (warmupCharge / maxWarmupTime) : 1f;
        warmupCharge = 0f;
        
        for (int i = 0; i < pelletsPerShot; i++)
        {
            if (projectilePrefab != null)
            {
                GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                
                if (enableWarmup)
                {
                    if (warmupAffectsDamage)
                    {
                        proj.SendMessage("MultiplyDamage", 1f + warmupMultiplier * (warmupDamageMultiplier - 1f), SendMessageOptions.DontRequireReceiver);
                    }
                    if (warmupAffectsForce)
                    {
                        proj.SendMessage("MultiplyInitialForce", 1f + warmupMultiplier * (warmupForceMultiplier - 1f), SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }
        
        ApplyRecoil();
        PlayMuzzleFlash();
        EjectShell();
        PlayFireSound();
    }
    #endregion

    #region Beam Firing
    private void FireBeam()
    {
        isBeaming = true;
        SendMessageUpwards("OnWeaponBeamFire", SendMessageOptions.DontRequireReceiver);
        
        if (!infiniteBeam)
        {
            beamHeat += Time.deltaTime;
        }
        
        if (beamObject == null)
        {
            beamObject = new GameObject("BeamEffect", typeof(LineRenderer));
            beamObject.transform.SetParent(transform);
        }
        
        LineRenderer lineRenderer = beamObject.GetComponent<LineRenderer>();
        ConfigureBeamRenderer(lineRenderer);
        
        List<Vector3> beamPoints = CalculateBeamPath();
        lineRenderer.positionCount = beamPoints.Count;
        lineRenderer.SetPositions(beamPoints.ToArray());
        
        if (!audioSource.isPlaying && fireSound != null)
        {
            audioSource.clip = fireSound;
            audioSource.Play();
        }
    }

    private void ConfigureBeamRenderer(LineRenderer lr)
    {
        lr.material = beamMaterial;
        lr.startColor = beamColor;
        lr.endColor = beamColor;
        lr.startWidth = beamStartWidth;
        lr.endWidth = beamEndWidth;
    }

    private List<Vector3> CalculateBeamPath()
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(raycastOrigin.position);
        
        Vector3 currentPosition = raycastOrigin.position;
        Vector3 currentDirection = raycastOrigin.forward;
        int reflectionCount = 0;
        
        while (reflectionCount < maxReflections)
        {
            Ray ray = new Ray(currentPosition, currentDirection);
            
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                points.Add(hit.point);
                ApplyDamage(hit.collider.gameObject, beamDamagePerSecond * Time.deltaTime);
                CreateImpactEffect(hit.point, hit.normal);
                
                if (!beamReflects) break;
                if (reflectiveMaterial != null && !HasMaterial(hit.collider.gameObject, reflectiveMaterial)) break;
                
                currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                currentPosition = hit.point + currentDirection * 0.01f;
                reflectionCount++;
            }
            else
            {
                points.Add(currentPosition + currentDirection * range);
                break;
            }
        }
        
        return points;
    }

    public void StopBeam()
    {
        beamHeat = Mathf.Max(0f, beamHeat - Time.deltaTime);
        
        if (beamObject != null)
        {
            Destroy(beamObject);
            beamObject = null;
        }
        
        audioSource.Stop();
        SendMessageUpwards("OnWeaponBeamStop", SendMessageOptions.DontRequireReceiver);
    }
    #endregion

    #region Damage & Impact
    private void ApplyDamage(GameObject target, float damageAmount)
    {
        target.SendMessageUpwards("ChangeHealth", -damageAmount, SendMessageOptions.DontRequireReceiver);
        target.SendMessageUpwards("TakeDamage", (int)damageAmount, SendMessageOptions.DontRequireReceiver);
    }

    private void ApplyImpact(RaycastHit hit, Vector3 direction)
    {
        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForce(direction * damage * impactForce, ForceMode.Impulse);
        }
    }
    #endregion

    #region Visual Effects
    private void ApplyRecoil()
    {
        if (!enableRecoil || !isPlayerWeapon || weaponModel == null) return;
        
        float kickBack = Random.Range(recoilKickMin, recoilKickMax);
        float kickRotation = Random.Range(recoilRotationMin, recoilRotationMax);
        
        weaponModel.transform.Translate(Vector3.back * kickBack, Space.Self);
        weaponModel.transform.Rotate(Vector3.left * kickRotation, Space.Self);
    }

    private void PlayMuzzleFlash()
    {
        if (!enableMuzzleFlash || muzzleFlashPrefabs.Length == 0) return;
        
        GameObject flash = muzzleFlashPrefabs[Random.Range(0, muzzleFlashPrefabs.Length)];
        if (flash != null)
        {
            Instantiate(flash, muzzleEffectPoint.position, muzzleEffectPoint.rotation);
        }
    }

    private void EjectShell()
    {
        if (!ejectShells || shellPrefab == null || shellEjectPoint == null) return;
        
        GameObject shell = Instantiate(shellPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
        
        if (shell.TryGetComponent<Rigidbody>(out var rb))
        {
            float force = shellEjectionForce + Random.Range(-shellForceVariance, shellForceVariance);
            rb.AddRelativeForce(Vector3.right * force, ForceMode.Impulse);
            
            Vector3 torque = shellTorque + Random.insideUnitSphere * shellTorqueVariance;
            rb.AddRelativeTorque(torque, ForceMode.Impulse);
        }
    }

    private void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        if (!enableImpactEffects || impactEffectPrefabs.Length == 0) return;
        
        GameObject effect = impactEffectPrefabs[Random.Range(0, impactEffectPrefabs.Length)];
        if (effect != null)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Instantiate(effect, position, rotation);
        }
    }

    private void CreateBulletHole(RaycastHit hit)
    {
        if (!enableBulletHoles) return;
        
        // Check exclusions
        if (IsExcluded(hit)) return;
        
        BulletHolePool pool = FindBulletHolePool(hit);
        
        if (pool != null)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            pool.PlaceBulletHole(hit.point, rotation);
        }
    }

    private bool IsExcluded(RaycastHit hit)
    {
        foreach (var exclusion in bulletHoleExclusions)
        {
            if (bulletHoleDetection == BulletHoleDetection.Tag && hit.collider.CompareTag(exclusion.tag))
                return true;
            if (bulletHoleDetection == BulletHoleDetection.Material && HasMaterial(hit.collider.gameObject, exclusion.material))
                return true;
            if (bulletHoleDetection == BulletHoleDetection.PhysicMaterial && hit.collider.sharedMaterial == exclusion.physicMaterial)
                return true;
        }
        return false;
    }

    private BulletHolePool FindBulletHolePool(RaycastHit hit)
    {
        foreach (var mapping in bulletHoleMappings)
        {
            bool match = false;
            
            if (bulletHoleDetection == BulletHoleDetection.Tag)
                match = hit.collider.CompareTag(mapping.tag);
            else if (bulletHoleDetection == BulletHoleDetection.Material)
                match = HasMaterial(hit.collider.gameObject, mapping.material);
            else if (bulletHoleDetection == BulletHoleDetection.PhysicMaterial)
                match = hit.collider.sharedMaterial == mapping.physicMaterial;
            
            if (match && mapping.bulletHolePool != null)
                return mapping.bulletHolePool;
        }
        
        // Return random default if available
        if (defaultBulletHoles.Count > 0)
        {
            return defaultBulletHoles[Random.Range(0, defaultBulletHoles.Count)];
        }
        
        return null;
    }
    #endregion

    #region Audio
    private void PlayFireSound()
    {
        if (fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }

    private void PlayDryFire()
    {
        if (dryFireSound != null)
        {
            audioSource.PlayOneShot(dryFireSound);
        }
    }
    #endregion

    #region Reload
    public void Reload()
    {
        if (infiniteAmmo || currentAmmo == magazineCapacity) return;
        
        currentAmmo = magazineCapacity;
        fireTimer = -reloadTime;
        
        if (reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        SendMessageUpwards("OnWeaponReload", SendMessageOptions.DontRequireReceiver);
    }
    #endregion

    #region Utilities
    private bool HasMaterial(GameObject obj, Material mat)
    {
        if (mat == null) return false;
        
        if (obj.TryGetComponent<MeshRenderer>(out var renderer))
        {
            return renderer.sharedMaterial == mat;
        }
        
        renderer = obj.GetComponentInChildren<MeshRenderer>();
        if (renderer != null) return renderer.sharedMaterial == mat;
        
        renderer = obj.GetComponentInParent<MeshRenderer>();
        return renderer != null && renderer.sharedMaterial == mat;
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => magazineCapacity;
    public float GetBeamHeat() => beamHeat;
    public float GetMaxBeamHeat() => maxBeamDuration;
    #endregion
}
