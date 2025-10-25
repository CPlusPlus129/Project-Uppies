using UnityEngine;

/// <summary>
/// Controller for fridge glow effects that render through walls
/// Uses custom URP shader and render feature for see-through rendering
/// </summary>
public class FridgeGlowController : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(0.5f, 1f, 0.5f, 1f);
    [SerializeField] private float glowIntensity = 3f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float glowScale = 1.1f;
    [SerializeField] private float fresnelPower = 3f;
    [SerializeField] private float pulseIntensityVariation = 0.5f;

    [Header("Material Settings")]
    [SerializeField] private Material glowMaterial;
    [SerializeField] private MeshFilter targetMesh;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private const string GLOW_LAYER_NAME = "FridgeGlow";
    private const string GLOW_SHADER_PATH = "Custom/FridgeGlow_AlwaysOnTop";
    
    // Shader property IDs (cached for performance)
    private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
    private static readonly int IntensityPropertyID = Shader.PropertyToID("_Intensity");
    private static readonly int FresnelPowerPropertyID = Shader.PropertyToID("_FresnelPower");
    private static readonly int PulseIntensityPropertyID = Shader.PropertyToID("_PulseIntensity");
    
    private GameObject glowObject;
    private Renderer glowRenderer;
    private Material glowMaterialInstance;
    private MaterialPropertyBlock propertyBlock;
    private bool isGlowing = false;
    private float pulseTimer = 0f;
    private int glowLayer = -1;

    private void Awake()
    {
        glowLayer = LayerMask.NameToLayer(GLOW_LAYER_NAME);
        
        if (glowLayer == -1)
        {
            Debug.LogError($"FridgeGlowController: Layer '{GLOW_LAYER_NAME}' not found! Create this layer in Project Settings > Tags and Layers");
            return;
        }
        
        CreateGlowObject();
    }

    private void CreateGlowObject()
    {
        MeshFilter meshFilter = targetMesh;
        if (meshFilter == null)
        {
            if (enableDebugLogs) Debug.LogWarning($"FridgeGlowController: No MeshFilter found on '{gameObject.name}'");
            return;
        }

        glowObject = new GameObject("GlowEffect");
        glowObject.transform.SetParent(meshFilter.transform);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one * glowScale;
        glowObject.layer = glowLayer;

        MeshFilter glowMeshFilter = glowObject.AddComponent<MeshFilter>();
        glowMeshFilter.mesh = meshFilter.mesh;
        glowRenderer = glowObject.AddComponent<MeshRenderer>();

        CreateGlowMaterial();
        propertyBlock = new MaterialPropertyBlock();
        glowObject.SetActive(false);

        if (enableDebugLogs) Debug.Log($"Created glow object for '{gameObject.name}' on layer '{GLOW_LAYER_NAME}'");
    }

    private void CreateGlowMaterial()
    {
        if (glowMaterial != null)
        {
            glowMaterialInstance = new Material(glowMaterial);
        }
        else
        {
            Shader glowShader = Shader.Find(GLOW_SHADER_PATH);
            
            if (glowShader != null)
            {
                glowMaterialInstance = new Material(glowShader);
            }
            else
            {
                Debug.LogError($"FridgeGlowController: Shader '{GLOW_SHADER_PATH}' not found! Check Assets/Shaders/");
                
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (fallbackShader != null)
                {
                    glowMaterialInstance = new Material(fallbackShader);
                    Debug.LogWarning("Using fallback URP Unlit shader - glow will NOT render through walls!");
                }
                return;
            }
        }

        glowMaterialInstance.SetColor(ColorPropertyID, glowColor);
        glowMaterialInstance.SetFloat(IntensityPropertyID, glowIntensity);
        glowMaterialInstance.SetFloat(FresnelPowerPropertyID, fresnelPower);
        glowMaterialInstance.SetFloat(PulseIntensityPropertyID, pulseIntensityVariation);
        glowRenderer.material = glowMaterialInstance;
    }

    private void Update()
    {
        if (!isGlowing || glowMaterialInstance == null || glowRenderer == null) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Sin(pulseTimer) * 0.5f + 0.5f;

        float minIntensity = glowIntensity * (1f - pulseIntensityVariation);
        float maxIntensity = glowIntensity * (1f + pulseIntensityVariation);
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);

        glowRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(IntensityPropertyID, currentIntensity);
        propertyBlock.SetFloat(PulseIntensityPropertyID, pulse);
        glowRenderer.SetPropertyBlock(propertyBlock);

        float scalePulse = Mathf.Lerp(glowScale * 0.98f, glowScale * 1.02f, pulse);
        glowObject.transform.localScale = Vector3.one * scalePulse;
    }

    public void StartGlowing()
    {
        if (!isGlowing && glowObject != null)
        {
            isGlowing = true;
            pulseTimer = 0f;
            glowObject.SetActive(true);

            if (enableDebugLogs)
            {
                Debug.Log($"Started glowing: {gameObject.name} (Layer: {glowObject.layer})");
            }
        }
    }

    public void StopGlowing()
    {
        if (isGlowing && glowObject != null)
        {
            isGlowing = false;
            glowObject.SetActive(false);

            if (enableDebugLogs) Debug.Log($"Stopped glowing: {gameObject.name}");
        }
    }

    public bool IsGlowing => isGlowing;

    private void OnDestroy()
    {
        if (glowMaterialInstance != null) Destroy(glowMaterialInstance);
        if (glowObject != null) Destroy(glowObject);
    }

    private void OnValidate()
    {
        if (glowMaterialInstance != null)
        {
            glowMaterialInstance.SetColor(ColorPropertyID, glowColor);
            glowMaterialInstance.SetFloat(IntensityPropertyID, glowIntensity);
            glowMaterialInstance.SetFloat(FresnelPowerPropertyID, fresnelPower);
        }
    }
}
