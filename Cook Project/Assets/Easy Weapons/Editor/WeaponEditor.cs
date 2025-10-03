/// <summary>
/// WeaponEditor.cs - Modernized for Unity 6.2
/// Custom inspector for the Weapon system
/// </summary>

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Weapon))]
public class WeaponEditor : Editor
{
    private bool showReferences = true;
    private bool showFireRate = true;
    private bool showAmmo = true;
    private bool showDamage = true;
    private bool showProjectile = false;
    private bool showBeam = false;
    private bool showAccuracy = true;
    private bool showWarmup = false;
    private bool showRecoil = true;
    private bool showEffects = true;
    private bool showBulletHoles = true;
    private bool showAudio = true;

    public override void OnInspectorGUI()
    {
        Weapon weapon = (Weapon)target;
        
        EditorGUILayout.Space();
        
        // Weapon Type & Mode
        EditorGUILayout.LabelField("Weapon Configuration", EditorStyles.boldLabel);
        weapon.weaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weapon.weaponType);
        weapon.fireMode = (FireMode)EditorGUILayout.EnumPopup("Fire Mode", weapon.fireMode);
        weapon.isPlayerWeapon = EditorGUILayout.Toggle("Player Weapon", weapon.isPlayerWeapon);
        
        EditorGUILayout.Space();
        
        // References
        showReferences = EditorGUILayout.Foldout(showReferences, "References", true);
        if (showReferences)
        {
            EditorGUI.indentLevel++;
            weapon.weaponModel = (GameObject)EditorGUILayout.ObjectField("Weapon Model", weapon.weaponModel, typeof(GameObject), true);
            
            if (weapon.weaponType == WeaponType.Raycast || weapon.weaponType == WeaponType.Beam)
                weapon.raycastOrigin = (Transform)EditorGUILayout.ObjectField("Raycast Origin", weapon.raycastOrigin, typeof(Transform), true);
            
            if (weapon.weaponType == WeaponType.Projectile)
                weapon.projectileSpawnPoint = (Transform)EditorGUILayout.ObjectField("Projectile Spawn Point", weapon.projectileSpawnPoint, typeof(Transform), true);
            
            weapon.muzzleEffectPoint = (Transform)EditorGUILayout.ObjectField("Muzzle Effect Point", weapon.muzzleEffectPoint, typeof(Transform), true);
            weapon.shellEjectPoint = (Transform)EditorGUILayout.ObjectField("Shell Eject Point", weapon.shellEjectPoint, typeof(Transform), true);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Fire Rate
        showFireRate = EditorGUILayout.Foldout(showFireRate, "Fire Rate & Timing", true);
        if (showFireRate)
        {
            EditorGUI.indentLevel++;
            weapon.roundsPerSecond = EditorGUILayout.Slider("Rounds Per Second", weapon.roundsPerSecond, 0.1f, 30f);
            weapon.fireDelay = EditorGUILayout.Slider("Fire Delay", weapon.fireDelay, 0f, 2f);
            
            if (weapon.fireMode == FireMode.Burst)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Burst Settings", EditorStyles.miniBoldLabel);
                weapon.burstCount = EditorGUILayout.IntSlider("Burst Count", weapon.burstCount, 2, 10);
                weapon.burstPause = EditorGUILayout.Slider("Burst Pause", weapon.burstPause, 0f, 2f);
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Ammo
        if (weapon.weaponType != WeaponType.Beam)
        {
            showAmmo = EditorGUILayout.Foldout(showAmmo, "Ammunition", true);
            if (showAmmo)
            {
                EditorGUI.indentLevel++;
                weapon.infiniteAmmo = EditorGUILayout.Toggle("Infinite Ammo", weapon.infiniteAmmo);
                
                if (!weapon.infiniteAmmo)
                {
                    weapon.magazineCapacity = EditorGUILayout.IntSlider("Magazine Capacity", weapon.magazineCapacity, 1, 999);
                    weapon.reloadTime = EditorGUILayout.Slider("Reload Time", weapon.reloadTime, 0.1f, 10f);
                    weapon.autoReload = EditorGUILayout.Toggle("Auto Reload", weapon.autoReload);
                }
                
                weapon.pelletsPerShot = EditorGUILayout.IntSlider("Pellets Per Shot", weapon.pelletsPerShot, 1, 20);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Damage & Power
        showDamage = EditorGUILayout.Foldout(showDamage, "Damage & Power", true);
        if (showDamage)
        {
            EditorGUI.indentLevel++;
            
            if (weapon.weaponType == WeaponType.Raycast)
            {
                weapon.damage = EditorGUILayout.Slider("Damage", weapon.damage, 1f, 1000f);
            }
            else if (weapon.weaponType == WeaponType.Beam)
            {
                weapon.beamDamagePerSecond = EditorGUILayout.Slider("Damage Per Second", weapon.beamDamagePerSecond, 0.1f, 100f);
            }
            
            if (weapon.weaponType != WeaponType.Projectile)
            {
                weapon.range = EditorGUILayout.Slider("Range", weapon.range, 1f, 1000f);
                weapon.impactForce = EditorGUILayout.Slider("Impact Force", weapon.impactForce, 1f, 100f);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Projectile Settings
        if (weapon.weaponType == WeaponType.Projectile)
        {
            showProjectile = EditorGUILayout.Foldout(showProjectile, "Projectile Settings", true);
            if (showProjectile)
            {
                EditorGUI.indentLevel++;
                weapon.projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", weapon.projectilePrefab, typeof(GameObject), false);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Beam Settings
        if (weapon.weaponType == WeaponType.Beam)
        {
            showBeam = EditorGUILayout.Foldout(showBeam, "Beam Settings", true);
            if (showBeam)
            {
                EditorGUI.indentLevel++;
                weapon.maxBeamDuration = EditorGUILayout.Slider("Max Duration", weapon.maxBeamDuration, 0.1f, 10f);
                weapon.infiniteBeam = EditorGUILayout.Toggle("Infinite Beam", weapon.infiniteBeam);
                weapon.beamReflects = EditorGUILayout.Toggle("Reflects", weapon.beamReflects);
                
                if (weapon.beamReflects)
                {
                    weapon.reflectiveMaterial = (Material)EditorGUILayout.ObjectField("Reflective Material", weapon.reflectiveMaterial, typeof(Material), false);
                    weapon.maxReflections = EditorGUILayout.IntSlider("Max Reflections", weapon.maxReflections, 1, 10);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Beam Visuals", EditorStyles.miniBoldLabel);
                weapon.beamMaterial = (Material)EditorGUILayout.ObjectField("Material", weapon.beamMaterial, typeof(Material), false);
                weapon.beamColor = EditorGUILayout.ColorField("Color", weapon.beamColor);
                weapon.beamStartWidth = EditorGUILayout.Slider("Start Width", weapon.beamStartWidth, 0.01f, 2f);
                weapon.beamEndWidth = EditorGUILayout.Slider("End Width", weapon.beamEndWidth, 0.01f, 2f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Accuracy
        if (weapon.weaponType == WeaponType.Raycast)
        {
            showAccuracy = EditorGUILayout.Foldout(showAccuracy, "Accuracy", true);
            if (showAccuracy)
            {
                EditorGUI.indentLevel++;
                weapon.baseAccuracy = EditorGUILayout.Slider("Base Accuracy", weapon.baseAccuracy, 0f, 100f);
                weapon.accuracyDecayPerShot = EditorGUILayout.Slider("Decay Per Shot", weapon.accuracyDecayPerShot, 0f, 10f);
                weapon.accuracyRecoveryRate = EditorGUILayout.Slider("Recovery Rate", weapon.accuracyRecoveryRate, 0.1f, 100f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Warmup
        if (weapon.fireMode == FireMode.SemiAuto && weapon.weaponType != WeaponType.Beam)
        {
            showWarmup = EditorGUILayout.Foldout(showWarmup, "Warmup / Charge System", true);
            if (showWarmup)
            {
                EditorGUI.indentLevel++;
                weapon.enableWarmup = EditorGUILayout.Toggle("Enable Warmup", weapon.enableWarmup);
                
                if (weapon.enableWarmup)
                {
                    weapon.maxWarmupTime = EditorGUILayout.Slider("Max Warmup Time", weapon.maxWarmupTime, 0.1f, 10f);
                    weapon.warmupAffectsDamage = EditorGUILayout.Toggle("Affects Damage", weapon.warmupAffectsDamage);
                    
                    if (weapon.warmupAffectsDamage)
                        weapon.warmupDamageMultiplier = EditorGUILayout.Slider("Damage Multiplier", weapon.warmupDamageMultiplier, 1f, 10f);
                    
                    weapon.warmupAffectsForce = EditorGUILayout.Toggle("Affects Force", weapon.warmupAffectsForce);
                    
                    if (weapon.warmupAffectsForce)
                        weapon.warmupForceMultiplier = EditorGUILayout.Slider("Force Multiplier", weapon.warmupForceMultiplier, 1f, 10f);
                    
                    weapon.allowWarmupCancel = EditorGUILayout.Toggle("Allow Cancel", weapon.allowWarmupCancel);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Recoil
        if (weapon.weaponType != WeaponType.Beam)
        {
            showRecoil = EditorGUILayout.Foldout(showRecoil, "Recoil", true);
            if (showRecoil)
            {
                EditorGUI.indentLevel++;
                weapon.enableRecoil = EditorGUILayout.Toggle("Enable Recoil", weapon.enableRecoil);
                
                if (weapon.enableRecoil)
                {
                    weapon.recoilKickMin = EditorGUILayout.Slider("Kick Min", weapon.recoilKickMin, 0f, 1f);
                    weapon.recoilKickMax = EditorGUILayout.Slider("Kick Max", weapon.recoilKickMax, 0f, 1f);
                    weapon.recoilRotationMin = EditorGUILayout.Slider("Rotation Min", weapon.recoilRotationMin, 0f, 5f);
                    weapon.recoilRotationMax = EditorGUILayout.Slider("Rotation Max", weapon.recoilRotationMax, 0f, 5f);
                    weapon.recoilRecoverySpeed = EditorGUILayout.Slider("Recovery Speed", weapon.recoilRecoverySpeed, 1f, 100f);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Visual Effects
        showEffects = EditorGUILayout.Foldout(showEffects, "Visual Effects", true);
        if (showEffects)
        {
            EditorGUI.indentLevel++;
            
            // Muzzle Flash
            weapon.enableMuzzleFlash = EditorGUILayout.Toggle("Muzzle Flash", weapon.enableMuzzleFlash);
            if (weapon.enableMuzzleFlash)
            {
                EditorGUI.indentLevel++;
                SerializedProperty muzzleArray = serializedObject.FindProperty("muzzleFlashPrefabs");
                EditorGUILayout.PropertyField(muzzleArray, true);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Impact Effects
            weapon.enableImpactEffects = EditorGUILayout.Toggle("Impact Effects", weapon.enableImpactEffects);
            if (weapon.enableImpactEffects)
            {
                EditorGUI.indentLevel++;
                SerializedProperty impactArray = serializedObject.FindProperty("impactEffectPrefabs");
                EditorGUILayout.PropertyField(impactArray, true);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Shell Ejection
            weapon.ejectShells = EditorGUILayout.Toggle("Eject Shells", weapon.ejectShells);
            if (weapon.ejectShells)
            {
                EditorGUI.indentLevel++;
                weapon.shellPrefab = (GameObject)EditorGUILayout.ObjectField("Shell Prefab", weapon.shellPrefab, typeof(GameObject), false);
                weapon.shellEjectionForce = EditorGUILayout.Slider("Ejection Force", weapon.shellEjectionForce, 1f, 50f);
                weapon.shellForceVariance = EditorGUILayout.Slider("Force Variance", weapon.shellForceVariance, 0f, 10f);
                weapon.shellTorque = EditorGUILayout.Vector3Field("Torque", weapon.shellTorque);
                weapon.shellTorqueVariance = EditorGUILayout.Slider("Torque Variance", weapon.shellTorqueVariance, 0f, 50f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Bullet Holes
        if (weapon.weaponType == WeaponType.Raycast)
        {
            showBulletHoles = EditorGUILayout.Foldout(showBulletHoles, "Bullet Holes", true);
            if (showBulletHoles)
            {
                EditorGUI.indentLevel++;
                weapon.enableBulletHoles = EditorGUILayout.Toggle("Enable Bullet Holes", weapon.enableBulletHoles);
                
                if (weapon.enableBulletHoles)
                {
                    weapon.bulletHoleDetection = (BulletHoleDetection)EditorGUILayout.EnumPopup("Detection Method", weapon.bulletHoleDetection);
                    
                    SerializedProperty mappings = serializedObject.FindProperty("bulletHoleMappings");
                    EditorGUILayout.PropertyField(mappings, true);
                    
                    SerializedProperty defaults = serializedObject.FindProperty("defaultBulletHoles");
                    EditorGUILayout.PropertyField(defaults, true);
                    
                    SerializedProperty exclusions = serializedObject.FindProperty("bulletHoleExclusions");
                    EditorGUILayout.PropertyField(exclusions, true);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Audio
        showAudio = EditorGUILayout.Foldout(showAudio, "Audio", true);
        if (showAudio)
        {
            EditorGUI.indentLevel++;
            weapon.fireSound = (AudioClip)EditorGUILayout.ObjectField("Fire Sound", weapon.fireSound, typeof(AudioClip), false);
            weapon.reloadSound = (AudioClip)EditorGUILayout.ObjectField("Reload Sound", weapon.reloadSound, typeof(AudioClip), false);
            weapon.dryFireSound = (AudioClip)EditorGUILayout.ObjectField("Dry Fire Sound", weapon.dryFireSound, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
}
