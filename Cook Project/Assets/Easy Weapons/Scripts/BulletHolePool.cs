using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BulletHolePool : MonoBehaviour
{
	public List<GameObject> bulletHoles = new List<GameObject>();			// A list of all the bullet holes in this pool
	public GameObject replacementBulletHole;								// A replacement bullet hole prefab to be instantiated when a bullet hole has been destroyed
	private int currentIndex = 0;											// An index to keep track of the next bullet hole in the pool that should be used
	


	// Use this for initialization
	void Start()
	{
		if (replacementBulletHole == null)
		{
			Debug.LogWarning("The Replacement Bullet Hole for " + gameObject.name + " is null.  Please set this variable in the inspector.");
			replacementBulletHole = new GameObject();
		}
	}
	
	// Update is called once per frame
	void Update()
	{
		
	}


	// Increment the current index - a method is used for this so that every time it's incremented, we also check and make sure the index hasn't yet reached number of bullet holes in the pool
	private void IncrementIndex()
	{
		// Add 1 to the index - because this one really needed to have a comment...
		currentIndex++;
		
		// If the index reaches the number of elements in the list, we want to cycle back to the beginning
		if (currentIndex >= bulletHoles.Count)
			currentIndex = 0;
	}

	// Place the next bullet hole at the specified position and rotation
	public void PlaceBulletHole(Vector3 pos, Quaternion rot)
	{
		// Make sure the current bullet hole still exists
		VerifyBulletHole();

		// Now the bullet hole is ready to be re-positioned

		// Start by clearing the parent.  This prevents problems with the transform inherited from previous parents when the bullet hole GameObject is re-parented
		bulletHoles[currentIndex].transform.parent = null;

		// Now set the position and rotation of the bullet hole
		bulletHoles[currentIndex].transform.position = pos;
		bulletHoles[currentIndex].transform.rotation = rot;
		bulletHoles[currentIndex].transform.localScale = bulletHoles[currentIndex].transform.localScale;

		// Now refresh the bullet hole so it can be re-parented, etc.
		if (bulletHoles[currentIndex].GetComponent<BulletHole>() == null)
			bulletHoles[currentIndex].AddComponent<BulletHole>();
		bulletHoles[currentIndex].GetComponent<BulletHole>().Refresh();

		// Now increment our index so the oldest bullet holes will always be the first to be re-used
		IncrementIndex();
	}

	// Verify that the specified bullet hole still exists
	private void VerifyBulletHole()
	{
		// If the bullet hole at the current index has been destroyed, instantiate a new one
		if (bulletHoles[currentIndex] == null)
		{
			GameObject bh = Instantiate(replacementBulletHole, transform.position, transform.rotation) as GameObject;
			bulletHoles[currentIndex] = bh;
		}
	}

}

