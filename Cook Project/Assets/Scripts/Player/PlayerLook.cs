using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xSensitivity = 0.5f;
    public float ySensitivity = 0.5f;
    private float xRotation = 0f;

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x * xSensitivity;
        float mouseY = input.y * ySensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localEulerAngles = new Vector3(xRotation, 0f, 0f);
        transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y + mouseX, 0f);
    }
}
