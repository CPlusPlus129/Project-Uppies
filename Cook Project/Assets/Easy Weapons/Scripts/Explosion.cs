/// <summary>
/// Explosion.cs
/// Author: MutantGopher
/// Attach this script to your explosion prefabs.  It handles damage for
/// nearby healths, force for nearby rigidbodies, and camera shaking FX.
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Explosion : MonoBehaviour
{
	public bool shooterAISupport = false;		// Enable compatibility with Shooter AI by Gateway Games
	public bool bloodyMessSupport = false;		// Enable compatibility with Bloody Mess by Heavy Diesel Softworks
	public int weaponType = 0;					// Bloody Mess property

	public float explosionForce = 5.0f;			// The force with which nearby objects will be blasted outwards
	public float explosionRadius = 10.0f;		// The radius of the explosion
	public bool shakeCamera = true;				// Give camera shaking effects to nearby cameras that have the vibration component
	public float cameraShakeViolence = 0.5f;	// The violence of the camera shake effect
	public bool causeDamage = true;				// Whether or not the explosion should apply damage to nearby GameObjects with the Heatlh component
	public float damage = 10.0f;				// The multiplier by which the ammount of damage to be applied is determined

	IEnumerator Start()
	{
		// Wait one frame so that explosion force will be applied to debris which
		// might not yet be instantiated
		yield return null;

		// An array of nearby colliders
		Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius);

		// Apply damage to any nearby GameObjects with the Health component
		if (causeDamage)
		{
			foreach (Collider col in cols)
			{
				float damageAmount = damage * (1 / Vector3.Distance(transform.position, col.transform.position));

				// The Easy Weapons health system
				col.GetComponent<Collider>().gameObject.SendMessageUpwards("ChangeHealth", -damageAmount, SendMessageOptions.DontRequireReceiver);

				// The Shooter AI health system
				if (shooterAISupport)
				{
					col.transform.SendMessageUpwards("Damage", damageAmount, SendMessageOptions.DontRequireReceiver);
				}

				// Bloody Mess damage
				if (bloodyMessSupport)
				{
					//call the ApplyDamage() function on the enenmy CharacterSetup script
					if (col.gameObject.layer == LayerMask.NameToLayer("Limb"))
					{
						Vector3 directionShot = col.transform.position - transform.position;

						//  Un-comment the following section to enable Bloody Mess support
						/*
						if (col.gameObject.GetComponent<Limb>())
						{
							RaycastHit limbHit;

							if (Physics.Raycast(transform.position, directionShot, out limbHit))
							{
								if (limbHit.collider.gameObject.tag == col.gameObject.tag)
								{
									GameObject parent = col.gameObject.GetComponent<Limb>().parent;
									CharacterSetup character = parent.GetComponent<CharacterSetup>();
									character.ApplyDamage(damage, col.gameObject, weaponType, directionShot, Camera.main.transform.position);
								}
							}
						}
						*/
					}
				}


			}
		}

		// A list to hold the nearby rigidbodies
		List<Rigidbody> rigidbodies = new List<Rigidbody>();

		foreach (Collider col in cols)
		{
			// Get a list of the nearby rigidbodies
			if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody))
			{
				rigidbodies.Add(col.attachedRigidbody);
			}

			// Shake the camera if it has a vibration component
			if (shakeCamera && col.transform.GetComponentInChildren<Vibration>() != null)
			{
				float shakeViolence = 1 / (Vector3.Distance(transform.position, col.transform.position) * cameraShakeViolence);
				col.transform.GetComponentInChildren<Vibration>().StartShakingRandom(-shakeViolence, shakeViolence, -shakeViolence, shakeViolence);
			}
		}

		foreach (Rigidbody rb in rigidbodies)
		{
			rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1, ForceMode.Impulse);
		}
	}
}
