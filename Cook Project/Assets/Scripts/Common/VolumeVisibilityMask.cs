using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Masks geometry inside a BoxCollider volume by either disabling components outright
/// or driving a clip-enabled shader that trims fragments inside the volume.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class VolumeVisibilityMask : MonoBehaviour
{
    private const int DefaultBufferSize = 64;

    private static readonly int ClipEnabledId = Shader.PropertyToID("_ClipBoxEnabled");
    private static readonly int ClipCenterId = Shader.PropertyToID("_ClipBoxCenter");
    private static readonly int ClipExtentsId = Shader.PropertyToID("_ClipBoxExtents");
    private static readonly int ClipWorldToLocalCol0Id = Shader.PropertyToID("_ClipBoxWorldToLocal_Col0");
    private static readonly int ClipWorldToLocalCol1Id = Shader.PropertyToID("_ClipBoxWorldToLocal_Col1");
    private static readonly int ClipWorldToLocalCol2Id = Shader.PropertyToID("_ClipBoxWorldToLocal_Col2");
    private static readonly int ClipWorldToLocalCol3Id = Shader.PropertyToID("_ClipBoxWorldToLocal_Col3");
    private static readonly int ClipFeatherId = Shader.PropertyToID("_ClipBoxFeather");

    public enum RendererMaskMode
    {
        DisableRenderer,
        ShaderClip
    }

    [Header("Filtering")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool includeTriggers = false;

    [Header("Colliders")]
    [SerializeField] private bool affectColliders = true;

    [Header("Renderers")]
    [SerializeField] private bool affectRenderers = true;
    [SerializeField] private RendererMaskMode rendererMode = RendererMaskMode.DisableRenderer;
    [SerializeField] private bool includeChildRenderers = true;
    [Tooltip("Applies a soft edge (in world units) when Shader Clip mode is active.")]
    [SerializeField] [Min(0f)] private float clipFeather = 0.05f;

    [Header("Performance")]
    [SerializeField] [Min(0f)] private float scanInterval = 0.1f;
    [SerializeField] [Range(1, 512)] private int maxColliders = DefaultBufferSize;

    private readonly Dictionary<Component, bool> disabledComponents = new Dictionary<Component, bool>();
    private readonly HashSet<Component> componentsInsideVolume = new HashSet<Component>();
    private readonly List<Component> componentRestoreBuffer = new List<Component>();

    private sealed class RendererState
    {
        public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        public bool IsClipping;
    }

    private readonly Dictionary<Renderer, RendererState> rendererStates = new Dictionary<Renderer, RendererState>();
    private readonly HashSet<Renderer> renderersInsideVolume = new HashSet<Renderer>();
    private readonly List<Renderer> rendererRestoreBuffer = new List<Renderer>();
    private readonly List<Renderer> rendererBuffer = new List<Renderer>(8);

    private Collider[] overlapBuffer;
    private BoxCollider boxCollider;
    private float nextScanTime;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (!boxCollider.isTrigger)
        {
            Debug.LogWarning($"VolumeVisibilityMask on {name} expects the BoxCollider to be a trigger. Setting isTrigger = true.");
            boxCollider.isTrigger = true;
        }

        AllocateBuffer();
    }

    private void OnValidate()
    {
        if (maxColliders < 1)
        {
            maxColliders = 1;
        }

        if (Application.isPlaying)
        {
            return;
        }

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        nextScanTime = Time.time;
    }

    private void Update()
    {
        if (scanInterval <= 0f || Time.time >= nextScanTime)
        {
            ScanVolume();
            nextScanTime = Time.time + scanInterval;
        }
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void OnDestroy()
    {
        RestoreAll();
    }

    private void AllocateBuffer()
    {
        if (overlapBuffer == null || overlapBuffer.Length != maxColliders)
        {
            overlapBuffer = new Collider[maxColliders];
        }
    }

    private void ScanVolume()
    {
        if (!affectColliders && !affectRenderers)
        {
            return;
        }

        AllocateBuffer();
        componentsInsideVolume.Clear();
        renderersInsideVolume.Clear();

        Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, transform.lossyScale);
        halfExtents = new Vector3(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y), Mathf.Abs(halfExtents.z));
        Quaternion orientation = transform.rotation;
        QueryTriggerInteraction triggerInteraction = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        int hitCount = Physics.OverlapBoxNonAlloc(
            worldCenter,
            halfExtents,
            overlapBuffer,
            orientation,
            targetLayers,
            triggerInteraction);

        if (hitCount == overlapBuffer.Length)
        {
            Debug.LogWarning($"VolumeVisibilityMask on {name} reached max overlap capacity ({overlapBuffer.Length}). Increase 'Max Colliders' if objects are missed.");
        }

        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
        Vector3 colliderCenter = boxCollider.center;
        Vector3 colliderExtents = boxCollider.size * 0.5f;

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapBuffer[i];
            if (other == null || other == boxCollider)
            {
                continue;
            }

            if (!includeTriggers && other.isTrigger)
            {
                continue;
            }

            if (!IsLayerIncluded(other.gameObject.layer))
            {
                continue;
            }

            if (other.transform == transform || other.transform.IsChildOf(transform))
            {
                continue;
            }

            if (affectColliders)
            {
                RegisterDisableComponent(other);
            }

            if (affectRenderers)
            {
                CollectRenderers(other.transform, worldToLocal, colliderCenter, colliderExtents);
            }
        }

        RestoreMissingComponents();
        RestoreMissingRenderers();
    }

    private void CollectRenderers(Transform root, Matrix4x4 worldToLocal, Vector3 center, Vector3 extents)
    {
        rendererBuffer.Clear();

        if (includeChildRenderers)
        {
            root.GetComponentsInChildren(includeInactive: false, rendererBuffer);
        }
        else
        {
            if (root.TryGetComponent(out Renderer renderer))
            {
                rendererBuffer.Add(renderer);
            }
        }

        if (rendererMode == RendererMaskMode.DisableRenderer)
        {
            for (int i = 0; i < rendererBuffer.Count; i++)
            {
                RegisterDisableComponent(rendererBuffer[i]);
            }
        }
        else
        {
            for (int i = 0; i < rendererBuffer.Count; i++)
            {
                ApplyRendererClip(rendererBuffer[i], worldToLocal, center, extents);
            }
        }

        rendererBuffer.Clear();
    }

    private void RegisterDisableComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (!affectColliders && component is Collider)
        {
            return;
        }

        componentsInsideVolume.Add(component);

        if (disabledComponents.ContainsKey(component))
        {
            return;
        }

        bool wasEnabled = GetEnabled(component);
        disabledComponents.Add(component, wasEnabled);

        if (wasEnabled)
        {
            SetEnabled(component, false);
        }
    }

    private void ApplyRendererClip(Renderer renderer, Matrix4x4 worldToLocal, Vector3 center, Vector3 extents)
    {
        if (renderer == null)
        {
            return;
        }

        if (renderer.transform == transform || renderer.transform.IsChildOf(transform))
        {
            return;
        }

        renderersInsideVolume.Add(renderer);

        if (!rendererStates.TryGetValue(renderer, out RendererState state))
        {
            state = new RendererState();
            rendererStates.Add(renderer, state);
        }

        Material[] sharedMaterials = renderer.sharedMaterials;
        int materialCount = sharedMaterials != null && sharedMaterials.Length > 0 ? sharedMaterials.Length : 1;

        for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
        {
            renderer.GetPropertyBlock(state.Block, materialIndex);
            state.Block.SetFloat(ClipEnabledId, 1f);
            state.Block.SetFloat(ClipFeatherId, clipFeather);
            state.Block.SetVector(ClipCenterId, new Vector4(center.x, center.y, center.z, 0f));
            state.Block.SetVector(ClipExtentsId, new Vector4(extents.x, extents.y, extents.z, 0f));
            Matrix4x4 m = worldToLocal;
            state.Block.SetVector(ClipWorldToLocalCol0Id, m.GetColumn(0));
            state.Block.SetVector(ClipWorldToLocalCol1Id, m.GetColumn(1));
            state.Block.SetVector(ClipWorldToLocalCol2Id, m.GetColumn(2));
            state.Block.SetVector(ClipWorldToLocalCol3Id, m.GetColumn(3));
            renderer.SetPropertyBlock(state.Block, materialIndex);
        }

        state.IsClipping = true;
    }

    private void RestoreMissingComponents()
    {
        componentRestoreBuffer.Clear();
        foreach (KeyValuePair<Component, bool> kvp in disabledComponents)
        {
            Component component = kvp.Key;
            if (component == null || !componentsInsideVolume.Contains(component))
            {
                componentRestoreBuffer.Add(component);
            }
        }

        for (int i = 0; i < componentRestoreBuffer.Count; i++)
        {
            RestoreComponent(componentRestoreBuffer[i]);
        }
    }

    private void RestoreMissingRenderers()
    {
        rendererRestoreBuffer.Clear();
        foreach (KeyValuePair<Renderer, RendererState> kvp in rendererStates)
        {
            Renderer renderer = kvp.Key;
            if (renderer == null || !renderersInsideVolume.Contains(renderer))
            {
                rendererRestoreBuffer.Add(renderer);
            }
        }

        for (int i = 0; i < rendererRestoreBuffer.Count; i++)
        {
            RestoreRenderer(rendererRestoreBuffer[i]);
        }
    }

    private void RestoreComponent(Component component)
    {
        if (component == null)
        {
            disabledComponents.Remove(component);
            return;
        }

        if (disabledComponents.TryGetValue(component, out bool originalState))
        {
            SetEnabled(component, originalState);
            disabledComponents.Remove(component);
        }
    }

    private void RestoreRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            rendererStates.Remove(renderer);
            return;
        }

        if (!rendererStates.TryGetValue(renderer, out RendererState state))
        {
            return;
        }

        if (!state.IsClipping)
        {
            return;
        }

        Material[] sharedMaterials = renderer.sharedMaterials;
        int materialCount = sharedMaterials != null && sharedMaterials.Length > 0 ? sharedMaterials.Length : 1;

        for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
        {
            renderer.GetPropertyBlock(state.Block, materialIndex);
            state.Block.SetFloat(ClipEnabledId, 0f);
            renderer.SetPropertyBlock(state.Block, materialIndex);
        }

        state.IsClipping = false;
    }

    private void RestoreAll()
    {
        foreach (KeyValuePair<Component, bool> kvp in disabledComponents)
        {
            SetEnabled(kvp.Key, kvp.Value);
        }

        disabledComponents.Clear();
        componentsInsideVolume.Clear();

        foreach (KeyValuePair<Renderer, RendererState> kvp in rendererStates)
        {
            Renderer renderer = kvp.Key;
            if (renderer != null)
            {
                renderer.GetPropertyBlock(kvp.Value.Block);
                kvp.Value.Block.SetFloat(ClipEnabledId, 0f);
                renderer.SetPropertyBlock(kvp.Value.Block);
                kvp.Value.IsClipping = false;
            }
        }

        renderersInsideVolume.Clear();
    }

    private bool IsLayerIncluded(int layer)
    {
        return (targetLayers.value & (1 << layer)) != 0;
    }

    private static bool GetEnabled(Component component)
    {
        switch (component)
        {
            case Behaviour behaviour:
                return behaviour.enabled;
            case Renderer renderer:
                return renderer.enabled;
            case Collider collider:
                return collider.enabled;
            default:
                return false;
        }
    }

    private static void SetEnabled(Component component, bool enabled)
    {
        switch (component)
        {
            case Behaviour behaviour:
                behaviour.enabled = enabled;
                break;
            case Renderer renderer:
                renderer.enabled = enabled;
                break;
            case Collider collider:
                collider.enabled = enabled;
                break;
        }
    }
}
