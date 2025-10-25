using UnityEngine;

public class SphereSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}