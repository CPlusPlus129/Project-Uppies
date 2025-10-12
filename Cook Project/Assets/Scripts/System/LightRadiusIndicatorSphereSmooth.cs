using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates a smooth, translucent 3D sphere showing the light's radius.
/// When multiple spheres overlap, they blend together to appear as one continuous volume.
/// Perfect for visualizing overlapping explosion lights without visual clutter.
/// Supports Additive, AlphaBlend, and Metaball blend modes.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightRadiusIndicatorSphereSmooth : MonoBehaviour
{
    [Header("Sphere Settings")]
    [SerializeField] private bool showIndicator = true;
    [SerializeField] private Color sphereColor = new Color(1f, 0.9f, 0.5f, 0.3f);
    [SerializeField] [Range(1, 3)] private int subdivisions = 2;
    [Tooltip("How pronounced the rim/edge glow is")]
    [SerializeField] [Range(0.5f, 8f)] private float rimPower = 3.0f;
    [Tooltip("Brightness of the rim glow")]
    [SerializeField] [Range(0f, 2f)] private float rimIntensity = 1.0f;
    
    [Header("Blending")]
    [SerializeField] private BlendMode blendMode = BlendMode.Metaball;
    
    public enum BlendMode
    {
        Additive,       // Overlaps add together (brighter, glowing effect)
        AlphaBlend,     // Standard transparency (darker when overlapping)
        Metaball        // Smooth blob merging - creates continuous volume when overlapping
    }
    
    [Header("Metaball Settings")]
    [Tooltip("Distance over which spheres fade when near geometry (requires depth texture)")]
    [SerializeField] [Range(0f, 5f)] private float depthFade = 1.0f;
    [Tooltip("How much the effect concentrates toward the center (higher = tighter blob)")]
    [SerializeField] [Range(0.1f, 2f)] private float centerFalloff = 0.8f;
    [Tooltip("Threshold for blob visibility (higher = less visible, cleaner merging)")]
    [SerializeField] [Range(0f, 1f)] private float blobThreshold = 0.5f;
    [Tooltip("Maximum brightness when many spheres overlap (prevents over-saturation)")]
    [SerializeField] [Range(0.1f, 2f)] private float maxBrightness = 1.0f;
    
    [Header("Animation")]
    [SerializeField] private bool pulseEffect = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    [Header("Advanced")]
    [SerializeField] private bool matchLightColor = true;
    [SerializeField] [Range(0f, 1f)] private float lightColorInfluence = 0.5f;
    
    private Light lightSource;
    private GameObject sphereObj;
    private MeshRenderer sphereRenderer;
    private MeshFilter sphereFilter;
    private Material sphereMaterial;
    private Mesh sphereMesh;
    
    private static Shader translucentShader;
    
    private void Start()
    {
        lightSource = GetComponent<Light>();
        
        if (showIndicator)
        {
            CreateSphereIndicator();
        }
    }
    
    private void Update()
    {
        if (!showIndicator || sphereObj == null || lightSource == null)
            return;
        
        // Calculate radius with optional pulse
        float currentRadius = lightSource.range;
        if (pulseEffect)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            currentRadius += pulse;
        }
        
        // Update scale to match light radius
        sphereObj.transform.localScale = Vector3.one * currentRadius * 2f;
        
        // Update color and metaball properties
        if (sphereMaterial != null)
        {
            Color indicatorColor = sphereColor;
            
            if (matchLightColor && lightSource.color != Color.white)
            {
                indicatorColor = Color.Lerp(sphereColor, lightSource.color, lightColorInfluence);
                indicatorColor.a = sphereColor.a;
            }
            
            sphereMaterial.SetColor("_Color", indicatorColor);
            
            // Update metaball-specific properties
            if (blendMode == BlendMode.Metaball)
            {
                sphereMaterial.SetFloat("_DepthFade", depthFade);
                sphereMaterial.SetFloat("_CenterFalloff", centerFalloff);
                sphereMaterial.SetFloat("_BlobThreshold", blobThreshold);
                sphereMaterial.SetFloat("_MaxBrightness", maxBrightness);
            }
        }
    }
    
    private void CreateSphereIndicator()
    {
        // Load shader
        if (translucentShader == null)
        {
            translucentShader = Shader.Find("Custom/LightIndicatorTranslucent");
            if (translucentShader == null)
            {
                Debug.LogError("LightRadiusIndicatorSphereSmooth: Could not find Custom/LightIndicatorTranslucent shader!");
                return;
            }
        }
        
        // Create sphere object
        sphereObj = new GameObject("LightRadiusSphereSmooth");
        sphereObj.transform.SetParent(transform);
        sphereObj.transform.localPosition = Vector3.zero;
        sphereObj.transform.localRotation = Quaternion.identity;
        
        // Add mesh components
        sphereFilter = sphereObj.AddComponent<MeshFilter>();
        sphereRenderer = sphereObj.AddComponent<MeshRenderer>();
        
        // Generate smooth icosphere mesh (higher subdivision for smooth appearance)
        sphereMesh = IcosphereGenerator.Create(0.5f, subdivisions);
        sphereFilter.mesh = sphereMesh;
        
        // Create material
        sphereMaterial = new Material(translucentShader);
        sphereMaterial.SetColor("_Color", sphereColor);
        sphereMaterial.SetFloat("_RimPower", rimPower);
        sphereMaterial.SetFloat("_RimIntensity", rimIntensity);
        
        // Set blend mode
        UpdateBlendMode();
        
        sphereRenderer.material = sphereMaterial;
        sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphereRenderer.receiveShadows = false;
        sphereRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        sphereRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        
        // Set initial scale
        float currentRadius = lightSource.range;
        sphereObj.transform.localScale = Vector3.one * currentRadius * 2f;
    }
    
    private void UpdateBlendMode()
    {
        if (sphereMaterial == null) return;
        
        switch (blendMode)
        {
            case BlendMode.Additive:
                // Additive blending: Blend SrcAlpha One
                sphereMaterial.SetFloat("_BlendMode", 1f);
                sphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                sphereMaterial.renderQueue = 3000;
                break;
                
            case BlendMode.AlphaBlend:
                // Alpha blending: Blend SrcAlpha OneMinusSrcAlpha
                sphereMaterial.SetFloat("_BlendMode", 10f);
                sphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                sphereMaterial.renderQueue = 3001;
                break;
                
            case BlendMode.Metaball:
                // Metaball blending: Blend One OneMinusSrcAlpha (with brightness ceiling)
                sphereMaterial.SetFloat("_BlendMode", 20f);
                sphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                sphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                sphereMaterial.renderQueue = 3000;
                
                // Set metaball-specific properties
                sphereMaterial.SetFloat("_DepthFade", depthFade);
                sphereMaterial.SetFloat("_CenterFalloff", centerFalloff);
                sphereMaterial.SetFloat("_BlobThreshold", blobThreshold);
                sphereMaterial.SetFloat("_MaxBrightness", maxBrightness);
                break;
        }
    }
    
    private void OnDestroy()
    {
        if (sphereObj != null)
        {
            Destroy(sphereObj);
        }
        
        if (sphereMaterial != null)
        {
            Destroy(sphereMaterial);
        }
        
        if (sphereMesh != null)
        {
            Destroy(sphereMesh);
        }
    }
    
    private void OnValidate()
    {
        // Update material properties when changed in inspector
        if (Application.isPlaying && sphereMaterial != null)
        {
            sphereMaterial.SetFloat("_RimPower", rimPower);
            sphereMaterial.SetFloat("_RimIntensity", rimIntensity);
            
            Color indicatorColor = sphereColor;
            if (matchLightColor && lightSource != null && lightSource.color != Color.white)
            {
                indicatorColor = Color.Lerp(sphereColor, lightSource.color, lightColorInfluence);
                indicatorColor.a = sphereColor.a;
            }
            sphereMaterial.SetColor("_Color", indicatorColor);
            
            // Update blend mode and metaball properties
            UpdateBlendMode();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (lightSource == null)
            lightSource = GetComponent<Light>();
        
        if (lightSource != null && showIndicator)
        {
            Gizmos.color = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lightSource.range);
        }
    }
    
    /// <summary>
    /// Toggle indicator visibility at runtime
    /// </summary>
    public void SetVisible(bool visible)
    {
        showIndicator = visible;
        
        if (sphereObj != null)
        {
            sphereObj.SetActive(visible);
        }
        else if (visible)
        {
            CreateSphereIndicator();
        }
    }
    
    /// <summary>
    /// Update sphere color at runtime
    /// </summary>
    public void SetColor(Color color)
    {
        sphereColor = color;
        if (sphereMaterial != null)
        {
            sphereMaterial.SetColor("_Color", color);
        }
    }
    
    /// <summary>
    /// Update blend mode at runtime
    /// </summary>
    public void SetBlendMode(BlendMode mode)
    {
        blendMode = mode;
        UpdateBlendMode();
    }
    
    /// <summary>
    /// Update metaball parameters at runtime
    /// </summary>
    public void SetMetaballParameters(float newDepthFade, float newCenterFalloff, float newBlobThreshold, float newMaxBrightness)
    {
        depthFade = newDepthFade;
        centerFalloff = newCenterFalloff;
        blobThreshold = newBlobThreshold;
        maxBrightness = newMaxBrightness;
        
        if (sphereMaterial != null && blendMode == BlendMode.Metaball)
        {
            sphereMaterial.SetFloat("_DepthFade", depthFade);
            sphereMaterial.SetFloat("_CenterFalloff", centerFalloff);
            sphereMaterial.SetFloat("_BlobThreshold", blobThreshold);
            sphereMaterial.SetFloat("_MaxBrightness", maxBrightness);
        }
    }
    
    /// <summary>
    /// Set maximum brightness ceiling for metaball mode
    /// </summary>
    public void SetMaxBrightness(float brightness)
    {
        maxBrightness = brightness;
        if (sphereMaterial != null && blendMode == BlendMode.Metaball)
        {
            sphereMaterial.SetFloat("_MaxBrightness", maxBrightness);
        }
    }
}
