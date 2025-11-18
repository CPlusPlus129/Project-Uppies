using UnityEngine;
public class FirePillarRotation : MonoBehaviour
{
    [Header("Rotate Speed")]
    public float rotateSpeed = 30f;

    bool isRotating = true;

    void Update()
    {
        if (isRotating)
        {
            RotateOnce();
        }
    }

    public void RotateOnce()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }

    public void StartRotate()
    {
        isRotating = true;
    }

    public void StopRotate()
    {
        isRotating = false;
    }
}
