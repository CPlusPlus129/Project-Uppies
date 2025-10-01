/// <summary>
/// Spawner.cs
/// Author: MutantGopher
/// This is a sample spawning script used to spawn the red cubes in the demo scene.
/// </summary>

using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
	public GameObject prefabToSpawn;				// The prefab that should be spawned
	public float spawnFrequency = 6.0f;				// The time (in seconds) between spawns
	public bool spawnOnStart = false;				// Whether or not one instance of the prefab should be spawned on Start()
	public bool move = true;						// Move this spawn spot around
	public float moveAmount = 5.0f;					// The amount to move
	public float turnAmount = 5.0f;					// The amount to turn

	private float spawnTimer = 0.0f;

	// Use this for initialization
	void Start()
	{
		if (spawnOnStart)
		{
			Spawn();
		}
	}
	
	// Update is called once per frame
	void Update()
	{
		// Update the spawning timer
		spawnTimer += Time.deltaTime;

		// Spawn a prefab if the timer has reached spawnFrequency
		if (spawnTimer >= spawnFrequency)
		{
			// First reset the spawn timer to 0
			spawnTimer = 0.0f;
			Spawn();
		}

		// Move and turn so that boxes don't keep spawning in the same spots
		transform.Translate(0, 0, moveAmount);
		transform.Rotate(0, turnAmount, 0);
	}

	void Spawn()
	{
		// First check to see if the prefab hasn't been set
		if (prefabToSpawn != null)
		{
			// Instantiate the prefab
			Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
		}
	}
}

