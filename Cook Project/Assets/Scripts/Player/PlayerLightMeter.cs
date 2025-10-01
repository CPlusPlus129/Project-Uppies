using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlayerLightMeter : MonoBehaviour
{
    [Header("Scene refs (assign in Inspector)")]
    public Camera meterCamera;     // tiny camera (enabled, CullingMask: LightMeter)
    public Transform sampleMarker; // tiny quad/sphere on Layer: LightMeter
    public Transform sampleFrom;   // player transform (feet/head)

    [Header("Sampling")]
    [Tooltip("How many times per second to update the meter.")]
    [Range(1, 60)] public int samplesPerSecond = 20;

    [Tooltip("RT side length in pixels. Larger -> more stable, slightly more GPU cost.")]
    [Range(1, 32)] public int sampleResolution = 8;

    [Tooltip("Lock the sensor's rotation so its normal is world up (recommended for ground brightness).")]
    public bool lockMarkerUp = true;

    [Tooltip("Offset (in local space of sampleFrom) where you want to measure.")]
    public Vector3 localOffset = new Vector3(0, 0.1f, 0);

    [Tooltip("0 = no smoothing, 1 = infinitely slow. Try 0.7–0.9 for chill readings.")]
    [Range(0f, 0.98f)] public float smoothing = 0.7f;

    [Header("Read-only")]
    [SerializeField] public float lightLevel;     // Linear luminance (0..many if HDR)
    [SerializeField] public Color lastAverage;    // Average color (linear)

    RenderTexture _rt;
    float _timer;
    bool _pending; // true while a readback is in flight

    // --- Lifecycle ----------------------------------------------------------

    void Awake()
    {
        EnsureRT();
        EnsureCameraSetup();
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (meterCamera) meterCamera.targetTexture = _rt; // keep bound but harmless
        _pending = false;
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            if (meterCamera && meterCamera.targetTexture == _rt)
                meterCamera.targetTexture = null;
            _rt.Release();
            _rt = null;
        }
    }

    void Update()
    {
        // Place/lock the marker
        if (sampleFrom)
        {
            sampleMarker.position = sampleFrom.TransformPoint(localOffset);
            if (lockMarkerUp)
                sampleMarker.rotation = Quaternion.identity; // world-up facing (for a quad: ensure its normal faces +Z)
        }

        // Throttle sampling
        _timer += Time.deltaTime;
    }

    // --- Rendering & Readback ----------------------------------------------

    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != meterCamera) return;

        // Only launch a new readback at our desired rate, and if none is pending.
        if (_pending) return;

        float period = 1f / Mathf.Max(1, samplesPerSecond);
        if (_timer < period) return;
        _timer = 0f;

        // Kick async readback for the RT the meter camera just rendered into.
        _pending = true;
        AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGBA32, OnReadback);
    }

    void OnReadback(AsyncGPUReadbackRequest req)
    {
        _pending = false;
        if (req.hasError || !req.done) return;

        var data = req.GetData<Color32>();
        int count = sampleResolution * sampleResolution;

        // Average color (Linear: our RT is sRGB=false)
        double sr = 0, sg = 0, sb = 0;
        for (int i = 0; i < count; i++)
        {
            var c = data[i];
            sr += c.r; sg += c.g; sb += c.b;
        }
        float r = (float)(sr / (255.0 * count));
        float g = (float)(sg / (255.0 * count));
        float b = (float)(sb / (255.0 * count));

        // Luminance (Rec.709, linear)
        float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;

        // Optional exponential smoothing
        lightLevel = Mathf.Lerp(lum, lightLevel, smoothing);
        lastAverage = new Color(r, g, b, 1f);
    }

    // --- Helpers ------------------------------------------------------------

    void EnsureRT()
    {
        if (_rt != null && (_rt.width != sampleResolution || _rt.height != sampleResolution))
        {
            if (meterCamera && meterCamera.targetTexture == _rt) meterCamera.targetTexture = null;
            _rt.Release();
            _rt = null;
        }

        if (_rt == null)
        {
            var desc = new RenderTextureDescriptor(sampleResolution, sampleResolution, RenderTextureFormat.ARGB32)
            {
                depthBufferBits = 16,        // required by 6.2 render-graph
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false                 // keep it linear
            };
            _rt = new RenderTexture(desc) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _rt.Create();
        }
    }

    void EnsureCameraSetup()
    {
        if (!meterCamera)
        {
            Debug.LogError("PlayerLightMeter: meterCamera not assigned.");
            return;
        }

        meterCamera.enabled = true;                 // SRP renders it automatically
        meterCamera.targetTexture = _rt;
        // URP/HDRP: make sure post is disabled on this cam (we want raw lighting)
        // URP: Camera → Rendering → Post Processing OFF
        // HDRP: Custom Frame Settings → Post-processing OFF
    }

    // If you change sampleResolution at runtime:
    void OnValidate()
    {
        if (Application.isPlaying) EnsureRT();
    }
}
