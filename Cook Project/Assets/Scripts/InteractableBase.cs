using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all interactable objects in the game.
/// Provides outline functionality and implements IInteractable interface.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interactable")]
    [SerializeField, Tooltip("Enable outline effect when player looks at this interactable")]
    protected bool useOutline = true;
    [SerializeField, Tooltip("The renderers show outline when player looks at this interactable")]
    protected List<Renderer> targetRenderers;

    private const uint OUTLINE_LAYER_MASK = 1u << 8; // Layer 8 for outline rendering

    protected virtual void Awake()
    {
        if (useOutline)
        {
            if (targetRenderers == null)
            {
                targetRenderers = new List<Renderer>();
            }

            if (targetRenderers.Count == 0)
            {
                var rArr = GetComponentsInChildren<Renderer>();
                targetRenderers.AddRange(rArr);
            }

            if (targetRenderers.Count == 0)
            {
                Debug.LogWarning($"[InteractableBase] {gameObject.name} has useOutline enabled but no Renderer component found.", this);
            }
        }
    }

    /// <summary>
    /// Called when player interacts with this object. Must be implemented by derived classes.
    /// </summary>
    public abstract void Interact();

    /// <summary>
    /// Toggles the outline effect on/off by manipulating the rendering layer mask.
    /// </summary>
    /// <param name="isOn">True to show outline, false to hide</param>
    public virtual void ToggleOutline(bool isOn)
    {
        if (!useOutline || targetRenderers.Count == 0)
            return;

        if (isOn)
        {
            foreach (var r in targetRenderers)
            {
                if (r != null)
                    r.renderingLayerMask |= OUTLINE_LAYER_MASK;
            }
        }
        else
        {
            foreach (var r in targetRenderers)
            {
                if (r != null)
                    r.renderingLayerMask &= ~OUTLINE_LAYER_MASK;
            }
        }
    }
}
