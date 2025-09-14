using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;
    private float xRotation = 0f;

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x * xSensitivity * Time.deltaTime;
        float mouseY = input.y * ySensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localEulerAngles = new Vector3(xRotation, 0f, 0f);
        transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y + mouseX, 0f);
    }
}
