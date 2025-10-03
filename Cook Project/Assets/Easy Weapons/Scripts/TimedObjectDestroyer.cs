/// <summary>
/// TimedObjectDestroyer.cs
/// Author: MutantGopher
/// This script destroys a GameObject after the number of seconds specified in
/// the lifeTime variable.  Useful for things like explosions and rockets.
/// Modified to support dynamic lifetime changes.
/// </summary>

using UnityEngine;
using System.Collections;

public class TimedObjectDestroyer : MonoBehaviour
{
	public float lifeTime = 10.0f;
	private float elapsedTime = 0f;

	// Update is called once per frame
	void Update()
	{
		elapsedTime += Time.deltaTime;
		
		// Destroy when elapsed time exceeds lifetime
		if (elapsedTime >= lifeTime)
		{
			Destroy(gameObject);
		}
	}
	
	/// <summary>
	/// Get the remaining lifetime
	/// </summary>
	public float GetRemainingLifetime()
	{
		return Mathf.Max(0, lifeTime - elapsedTime);
	}
	
	/// <summary>
	/// Get the elapsed time since creation
	/// </summary>
	public float GetElapsedTime()
	{
		return elapsedTime;
	}
}
