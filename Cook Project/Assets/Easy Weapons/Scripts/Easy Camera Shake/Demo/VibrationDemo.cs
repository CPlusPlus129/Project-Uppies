/// <summary>
/// Vibration demo.
/// Author: MutantGopher
/// This script demonstrates uses of the Vibration script.
/// </summary>

using UnityEngine;
using System.Collections;

public class VibrationDemo : MonoBehaviour
{
	public GameObject objectToVibrate;
	private Vibration vibration;

	private float xVibe = 0.5f;
	private float yVibe = 0.5f;
	private float zVibe = 0.5f;
	private float xRot = 0.05f;
	private float yRot = -0.05f;
	private float zRot = -0.05f;
	private float speed = 60.0f;
	private float diminish = 0.5f;
	private int numberOfShakes = 8;
	private float randomMin = -1.4f;
	private float randomMax = 1.4f;
	private float randomRotationMin = -0.2f;
	private float randomRotationMax = 0.2f;

	void Start()
	{
		vibration = objectToVibrate.GetComponent<Vibration>();
	}

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(Screen.width / 4, 10, Screen.width / 2, Screen.height));
		// User input for shake values
		GUILayout.BeginHorizontal();
		GUILayout.Label("X Vibration");
		xVibe = GUILayout.HorizontalSlider(xVibe, -2.0f, 2.0f);
		GUILayout.Label("\t" + xVibe.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Y Vibration");
		yVibe = GUILayout.HorizontalSlider(yVibe, -2.0f, 2.0f);
		GUILayout.Label("\t" + yVibe.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Z Vibration");
		zVibe = GUILayout.HorizontalSlider(zVibe, -2.0f, 2.0f);
		GUILayout.Label("\t" + zVibe.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("X Rotation");
		xRot = GUILayout.HorizontalSlider(xRot, -2.0f, 2.0f);
		GUILayout.Label("\t" + xRot.ToString("F2"));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Y Rotation");
		yRot = GUILayout.HorizontalSlider(yRot, -2.0f, 2.0f);
		GUILayout.Label("\t" + yRot.ToString("F2"));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Z Rotation");
		zRot = GUILayout.HorizontalSlider(zRot, -2.0f, 2.0f);
		GUILayout.Label("\t" + zRot.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Speed");
		speed = GUILayout.HorizontalSlider(speed, 10.0f, 150.0f);
		GUILayout.Label("\t" + speed.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Decrease Multiplier");
		diminish = GUILayout.HorizontalSlider(diminish, 0.0f, 1.0f);
		GUILayout.Label("\t" + diminish.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Number of Shakes");
		numberOfShakes = (int)GUILayout.HorizontalSlider(numberOfShakes, 1, 25);
		GUILayout.Label("\t" + numberOfShakes);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Random Position Min");
		randomMin = GUILayout.HorizontalSlider(randomMin, -2.0f, 2.0f);
		GUILayout.Label("\t" + randomMin.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Random Position Max");
		randomMax = GUILayout.HorizontalSlider(randomMax, -2.0f, 2.0f);
		GUILayout.Label("\t" + randomMax.ToString("F2"));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Random Rotation Min");
		randomRotationMin = GUILayout.HorizontalSlider(randomRotationMin, -2.0f, 2.0f);
		GUILayout.Label("\t" + randomRotationMin.ToString("F2"));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Random Rotation Max");
		randomRotationMax = GUILayout.HorizontalSlider(randomRotationMax, -2.0f, 2.0f);
		GUILayout.Label("\t" + randomRotationMax.ToString("F2"));
		GUILayout.EndHorizontal();

		// Shake and Shake Random buttons
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Shake", GUILayout.Height(50)))
		{
			vibration.StartShaking(new Vector3(xVibe, yVibe, zVibe), new Quaternion(xRot, yRot, zRot, 1), speed, diminish, numberOfShakes);
		}
		if (GUILayout.Button("Shake Random", GUILayout.Height(50)))
		{
			vibration.StartShakingRandom(randomMin, randomMax, randomRotationMin, randomRotationMax);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}
}

