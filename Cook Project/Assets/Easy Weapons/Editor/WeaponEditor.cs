/// <summary>
/// WeaponEditor.cs
/// Author: MutantGopher
/// This script creates a custom inspector for the weapon system in Weapon.cs.
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Weapon))]
public class WeaponEditor : Editor
{
	private bool showPluginSupport = false;
	private bool showGeneral = false;
	private bool showAmmo = false;
	private bool showROF = false;
	private bool showPower = false;
	private bool showAccuracy = false;
	private bool showWarmup = false;
	private bool showRecoil = false;
	private bool showEffects = false;
	private bool showBulletHoles = false;
	private bool showCrosshairs = false;
	private bool showAudio = false;

	public override void OnInspectorGUI()
	{
		// Get a reference to the weapon script
		Weapon weapon = (Weapon)target;

		// Weapon type
		weapon.type = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weapon.type);

		// External tools support
		showPluginSupport = EditorGUILayout.Foldout(showPluginSupport, "3rd Party Plugin Support");
		if (showPluginSupport)
		{
			// Shooter AI support
			weapon.shooterAIEnabled = EditorGUILayout.Toggle(new GUIContent("Shooter AI", "Support for Shooter AI by Gateway Games"), weapon.shooterAIEnabled);

			// Bloody Mess support
			weapon.bloodyMessEnabled = EditorGUILayout.Toggle (new GUIContent("Bloody Mess"), weapon.bloodyMessEnabled);
			if (weapon.bloodyMessEnabled)
			{
				weapon.weaponType = EditorGUILayout.IntField("Weapon Type", weapon.weaponType);
			}
		}

		// General
		showGeneral = EditorGUILayout.Foldout(showGeneral, "General");
		if (showGeneral)
		{
			weapon.playerWeapon = EditorGUILayout.Toggle("Player's Weapon", weapon.playerWeapon);
			if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Projectile)
				weapon.auto = (Auto)EditorGUILayout.EnumPopup("Auto Type", weapon.auto);
			weapon.weaponModel = (GameObject)EditorGUILayout.ObjectField("Weapon Model", weapon.weaponModel, typeof(GameObject), true);
			if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Beam)
				weapon.raycastStartSpot = (Transform)EditorGUILayout.ObjectField("Raycasting Point", weapon.raycastStartSpot, typeof(Transform), true);

			// Projectile
			if (weapon.type == WeaponType.Projectile)
			{
				weapon.projectile = (GameObject)EditorGUILayout.ObjectField("Projectile", weapon.projectile, typeof(GameObject), false);
				weapon.projectileSpawnSpot = (Transform)EditorGUILayout.ObjectField("Projectile Spawn Point", weapon.projectileSpawnSpot, typeof(Transform), true);
			}

			// Beam
			if (weapon.type == WeaponType.Beam)
			{
				weapon.reflect = EditorGUILayout.Toggle("Reflect", weapon.reflect);
				weapon.reflectionMaterial = (Material)EditorGUILayout.ObjectField("Reflection Material", weapon.reflectionMaterial, typeof(Material), false);
				weapon.maxReflections = EditorGUILayout.IntField("Max Reflections", weapon.maxReflections);
				weapon.beamTypeName = EditorGUILayout.TextField("Beam Effect Name", weapon.beamTypeName);
				weapon.maxBeamHeat = EditorGUILayout.FloatField("Max Heat", weapon.maxBeamHeat);
				weapon.infiniteBeam = EditorGUILayout.Toggle("Infinite", weapon.infiniteBeam);
				weapon.beamMaterial = (Material)EditorGUILayout.ObjectField("Material", weapon.beamMaterial, typeof(Material), false);
				weapon.beamColor = EditorGUILayout.ColorField("Color", weapon.beamColor);
				weapon.startBeamWidth = EditorGUILayout.FloatField("Start Width", weapon.startBeamWidth);
				weapon.endBeamWidth = EditorGUILayout.FloatField("End Width", weapon.endBeamWidth);
			}

			if (weapon.type == WeaponType.Beam)
				weapon.showCurrentAmmo = EditorGUILayout.Toggle("Show Current Heat", weapon.showCurrentAmmo);

		}



		// Power
		if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Beam)
		{
			showPower = EditorGUILayout.Foldout(showPower, "Power");
			if (showPower)
			{
				if (weapon.type == WeaponType.Raycast)
					weapon.power = EditorGUILayout.FloatField("Power", weapon.power);
				else
					weapon.beamPower = EditorGUILayout.FloatField("Power", weapon.beamPower);

				weapon.forceMultiplier = EditorGUILayout.FloatField("Force Multiplier", weapon.forceMultiplier);
				weapon.range = EditorGUILayout.FloatField("Range", weapon.range);
			}
		}


		// ROF
		if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Projectile)
		{
			showROF = EditorGUILayout.Foldout(showROF, "Rate Of Fire");
			if (showROF)
			{
				weapon.rateOfFire = EditorGUILayout.FloatField("Rate Of Fire", weapon.rateOfFire);
				weapon.delayBeforeFire = EditorGUILayout.FloatField("Delay Before Fire", weapon.delayBeforeFire);
				// Burst
				weapon.burstRate = EditorGUILayout.IntField("Burst Rate", weapon.burstRate);
				weapon.burstPause = EditorGUILayout.FloatField("Burst Pause", weapon.burstPause);
			}
		}


		// Ammo
		if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Projectile)
		{
			showAmmo = EditorGUILayout.Foldout(showAmmo, "Ammunition");
			if (showAmmo)
			{
				weapon.infiniteAmmo = EditorGUILayout.Toggle("Infinite Ammo", weapon.infiniteAmmo);

				if (!weapon.infiniteAmmo)
				{
					weapon.ammoCapacity = EditorGUILayout.IntField("Ammo Capacity", weapon.ammoCapacity);
					weapon.reloadTime = EditorGUILayout.FloatField("Reload Time", weapon.reloadTime);
					weapon.showCurrentAmmo = EditorGUILayout.Toggle("Show Current Ammo", weapon.showCurrentAmmo);
					weapon.reloadAutomatically = EditorGUILayout.Toggle("Reload Automatically", weapon.reloadAutomatically);
				}
				weapon.shotPerRound = EditorGUILayout.IntField("Shots Per Round", weapon.shotPerRound);
			}
		}



		// Accuracy
		if (weapon.type == WeaponType.Raycast)
		{
			showAccuracy = EditorGUILayout.Foldout(showAccuracy, "Accuracy");
			if (showAccuracy)
			{
				weapon.accuracy = EditorGUILayout.FloatField("Accuracy", weapon.accuracy);
				weapon.accuracyDropPerShot = EditorGUILayout.FloatField("Accuracy Drop Per Shot", weapon.accuracyDropPerShot);
				weapon.accuracyRecoverRate = EditorGUILayout.FloatField("Accuracy Recover Rate", weapon.accuracyRecoverRate);
			}
		}


		// Warmup
		if ((weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Projectile) && weapon.auto == Auto.Semi)
		{
			showWarmup = EditorGUILayout.Foldout(showWarmup, "Warmup");
			if (showWarmup)
			{
				weapon.warmup = EditorGUILayout.Toggle("Warmup", weapon.warmup);

				if (weapon.warmup)
				{
					weapon.maxWarmup = EditorGUILayout.FloatField("Max Warmup", weapon.maxWarmup);
					
					if (weapon.type == WeaponType.Projectile)
					{
						weapon.multiplyForce = EditorGUILayout.Toggle("Multiply Force", weapon.multiplyForce);
						if (weapon.multiplyForce)
							weapon.initialForceMultiplier = EditorGUILayout.FloatField("Initial Force Multiplier", weapon.initialForceMultiplier);

						weapon.multiplyPower = EditorGUILayout.Toggle("Multiply Power", weapon.multiplyPower);
						if (weapon.multiplyPower)
							weapon.powerMultiplier = EditorGUILayout.FloatField("Power Multiplier", weapon.powerMultiplier);
					}
					else	// If this is a raycast weapon
					{
						weapon.powerMultiplier = EditorGUILayout.FloatField("Power Multiplier", weapon.powerMultiplier);
					}
					weapon.allowCancel = EditorGUILayout.Toggle("Allow Cancel", weapon.allowCancel);
				}
			}
		}


		// Recoil
		if (weapon.type == WeaponType.Raycast || weapon.type == WeaponType.Projectile)
		{
			showRecoil = EditorGUILayout.Foldout(showRecoil, "Recoil");
			if (showRecoil)
			{
				weapon.recoil = EditorGUILayout.Toggle("Recoil", weapon.recoil);

				if (weapon.recoil)
				{
					weapon.recoilKickBackMin = EditorGUILayout.FloatField("Recoil Move Min", weapon.recoilKickBackMin);
					weapon.recoilKickBackMax = EditorGUILayout.FloatField("Recoil Move Max", weapon.recoilKickBackMax);
					weapon.recoilRotationMin = EditorGUILayout.FloatField("Recoil Rotation Min", weapon.recoilRotationMin);
					weapon.recoilRotationMax = EditorGUILayout.FloatField("Recoil Rotation Max", weapon.recoilRotationMax);
					weapon.recoilRecoveryRate = EditorGUILayout.FloatField("Recoil Recovery Rate", weapon.recoilRecoveryRate);
				}
			}
		}


		// Shells
		showEffects = EditorGUILayout.Foldout(showEffects, "Effects");
		if (showEffects)
		{
			weapon.spitShells = EditorGUILayout.Toggle("Spit Shells", weapon.spitShells);
			if (weapon.spitShells)
			{
				weapon.shell = (GameObject)EditorGUILayout.ObjectField("Shell", weapon.shell, typeof(GameObject), false);
				weapon.shellSpitForce = EditorGUILayout.FloatField("Shell Spit Force", weapon.shellSpitForce);
				weapon.shellForceRandom = EditorGUILayout.FloatField("Force Variant", weapon.shellForceRandom);
				weapon.shellSpitTorqueX = EditorGUILayout.FloatField("X Torque", weapon.shellSpitTorqueX);
				weapon.shellSpitTorqueY = EditorGUILayout.FloatField("Y Torque", weapon.shellSpitTorqueY);
				weapon.shellTorqueRandom = EditorGUILayout.FloatField("Torque Variant", weapon.shellTorqueRandom);
				weapon.shellSpitPosition = (Transform)EditorGUILayout.ObjectField("Shell Spit Point", weapon.shellSpitPosition, typeof(Transform), true);
			}

			// Muzzle FX
			EditorGUILayout.Separator();
			weapon.makeMuzzleEffects = EditorGUILayout.Toggle("Muzzle Effects", weapon.makeMuzzleEffects);
			if (weapon.makeMuzzleEffects)
			{
				weapon.muzzleEffectsPosition = (Transform)EditorGUILayout.ObjectField("Muzzle FX Spawn Point", weapon.muzzleEffectsPosition, typeof(Transform), true);

				if (GUILayout.Button("Add"))
				{
					List<GameObject> temp = new List<GameObject>(weapon.muzzleEffects);
					temp.Add(null);
					weapon.muzzleEffects = temp.ToArray();
				}
				EditorGUI.indentLevel++;
				for (int i = 0; i < weapon.muzzleEffects.Length; i++)
				{
					EditorGUILayout.BeginHorizontal();
					weapon.muzzleEffects[i] = (GameObject)EditorGUILayout.ObjectField("Muzzle FX Prefabs", weapon.muzzleEffects[i], typeof(GameObject), false);
					if (GUILayout.Button("Remove"))
					{
						List<GameObject> temp = new List<GameObject>(weapon.muzzleEffects);
						temp.Remove(temp[i]);
						weapon.muzzleEffects = temp.ToArray();
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel--;
			}

			// Hit FX
			if (weapon.type != WeaponType.Projectile)
			{
				EditorGUILayout.Separator();
				weapon.makeHitEffects = EditorGUILayout.Toggle("Hit Effects", weapon.makeHitEffects);
				if (weapon.makeHitEffects)
				{
					if (GUILayout.Button("Add"))
					{
						List<GameObject> temp = new List<GameObject>(weapon.hitEffects);
						temp.Add(null);
						weapon.hitEffects = temp.ToArray();
					}
					EditorGUI.indentLevel++;
					for (int i = 0; i < weapon.hitEffects.Length; i++)
					{
						EditorGUILayout.BeginHorizontal();
						weapon.hitEffects[i] = (GameObject)EditorGUILayout.ObjectField("Hit FX Prefabs", weapon.hitEffects[i], typeof(GameObject), false);
						if (GUILayout.Button("Remove"))
						{
							List<GameObject> temp = new List<GameObject>(weapon.hitEffects);
							temp.Remove(temp[i]);
							weapon.hitEffects = temp.ToArray();
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUI.indentLevel--;
				}
			}
		}


		// Bullet Holes
		if (weapon.type == WeaponType.Raycast)
		{
			showBulletHoles = EditorGUILayout.Foldout(showBulletHoles, "Bullet Holes");
			if (showBulletHoles)
			{

				weapon.makeBulletHoles = EditorGUILayout.Toggle("Bullet Holes", weapon.makeBulletHoles);

				if (weapon.makeBulletHoles)
				{
					weapon.bhSystem = (BulletHoleSystem)EditorGUILayout.EnumPopup("Determined By", weapon.bhSystem);

					if (GUILayout.Button("Add"))
					{
						weapon.bulletHoleGroups.Add(new SmartBulletHoleGroup());
						weapon.bulletHolePoolNames.Add("Default");
					}

					EditorGUILayout.BeginVertical();

					for (int i = 0; i < weapon.bulletHolePoolNames.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();

						// The tag, material, or physic material by which the bullet hole is determined
						if (weapon.bhSystem == BulletHoleSystem.Tag)
						{
							weapon.bulletHoleGroups[i].tag = EditorGUILayout.TextField(weapon.bulletHoleGroups[i].tag);
						}
						else if (weapon.bhSystem == BulletHoleSystem.Material)
						{
							weapon.bulletHoleGroups[i].material = (Material)EditorGUILayout.ObjectField(weapon.bulletHoleGroups[i].material, typeof(Material), false);
						}
						else if (weapon.bhSystem == BulletHoleSystem.Physic_Material)
						{
							weapon.bulletHoleGroups[i].physicMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField(weapon.bulletHoleGroups[i].physicMaterial, typeof(PhysicsMaterial), false);
						}

						// The bullet hole to be instantiated for this type
						weapon.bulletHolePoolNames[i] = EditorGUILayout.TextField(weapon.bulletHolePoolNames[i]);

						if (GUILayout.Button("Remove"))
						{
							weapon.bulletHoleGroups.Remove(weapon.bulletHoleGroups[i]);
							weapon.bulletHolePoolNames.Remove(weapon.bulletHolePoolNames[i]);
						}

						EditorGUILayout.EndHorizontal();
					}

					// The default bullet holes to be instantiated when other specifications (above) are not met
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Default Bullet Holes");

					if (GUILayout.Button("Add"))
					{
						weapon.defaultBulletHoles.Add(null);
						weapon.defaultBulletHolePoolNames.Add("Default");
					}

					for (int i = 0; i < weapon.defaultBulletHolePoolNames.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();

						weapon.defaultBulletHolePoolNames[i] = EditorGUILayout.TextField(weapon.defaultBulletHolePoolNames[i]);

						if (GUILayout.Button("Remove"))
						{
							weapon.defaultBulletHoles.Remove(weapon.defaultBulletHoles[i]);
							weapon.defaultBulletHolePoolNames.Remove(weapon.defaultBulletHolePoolNames[i]);
							
						}

						EditorGUILayout.EndHorizontal();
					}


					// The exceptions to the bullet hole rules defined in the default bullet holes
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Exceptions");

					if (GUILayout.Button("Add"))
					{
						weapon.bulletHoleExceptions.Add(new SmartBulletHoleGroup());
					}

					for (int i = 0; i < weapon.bulletHoleExceptions.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();

						// The tag, material, or physic material by which the bullet hole is determined
						if (weapon.bhSystem == BulletHoleSystem.Tag)
						{
							weapon.bulletHoleExceptions[i].tag = EditorGUILayout.TextField(weapon.bulletHoleExceptions[i].tag);
						}
						else if (weapon.bhSystem == BulletHoleSystem.Material)
						{
							weapon.bulletHoleExceptions[i].material = (Material)EditorGUILayout.ObjectField(weapon.bulletHoleExceptions[i].material, typeof(Material), false);
						}
						else if (weapon.bhSystem == BulletHoleSystem.Physic_Material)
						{
							weapon.bulletHoleExceptions[i].physicMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField(weapon.bulletHoleExceptions[i].physicMaterial, typeof(PhysicsMaterial), false);
						}


						if (GUILayout.Button("Remove"))
						{
							weapon.bulletHoleExceptions.Remove(weapon.bulletHoleExceptions[i]);
						}

						EditorGUILayout.EndHorizontal();
					}


					EditorGUILayout.EndVertical();

					if (weapon.bulletHoleGroups.Count > 0)
					{
						EditorGUILayout.HelpBox("Assign bullet hole prefabs corresponding with tags, materials, or physic materials.  If you assign multiple bullet holes to the same parameter, one of them will be chosen at random.  The default bullet hole will be used when something is hit that doesn't match any of the other parameters.  The exceptions define parameters for which no bullet holes will be instantiated.", MessageType.None);
					}
				}

			}
		}


		// Crosshairs
		showCrosshairs = EditorGUILayout.Foldout(showCrosshairs, "Crosshairs");
		if (showCrosshairs)
		{
			weapon.showCrosshair = EditorGUILayout.Toggle("Show Crosshairs", weapon.showCrosshair);
			if (weapon.showCrosshair)
			{
				weapon.crosshairTexture = (Texture2D)EditorGUILayout.ObjectField("Crosshair Texture", weapon.crosshairTexture, typeof(Texture2D), false);
				weapon.crosshairLength = EditorGUILayout.IntField("Crosshair Length", weapon.crosshairLength);
				weapon.crosshairWidth = EditorGUILayout.IntField("Crosshair Width", weapon.crosshairWidth);
				weapon.startingCrosshairSize = EditorGUILayout.FloatField("Start Crosshair Size", weapon.startingCrosshairSize);
			}
		}
		

		// Audio
		showAudio = EditorGUILayout.Foldout(showAudio, "Audio");
		if (showAudio)
		{
			weapon.fireSound = (AudioClip)EditorGUILayout.ObjectField("Fire", weapon.fireSound, typeof(AudioClip), false);
			weapon.reloadSound = (AudioClip)EditorGUILayout.ObjectField("Reload", weapon.reloadSound, typeof(AudioClip), false);
			weapon.dryFireSound = (AudioClip)EditorGUILayout.ObjectField("Out of Ammo", weapon.dryFireSound, typeof(AudioClip), false);
		}


		// This makes the editor gui re-draw the inspector if values have changed
		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}
}

