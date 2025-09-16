using UnityEngine;

public class WorldPosFollowUI : MonoBehaviour
{
    public Transform target; // The world position to follow
    public Vector3 offset; // Offset from the target position
    private RectTransform rectTransform;
    private Camera mainCamera;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }
    private void Update()
    {
        if (target != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position + offset);
            rectTransform.position = screenPos;
        }
    }
}