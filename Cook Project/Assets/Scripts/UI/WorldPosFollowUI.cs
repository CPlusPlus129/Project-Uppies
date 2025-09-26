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
            Vector3 worldPos = target.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0)
            {
                rectTransform.position = screenPos;
                rectTransform.gameObject.SetActive(true);
            }
            else
            {
                rectTransform.gameObject.SetActive(false);
            }
        }
    }
}