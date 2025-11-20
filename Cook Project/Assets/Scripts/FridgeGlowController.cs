using System.Collections;
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
    private Coroutine glowDurationRoutine;

    private void Awake()
    {
        glowLayer = LayerMask.NameToLayer(GLOW_LAYER_NAME);

        if (glowLayer == -1)
        {
            Debug.LogError($"FridgeGlowController: Layer '{GLOW_LAYER_NAME}' not found! Create this layer in Project Settings > Tags and Layers");
            return;
        }

        if (!TryResolveTargetMesh())
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"FridgeGlowController: No MeshFilter assigned or found for '{gameObject.name}' – glow will stay disabled");
            }
            return;
        }

        CreateGlowObject();
    }

    private bool TryResolveTargetMesh()
    {
        if (targetMesh != null)
        {
            return true;
        }

        MeshFilter bestMatch = null;
        float bestScore = -1f;

        // Check self first
        if (TryGetComponent(out MeshFilter selfMeshFilter) && HasValidMesh(selfMeshFilter) && !IsTextMeshFilter(selfMeshFilter))
        {
            bestMatch = selfMeshFilter;
            bestScore = GetMeshWeight(selfMeshFilter);
        }

        // Check children and see if we find something better
        MeshFilter[] childMeshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in childMeshFilters)
        {
            // Skip self if we already checked it (GetComponentsInChildren includes self)
            if (meshFilter == selfMeshFilter) continue;

            if (!HasValidMesh(meshFilter) || IsTextMeshFilter(meshFilter))
            {
                continue;
            }

            float score = GetMeshWeight(meshFilter);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = meshFilter;
            }
        }

        if (bestMatch != null)
        {
            targetMesh = bestMatch;
            if (enableDebugLogs)
            {
                Debug.Log($"FridgeGlowController: Auto-assigned MeshFilter '{bestMatch.name}' on '{gameObject.name}' with score {bestScore}");
            }
            return true;
        }

        // Fall back to any available MeshFilter so legacy behaviour still works if only text meshes are present.
        foreach (MeshFilter meshFilter in childMeshFilters)
        {
            if (!HasValidMesh(meshFilter))
            {
                continue;
            }

            targetMesh = meshFilter;
            if (enableDebugLogs)
            {
                Debug.LogWarning($"FridgeGlowController: Only text meshes found, using '{meshFilter.name}' on '{gameObject.name}'");
            }
            return true;
        }

        return false;
    }

    private bool HasValidMesh(MeshFilter meshFilter)
    {
        return meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount > 0;
    }

    private bool IsTextMeshFilter(MeshFilter meshFilter)
    {
        if (meshFilter == null)
        {
            return false;
        }

        if (meshFilter.GetComponent<TMPro.TMP_SubMesh>() != null || meshFilter.GetComponent<TMPro.TMP_SubMeshUI>() != null)
        {
            return true;
        }

        TMPro.TMP_Text parentText = meshFilter.GetComponentInParent<TMPro.TMP_Text>(true);
        return parentText != null;
    }

    private float GetMeshWeight(MeshFilter meshFilter)
    {
        return meshFilter.sharedMesh != null ? meshFilter.sharedMesh.vertexCount : 0f;
    }

    private void CreateGlowObject()
    {
        if (glowObject != null)
        {
            return;
        }

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
        // Fix: Disable occlusion culling so it renders through walls even if the occlusion system thinks it's hidden.
        glowRenderer.allowOcclusionWhenDynamic = false; 
        glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;

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

        ApplyMaterialOverrides();
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

    public void ConfigureGlow(
        Color? color = null,
        float? intensity = null,
        float? newPulseSpeed = null,
        float? newScale = null,
        float? newFresnelPower = null,
        float? newPulseVariation = null)
    {
        if (color.HasValue)
        {
            glowColor = color.Value;
        }

        if (intensity.HasValue)
        {
            glowIntensity = Mathf.Max(0f, intensity.Value);
        }

        if (newPulseSpeed.HasValue)
        {
            pulseSpeed = Mathf.Max(0f, newPulseSpeed.Value);
        }

        if (newScale.HasValue)
        {
            glowScale = Mathf.Max(0.01f, newScale.Value);
        }

        if (newFresnelPower.HasValue)
        {
            fresnelPower = Mathf.Max(0.01f, newFresnelPower.Value);
        }

        if (newPulseVariation.HasValue)
        {
            pulseIntensityVariation = Mathf.Max(0f, newPulseVariation.Value);
        }

        ApplyMaterialOverrides();
    }

    public void SetTargetMesh(MeshFilter newMesh)
    {
        if (newMesh == null) return;
        
        targetMesh = newMesh;
        
        if (glowObject != null)
        {
            if (Application.isPlaying) Destroy(glowObject);
            else DestroyImmediate(glowObject);
            glowObject = null;
        }

        if (glowMaterialInstance != null)
        {
            if (Application.isPlaying) Destroy(glowMaterialInstance);
            else DestroyImmediate(glowMaterialInstance);
            glowMaterialInstance = null;
        }

        CreateGlowObject();
        
        if (isGlowing && glowObject != null)
        {
            glowObject.SetActive(true);
        }
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
            CancelGlowDurationRoutine();

            if (enableDebugLogs) Debug.Log($"Stopped glowing: {gameObject.name}");
        }
    }

    public void EnableGlow()
    {
        StartGlowing();
    }

    public void DisableGlow()
    {
        StopGlowing();
    }

    public void EnableGlowForDuration(float seconds)
    {
        if (seconds <= 0f)
        {
            EnableGlow();
            return;
        }

        CancelGlowDurationRoutine();
        glowDurationRoutine = StartCoroutine(GlowDurationCoroutine(seconds));
    }

    private IEnumerator GlowDurationCoroutine(float seconds)
    {
        EnableGlow();
        yield return new WaitForSeconds(seconds);
        glowDurationRoutine = null;
        DisableGlow();
    }

    private void CancelGlowDurationRoutine()
    {
        if (glowDurationRoutine != null)
        {
            StopCoroutine(glowDurationRoutine);
            glowDurationRoutine = null;
        }
    }

    public bool IsGlowing => isGlowing;

    private void OnDisable()
    {
        CancelGlowDurationRoutine();
    }

    private void OnDestroy()
    {
        CancelGlowDurationRoutine();
        if (glowMaterialInstance != null) Destroy(glowMaterialInstance);
        if (glowObject != null) Destroy(glowObject);
    }

    private void OnValidate()
    {
        ApplyMaterialOverrides();
    }

    private void ApplyMaterialOverrides()
    {
        if (glowMaterialInstance != null)
        {
            glowMaterialInstance.SetColor(ColorPropertyID, glowColor);
            glowMaterialInstance.SetFloat(IntensityPropertyID, glowIntensity);
            glowMaterialInstance.SetFloat(FresnelPowerPropertyID, fresnelPower);
            glowMaterialInstance.SetFloat(PulseIntensityPropertyID, pulseIntensityVariation);
        }

        if (glowObject != null)
        {
            glowObject.transform.localScale = Vector3.one * glowScale;
        }
    }
}
