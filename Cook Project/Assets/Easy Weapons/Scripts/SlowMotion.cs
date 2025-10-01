using UnityEngine;
using System.Collections;

public class SlowMotion : MonoBehaviour
{
	public bool enableSloMo = true;

	
	// Update is called once per frame
	void Update ()
	{
		if (enableSloMo)
		{
			if (Input.GetKey(KeyCode.Q))
			{
				Time.timeScale = 0.25f;
			}
			else
			{
				Time.timeScale = 1.0f;
			}

			Time.fixedDeltaTime = 0.02F * Time.timeScale;
		}
	}
}
