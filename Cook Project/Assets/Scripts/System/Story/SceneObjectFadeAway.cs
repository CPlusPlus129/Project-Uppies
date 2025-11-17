using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple helper that linearly fades the materials on a hierarchy of renderers
/// and optionally destroys the GameObject once the fade completes.
/// Attach this to BabyBossReturns (or any prefab) and wire UnityEvents to the
/// public FadeAway() method to start the effect.
/// </summary>
[DisallowMultipleComponent]
public class SceneObjectFadeAway : MonoBehaviour
{
    private const string ColorProperty = "_Color";

    [SerializeField]
    [Tooltip("Seconds it takes to fade from fully visible to invisible.")]
    private float fadeDuration = 1.5f;

    [SerializeField]
    [Tooltip("Curve applied to the fade progress (x = normalized time, y = opacity).")]
    private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [SerializeField]
    [Tooltip("Disable all colliders once the fade begins so players can't interact with the VIP.")]
    private bool disableCollidersOnFade = true;

    [SerializeField]
    [Tooltip("Destroy the root GameObject as soon as the fade finishes.")]
    private bool destroyOnComplete = true;

    [SerializeField]
    [Tooltip("Optional extra renderers to include (others are auto-discovered in children).")]
    private List<Renderer> additionalRenderers = new List<Renderer>();

    private readonly List<RendererEntry> renderers = new List<RendererEntry>(16);
    private readonly List<Collider> colliders = new List<Collider>(8);
    private MaterialPropertyBlock propertyBlock;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        CacheRenderers();
        CacheColliders();
    }

    /// <summary>
    /// Begins the fade (safe to call multiple times; it restarts the effect).
    /// </summary>
    public void FadeAway()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        if (disableCollidersOnFade)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        float elapsed = 0f;
        fadeDuration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / fadeDuration);
            var opacity = Mathf.Clamp01(fadeCurve.Evaluate(t));
            ApplyOpacity(opacity);
            yield return null;
        }

        ApplyOpacity(0f);
        fadeRoutine = null;

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyOpacity(float opacity)
    {
        propertyBlock ??= new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Count; i++)
        {
            var entry = renderers[i];
            if (entry.Renderer == null)
            {
                continue;
            }

            var color = entry.BaseColor;
            color.a *= opacity;
            propertyBlock.SetColor(ColorProperty, color);
            entry.Renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        propertyBlock ??= new MaterialPropertyBlock();

        var found = new List<Renderer>();
        GetComponentsInChildren(true, found);

        if (additionalRenderers != null)
        {
            foreach (var extra in additionalRenderers)
            {
                if (extra != null && !found.Contains(extra))
                {
                    found.Add(extra);
                }
            }
        }

        foreach (var renderer in found)
        {
            if (renderer == null)
            {
                continue;
            }

            var baseColor = Color.white;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(ColorProperty))
            {
                baseColor = renderer.sharedMaterial.color;
            }

            renderers.Add(new RendererEntry
            {
                Renderer = renderer,
                BaseColor = baseColor
            });
        }
    }

    private void CacheColliders()
    {
        colliders.Clear();
        GetComponentsInChildren(true, colliders);
    }

    private struct RendererEntry
    {
        public Renderer Renderer;
        public Color BaseColor;
    }
}
