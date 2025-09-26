/// <summary>
/// Projectile.cs
/// Author: MutantGopher
/// Attach this script to your projectile prefabs.  This includes rockets, missiles,
/// mortars, grenade launchers, and a number of other weapons.  This script handles
/// features like seeking missiles and the instantiation of explosions on impact.
/// </summary>

using UnityEngine;
using System.Collections;

public enum ProjectileType
{
	Standard,
	Seeker,
	ClusterBomb
}
public enum DamageType
{
	Direct,
	Explosionawe
}

public class Projectile : MonoBehaviour
{
	public ProjectileType projectileType = ProjectileType.Standard;		// The type of projectile - Standard is a straight forward moving projectile, Seeker type seeks GameObjects with a specified tag
	public DamageType damageType = DamageType.Direct;					// The damage type - Direct applys damage directly from the projectile, Explosion lets an instantiated explosion handle damage
	public float damage = 100.0f;										// The amount of damage to be applied (only for Direct damage type)
	public float speed = 10.0f;											// The speed at which this projectile will move
	public float initialForce = 1000.0f;								// The force to be applied to the projectile initially
	public float lifetime = 30.0f;										// The maximum time (in seconds) before the projectile is destroyed
	
	public float seekRate = 1.0f;										// The rate at which the projectile will turn to seek enemies
	public string seekTag = "Enemy";									// The projectile will seek gameobjects with this tag
	public GameObject explosion;										// The explosion to be instantiated when this projectile hits something
	public float targetListUpdateRate = 1.0f;							// The rate at which the projectile will update its list of all enemies to target

	public GameObject clusterBomb;										// The array of bombs to be instantiated on explode if this projectile is of clusterbomb type
	public int clusterBombNum = 6;										// The number of cluster bombs to instantiate

	public int weaponType = 0;											// Bloody Mess support variable

	private float lifeTimer = 0.0f;										// The timer to keep track of how long this projectile has been in existence
	private float targetListUpdateTimer = 0.0f;							// The timer to keep track of how long it's been since the enemy list was last updated
	private GameObject[] enemyList;										// An array to hold possible targets


	void Start()
	{
		// Initialize the enemy list
		UpdateEnemyList();

		// Add the initial force to rigidbody
		GetComponent<Rigidbody>().AddRelativeForce(0, 0, initialForce);
	}

	// Update is called once per frame
	void Update()
	{
		// Update the timer
		lifeTimer += Time.deltaTime;

		// Destroy the projectile if the time is up
		if (lifeTimer >= lifetime)
		{
			Explode(transform.position);
		}

		// Make the projectile move
		if (initialForce == 0)		// Only if initial force is not being used to propel this projectile
			GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;

		// Make the projectile seek nearby targets if the projectile type is set to seeker
		if (projectileType == ProjectileType.Seeker)
		{
			// Keep the timer updating
			targetListUpdateTimer += Time.deltaTime;

			// If the targetListUpdateTimer has reached the targetListUpdateRate, update the enemy list and restart the timer
			if (targetListUpdateTimer >= targetListUpdateRate)
			{
				UpdateEnemyList();
			}

			if (enemyList != null)
			{
				// Choose a target to "seek" or rotate toward
				float greatestDotSoFar = -1.0f;
				Vector3 target = transform.forward * 1000;
				foreach (GameObject enemy in enemyList)
				{
					if (enemy != null)
					{
						Vector3 direction = enemy.transform.position - transform.position;
						float dot = Vector3.Dot(direction.normalized, transform.forward);
						if (dot > greatestDotSoFar)
						{
							target = enemy.transform.position;
							greatestDotSoFar = dot;
						}
					}
				}

				// Rotate the projectile to look at the target
				Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);
				transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * seekRate);
			}
		}
	}

	void UpdateEnemyList()
	{
		enemyList = GameObject.FindGameObjectsWithTag(seekTag);
		targetListUpdateTimer = 0.0f;
	}

	void OnCollisionEnter(Collision col)
	{
		// If the projectile collides with something, call the Hit() function
		Hit(col);
	}

	void Hit(Collision col)
	{
		// Make the projectile explode
		Explode(col.contacts[0].point);

		// Apply damage to the hit object if damageType is set to Direct
		if (damageType == DamageType.Direct)
		{
			col.collider.gameObject.SendMessageUpwards("ChangeHealth", -damage, SendMessageOptions.DontRequireReceiver);

			//call the ApplyDamage() function on the enenmy CharacterSetup script
			if (col.collider.gameObject.layer == LayerMask.NameToLayer("Limb"))
			{
				Vector3 directionShot = col.collider.transform.position - transform.position;

				// Un-comment the following section for Bloody Mess support
				/*
				if (col.collider.gameObject.GetComponent<Limb>())
				{
					GameObject parent = col.collider.gameObject.GetComponent<Limb>().parent;
					CharacterSetup character = parent.GetComponent<CharacterSetup>();
					character.ApplyDamage(damage, col.collider.gameObject, weaponType, directionShot, Camera.main.transform.position);
				}
				*/
			}
		}
	}

	void Explode(Vector3 position)
	{
		// Instantiate the explosion
		if (explosion != null)
		{
			Instantiate(explosion, position, Quaternion.identity);
		}

		// Cluster bombs
		if (projectileType == ProjectileType.ClusterBomb)
		{
			if (clusterBomb != null)
			{
				for (int i = 0; i <= clusterBombNum; i++)
				{
					Instantiate(clusterBomb, transform.position, transform.rotation);
				}
			}
		}

		// Destroy this projectile
		Destroy(gameObject);
	}

	// Modify the damage that this projectile can cause
	public void MultiplyDamage(float amount)
	{
		damage *= amount;
	}

	// Modify the inital force
	public void MultiplyInitialForce(float amount)
	{
		initialForce *= amount;
	}
}

