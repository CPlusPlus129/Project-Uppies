using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ParticleSystem))]
public class RollingMist : MonoBehaviour
{
    private const string DefaultMaterialPath = "Assets/Art/Materials/MistLit.mat";

    [Header("Mist Volume Settings")]
    [Tooltip("The size of the volume (Width, Height, Depth) where mist is generated.")]
    public Vector3 mistVolumeSize = new Vector3(20f, 1f, 20f);

    [Tooltip("Controls vertical rise/fall. Negative = Rise, Positive = Fall.")]
    [Range(-0.1f, 0.1f)]
    public float mistGravity = -0.01f;

    [Tooltip("Number of particles emitted per second.")]
    public float emissionRate = 20f;

    [Header("Particle Settings")]
    [Tooltip("Random start size of particles (Min, Max). Lower this to keep fog lower to the ground.")]
    public Vector2 particleStartSize = new Vector2(1f, 2f);

    [Tooltip("How much the particles grow over their lifetime.")]
    [Range(1f, 3f)]
    public float sizeGrowth = 1.5f;

    [Tooltip("Random lifetime of particles (Min, Max).")]
    public Vector2 particleLifetime = new Vector2(6f, 10f);

    [Tooltip("Random speed of particles (Min, Max).")]
    public Vector2 particleSpeed = new Vector2(0.2f, 0.8f);

    void Reset()
    {
        SetupMist();
    }

    void Awake()
    {
        // Always ensure maxParticleSize is high enough to prevent culling when close to camera
        GetComponent<ParticleSystemRenderer>().maxParticleSize = 10.0f;

        // Apply settings to ensure the Particle System matches the Inspector values
        ApplySettings();

        // Ensure it's set up at runtime if something went wrong or it was just added
        if (GetComponent<ParticleSystem>().particleCount == 0 && GetComponent<ParticleSystem>().emission.rateOverTime.constant == 0)
        {
             SetupMist();
        }
    }

    void OnValidate()
    {
        ApplySettings();
    }

    void ApplySettings()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = mistVolumeSize;
        // Ensure the box is aligned with the transform (Resetting rotation to make X/Y/Z intuitive)
        shape.rotation = Vector3.zero;

        var main = ps.main;
        main.gravityModifier = mistGravity;
        main.startSize = new ParticleSystem.MinMaxCurve(particleStartSize.x, particleStartSize.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime.x, particleLifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed.x, particleSpeed.y);

        var sizeOverLifetime = ps.sizeOverLifetime;
        if (sizeOverLifetime.enabled)
        {
            // Preserve the curve shape but scale it by our growth factor
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 1.0f);
            curve.AddKey(1.0f, sizeGrowth);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
        }

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;
    }

    [ContextMenu("Setup Mist")]
    public void SetupMist()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        
        // Fix: Stop system to allow modifying duration/core properties without errors
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer psr = GetComponent<ParticleSystemRenderer>();
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var colorOverLifetime = ps.colorOverLifetime;
        var sizeOverLifetime = ps.sizeOverLifetime;
        var rotationOverLifetime = ps.rotationOverLifetime;

        // Main Module
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new Color(1f, 1f, 1f, 0.3f); // Ghostly semi-transparent
        main.maxParticles = 1000;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Independent of parent movement

        // Settings applied via ApplySettings()
        // main.gravityModifier = mistGravity;
        // emission.rateOverTime = emissionRate;
        // shape.scale = mistVolumeSize;

        // Shape (Box, spreading out)
        shape.shapeType = ParticleSystemShapeType.Box;
        
        // Color Over Lifetime (Fade in and out)
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.4f, 0.2f), new GradientAlphaKey(0.4f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Size Over Lifetime (Grow as it dissipates)
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 2.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.5f, curve);

        // Rotation Over Lifetime (Slow roll)
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        // Renderer Material
#if UNITY_EDITOR
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        if (mat != null)
        {
            psr.material = mat;
        }
        else
        {
            Debug.LogWarning("RollingMist: Could not find material at " + DefaultMaterialPath);
        }
#endif
        // Enable Light Probes for efficient lighting
        psr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;

        // CRITICAL: Lit particles require Normal information to calculate lighting
        // We must enable the Normal vertex stream
        var streams = new System.Collections.Generic.List<ParticleSystemVertexStream>();
        psr.GetActiveVertexStreams(streams);
        if (!streams.Contains(ParticleSystemVertexStream.Normal))
        {
            streams.Add(ParticleSystemVertexStream.Normal);
            psr.SetActiveVertexStreams(streams);
        }

        // Sorting and Render Settings
        psr.sortMode = ParticleSystemSortMode.Distance;

        // IMPORTANT: Prevent culling when close to the camera
        psr.maxParticleSize = 10.0f; 

        // Apply our custom volume settings
        ApplySettings();
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the emission volume
        Gizmos.color = new Color(0.6f, 0.9f, 1.0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, mistVolumeSize);
        Gizmos.color = new Color(0.6f, 0.9f, 1.0f, 0.1f);
        Gizmos.DrawCube(Vector3.zero, mistVolumeSize);
    }
}
