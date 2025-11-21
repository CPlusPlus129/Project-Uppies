using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// A generic controller for applying a colored pulse effect to any 3D object.
/// Uses a custom shader to displace vertices along normals, creating a clean outline pulse.
/// Supports multiple trail rings and a static halo effect.
/// </summary>
public class UniversalPulseController : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private Color pulseColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private float pulseSpeed = 1.5f; // Pulses per second
    [SerializeField] private float maxOutlineDistance = 0.2f; // Max expansion
    [SerializeField] private float intensity = 2.0f;
    [SerializeField] private float fresnelPower = 3f;
    
    [Header("Trail Settings")]
    [Range(1, 8)] public int pulseCount = 1;
    [Range(0f, 1f)] public float pulseSpacing = 0.3f;
    [Tooltip("If true, pulses fade out as they expand. If false, they stay solid.")]
    [SerializeField] private bool fadeOut = true;

    [Header("Halo Settings")]
    public bool enableStaticHalo = false;
    [Range(0f, 1f)] public float haloExpansion = 0.05f;
    public float haloIntensity = 1.0f;
    [Range(0f, 10f)] public float haloBreathingSpeed = 2.0f;

    [Header("Strobe Settings")]
    public bool enableStrobe = false;
    [Tooltip("Flashes per second")]
    public float strobeSpeed = 10.0f;
    [Range(0f, 1f)]
    public float strobeMinAlpha = 0.0f; // Opacity floor during strobe

    [Header("Visibility Settings")]
    [Tooltip("If true, the pulse will be visible through other objects (X-Ray).")]
    [SerializeField] private bool visibleThroughWalls = false;

    [Header("Debug")]
    [SerializeField] private bool previewInEditMode = false;

    // Internal State
    private List<PulseInstance> activePulses = new List<PulseInstance>();
    private PulseInstance haloInstance;
    
    private MeshFilter targetMeshFilter;
    private SkinnedMeshRenderer targetSkinnedMesh;
    
    private float globalTime;
    private Material sharedMaterial; // Single material instance shared by all pulses (avoids creating new mat per pulse)
    
    private const string SHADER_NAME = "Custom/UniversalPulse";
    
    // Shader IDs
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int FresnelPowerID = Shader.PropertyToID("_FresnelPower");
    private static readonly int ExpansionID = Shader.PropertyToID("_Expansion");
    private static readonly int MaxOutlineID = Shader.PropertyToID("_MaxOutline");
    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
    private static readonly int ZTestID = Shader.PropertyToID("_ZTest");

    private class PulseInstance
    {
        public GameObject gameObject;
        public Renderer renderer;
        public MaterialPropertyBlock propertyBlock;

        public PulseInstance(GameObject obj, Renderer r)
        {
            gameObject = obj;
            renderer = r;
            propertyBlock = new MaterialPropertyBlock();
        }
        
        public void Cleanup()
        {
            if (gameObject != null)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        SetInstancesActive(true);
    }

    private void OnDisable()
    {
        SetInstancesActive(false);
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Update()
    {
        if (sharedMaterial == null || (activePulses.Count == 0 && !enableStaticHalo))
        {
             if (!Application.isPlaying && previewInEditMode)
             {
                 // Auto-init in editor if needed
                 if (targetMeshFilter == null && targetSkinnedMesh == null) Initialize();
                 else RebuildInstances(); // Just rebuild lists if mesh is found
             }
             else if (activePulses.Count == 0 && enableStaticHalo && haloInstance == null)
             {
                 RebuildInstances();
             }
             return;
        }

        // Check for configuration changes that require rebuild
        if (activePulses.Count != pulseCount || (haloInstance == null && enableStaticHalo) || (haloInstance != null && !enableStaticHalo))
        {
            RebuildInstances();
        }

        globalTime += Time.deltaTime;

        // 1. Update Pulses
        for (int i = 0; i < activePulses.Count; i++)
        {
            UpdatePulse(activePulses[i], i);
        }

        // 2. Update Halo
        if (enableStaticHalo && haloInstance != null)
        {
            UpdateHalo(haloInstance);
        }
    }

    private void UpdatePulse(PulseInstance instance, int index)
    {
        if (instance.renderer == null) return;

        // Calculate phase
        // Offset each pulse by spacing
        // We want them to appear sequentially.
        float offset = index * pulseSpacing;
        float t = (globalTime * pulseSpeed - offset) % 1.0f;
        if (t < 0) t += 1.0f; // Handle start delay

        // Properties
        instance.renderer.GetPropertyBlock(instance.propertyBlock);
        
        instance.propertyBlock.SetColor(ColorID, pulseColor);
        instance.propertyBlock.SetFloat(IntensityID, intensity);
        instance.propertyBlock.SetFloat(FresnelPowerID, fresnelPower);
        instance.propertyBlock.SetFloat(MaxOutlineID, maxOutlineDistance);
        instance.propertyBlock.SetFloat(ZTestID, visibleThroughWalls ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
        
        // Animation
        instance.propertyBlock.SetFloat(ExpansionID, t);
        
        // Fade
        float alpha = fadeOut ? (1.0f - t) : 1.0f;
        // Optional: smooth fade in at start to avoid pop - DISABLED for visibility check
        // alpha *= Mathf.SmoothStep(0f, 0.1f, t); 

        // Apply Strobe
        if (enableStrobe)
        {
            float strobe = Mathf.PingPong(globalTime * strobeSpeed, 1.0f); // Triangle wave 0..1
            alpha *= Mathf.Lerp(strobeMinAlpha, 1.0f, strobe);
        }
        
        instance.propertyBlock.SetFloat(OpacityID, alpha);
        
        instance.renderer.SetPropertyBlock(instance.propertyBlock);
    }

    private void UpdateHalo(PulseInstance instance)
    {
        if (instance.renderer == null) return;

        instance.renderer.GetPropertyBlock(instance.propertyBlock);
        
        instance.propertyBlock.SetColor(ColorID, pulseColor);
        instance.propertyBlock.SetFloat(IntensityID, intensity * haloIntensity);
        instance.propertyBlock.SetFloat(FresnelPowerID, fresnelPower);
        instance.propertyBlock.SetFloat(MaxOutlineID, maxOutlineDistance); // Use same max ref, but override expansion
        instance.propertyBlock.SetFloat(ZTestID, visibleThroughWalls ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);

        // Static / Breathing Animation
        float breath = Mathf.Sin(globalTime * haloBreathingSpeed) * 0.5f + 0.5f; // 0 to 1
        float currentExpansion = haloExpansion; // Fixed base
        // Maybe slight breath on expansion?
        // currentExpansion += breath * 0.01f; 
        
        instance.propertyBlock.SetFloat(ExpansionID, currentExpansion);

        float alpha = 1.0f;
        
        // Apply Strobe to Halo too
        if (enableStrobe)
        {
            float strobe = Mathf.PingPong(globalTime * strobeSpeed, 1.0f);
            alpha *= Mathf.Lerp(strobeMinAlpha, 1.0f, strobe);
        }

        instance.propertyBlock.SetFloat(OpacityID, alpha); // Always visible unless strobing
        
        instance.renderer.SetPropertyBlock(instance.propertyBlock);
    }

    private void OnValidate()
    {
        // Editor-time feedback
        // We can't easily rebuild instances here safely, but we can ensure values are sane
        if (pulseCount < 1) pulseCount = 1;
    }

    [ContextMenu("Force Initialize")]
    public void Initialize()
    {
        Cleanup();

        // 1. Find the mesh
        bool foundStatic = TryFindMeshFilter(out targetMeshFilter);
        bool foundSkinned = TryFindSkinnedMesh(out targetSkinnedMesh);

        if (!foundStatic && !foundSkinned)
        {
            Debug.LogWarning($"[{nameof(UniversalPulseController)}] No valid MeshFilter or SkinnedMeshRenderer found.");
            return;
        }

        // 2. Create Material
        if (sharedMaterial == null)
        {
            Shader shader = Shader.Find(SHADER_NAME);
            if (shader == null)
            {
                Debug.LogError($"Shader {SHADER_NAME} not found");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        // 3. Build
        RebuildInstances();
    }

    private void RebuildInstances()
    {
        // Clear existing
        foreach (var p in activePulses) p.Cleanup();
        activePulses.Clear();
        if (haloInstance != null) haloInstance.Cleanup();
        haloInstance = null;

        if (targetMeshFilter == null && targetSkinnedMesh == null) return;

        // Build Pulses
        for (int i = 0; i < pulseCount; i++)
        {
            activePulses.Add(CreateInstance($"Pulse_{i}"));
        }

        // Build Halo
        if (enableStaticHalo)
        {
            haloInstance = CreateInstance("Halo");
        }
    }

    private PulseInstance CreateInstance(string suffix)
    {
        GameObject obj = new GameObject($"{name}_Effect_{suffix}");
        obj.transform.SetParent(targetSkinnedMesh ? targetSkinnedMesh.transform : (targetMeshFilter ? targetMeshFilter.transform : transform));
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.layer = gameObject.layer; // Match layer

        Renderer r = null;

        if (targetSkinnedMesh != null)
        {
            var smr = obj.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = targetSkinnedMesh.sharedMesh;
            smr.rootBone = targetSkinnedMesh.rootBone;
            smr.bones = targetSkinnedMesh.bones;
            r = smr;
        }
        else
        {
            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = targetMeshFilter.sharedMesh;
            r = obj.AddComponent<MeshRenderer>();
        }

        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.allowOcclusionWhenDynamic = false;
        r.sharedMaterial = sharedMaterial;

        return new PulseInstance(obj, r);
    }

    private void SetInstancesActive(bool active)
    {
        foreach (var p in activePulses) if (p.gameObject != null) p.gameObject.SetActive(active);
        if (haloInstance != null && haloInstance.gameObject != null) haloInstance.gameObject.SetActive(active);
    }

    private bool TryFindMeshFilter(out MeshFilter meshFilter)
    {
        meshFilter = null;
        if (TryGetComponent(out MeshFilter selfMesh)) { meshFilter = selfMesh; return true; }
        var childMesh = GetComponentInChildren<MeshFilter>();
        if (childMesh != null) { meshFilter = childMesh; return true; }
        return false;
    }

    private bool TryFindSkinnedMesh(out SkinnedMeshRenderer skinnedMesh)
    {
        skinnedMesh = null;
        if (TryGetComponent(out SkinnedMeshRenderer selfSkinned)) { skinnedMesh = selfSkinned; return true; }
        var childSkinned = GetComponentInChildren<SkinnedMeshRenderer>();
        if (childSkinned != null) { skinnedMesh = childSkinned; return true; }
        return false;
    }

    private void Cleanup()
    {
        foreach (var p in activePulses) p.Cleanup();
        activePulses.Clear();
        if (haloInstance != null) haloInstance.Cleanup();
        haloInstance = null;

        if (sharedMaterial != null)
        {
            if (Application.isPlaying) Destroy(sharedMaterial);
            else DestroyImmediate(sharedMaterial);
            sharedMaterial = null;
        }
    }
}
