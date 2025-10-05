using UnityEngine;

/// <summary>
/// Creates a simple visible circle showing the light's radius on the ground.
/// Always horizontal, positioned below the light.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightRadiusIndicator : MonoBehaviour
{
    [Header("Circle Settings")]
    [SerializeField] private bool showIndicator = true;
    [SerializeField] private Color circleColor = new Color(1f, 0.9f, 0.5f, 0.8f);
    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private int segments = 64;
    [SerializeField] private float heightOffset = 0.01f;
    
    [Header("Animation")]
    [SerializeField] private bool pulseEffect = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    [Header("Advanced")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float raycastDistance = 50f;
    
    private Light lightSource;
    private LineRenderer lineRenderer;
    private GameObject circleObj;
    
    private void Start()
    {
        lightSource = GetComponent<Light>();
        
        if (showIndicator)
        {
            CreateCircleIndicator();
        }
    }
    
    private void Update()
    {
        if (showIndicator && lineRenderer != null && lightSource != null)
        {
            UpdateCirclePosition();
            
            // Calculate radius with optional pulse
            float currentRadius = lightSource.range;
            if (pulseEffect)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                currentRadius += pulse;
            }
            
            UpdateCirclePoints(currentRadius);
            
            // Update color to match light
            Color indicatorColor = circleColor;
            if (lightSource.color != Color.white)
            {
                indicatorColor = Color.Lerp(circleColor, lightSource.color, 0.5f);
                indicatorColor.a = circleColor.a;
            }
            lineRenderer.startColor = indicatorColor;
            lineRenderer.endColor = indicatorColor;
        }
    }
    
    private void CreateCircleIndicator()
    {
        circleObj = new GameObject("LightRadiusCircle");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;
        
        // Always horizontal (facing down)
        circleObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        lineRenderer = circleObj.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = segments;
        
        // Disable all lighting interactions
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        
        // Create unlit material
        Material lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineMaterial.color = circleColor;
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = circleColor;
        lineRenderer.endColor = circleColor;
        
        UpdateCirclePosition();
    }
    
    private void UpdateCirclePosition()
    {
        if (circleObj == null) return;
        
        // Simple downward raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            // Position slightly above ground
            circleObj.transform.position = hit.point + Vector3.up * heightOffset;
        }
        else
        {
            // No ground found, position below light
            circleObj.transform.position = transform.position + Vector3.down * 2f;
        }
    }
    
    private void UpdateCirclePoints(float radius)
    {
        if (lineRenderer == null) return;
        
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
        }
    }
    
    private void OnDestroy()
    {
        if (circleObj != null) Destroy(circleObj);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (lightSource == null)
            lightSource = GetComponent<Light>();
        
        if (lightSource != null && showIndicator)
        {
            Gizmos.color = new Color(circleColor.r, circleColor.g, circleColor.b, 0.3f);
            
            // Draw gizmo circle around light
            for (int i = 0; i < 32; i++)
            {
                float angle1 = i * (360f / 32f) * Mathf.Deg2Rad;
                float angle2 = (i + 1) * (360f / 32f) * Mathf.Deg2Rad;
                
                Vector3 offset1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * lightSource.range;
                Vector3 offset2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * lightSource.range;
                
                Gizmos.DrawLine(transform.position + offset1, transform.position + offset2);
            }
        }
    }
}
