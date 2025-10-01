/// <summary>
/// TimedObjectDestroyer.cs
/// Author: MutantGopher
/// This script destroys a GameObject after the number of seconds specified in
/// the lifeTime variable.  Useful for things like explosions and rockets.
/// </summary>

using UnityEngine;
using System.Collections;

public class TimedObjectDestroyer : MonoBehaviour
{
	public float lifeTime = 10.0f;

	// Use this for initialization
	void Start()
	{
		Destroy(gameObject, lifeTime);
	}
}
