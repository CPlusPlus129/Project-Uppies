using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight glow overlay that tints the emission color of every renderer under
/// this object. Designed for runtime cues (highlight until interacted, etc.).
/// </summary>
[DisallowMultipleComponent]
public class SceneObjectGlowController : MonoBehaviour
{
    [SerializeField]
    private Color glowColor = Color.cyan;

    [SerializeField]
    [Range(0f, 10f)]
    private float glowIntensity = 2.25f;

    [SerializeField]
    private bool pulse = true;

    [SerializeField]
    [Range(0.1f, 8f)]
    private float pulseSpeed = 2f;

    [SerializeField]
    private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly List<RendererState> renderers = new List<RendererState>(16);
    private float pulseTimer;
    private bool glowEnabled;

    private void Awake()
    {
        CacheRenderers();
    }

    private void Update()
    {
        if (!glowEnabled)
        {
            return;
        }

        float strength = 1f;
        if (pulse)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            strength = pulseCurve.Evaluate(Mathf.Repeat(pulseTimer, 1f));
        }

        ApplyGlow(strength);
    }

    public void Configure(Color? color = null, float? intensity = null, bool? shouldPulse = null, float? newPulseSpeed = null)
    {
        if (color.HasValue)
        {
            glowColor = color.Value;
        }

        if (intensity.HasValue)
        {
            glowIntensity = Mathf.Max(0f, intensity.Value);
        }

        if (shouldPulse.HasValue)
        {
            pulse = shouldPulse.Value;
        }

        if (newPulseSpeed.HasValue)
        {
            pulseSpeed = Mathf.Max(0.1f, newPulseSpeed.Value);
        }
    }

    public void EnableGlow()
    {
        glowEnabled = true;
        pulseTimer = 0f;
        ApplyGlow(1f);
    }

    public void DisableGlow()
    {
        glowEnabled = false;
        ApplyGlow(0f);
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        var found = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in found)
        {
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is LineRenderer)
            {
                continue;
            }

            var state = new RendererState
            {
                Renderer = renderer,
                PropertyBlock = new MaterialPropertyBlock(),
                Materials = renderer.materials,
            };

            for (int i = 0; i < state.Materials.Length; i++)
            {
                if (state.Materials[i] == null)
                {
                    continue;
                }

                state.Materials[i].EnableKeyword("_EMISSION");
                state.Materials[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            renderers.Add(state);
        }
    }

    private void ApplyGlow(float normalizedStrength)
    {
        var emissiveColor = glowColor * Mathf.Max(0f, glowIntensity * normalizedStrength);

        foreach (var state in renderers)
        {
            if (state.Renderer == null)
            {
                continue;
            }

            state.PropertyBlock ??= new MaterialPropertyBlock();
            state.PropertyBlock.SetColor(EmissionColorId, emissiveColor);
            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }
    }

    private sealed class RendererState
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
        public Material[] Materials;
    }
}
