using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic interactable wrapper that lets designers hook UnityEvents into the interact system.
/// </summary>
[AddComponentMenu("Gameplay/Unity Interactable")]
[RequireComponent(typeof(Collider))]
public class UnityInteractable : MonoBehaviour, IInteractable, IInteractionPromptProvider
{
    [SerializeField]
    [Tooltip("Invoked every time the player interacts while this component is enabled.")]
    private UnityEvent onInteract = new UnityEvent();

    [SerializeField]
    [Tooltip("Invoked on the first interaction only.")]
    private UnityEvent onFirstInteract = new UnityEvent();

    [SerializeField]
    [Tooltip("Only allow the interaction once per scene load.")]
    private bool interactOnce = false;

    [SerializeField]
    [Tooltip("Disable listed behaviours after the first successful interaction when Interact Once is enabled.")]
    private Behaviour[] disableOnUse;

    [SerializeField]
    [Tooltip("Optional collider reference toggled off after a single interaction.")]
    private Collider colliderToDisable;

    [SerializeField]
    [Tooltip("When a BillboardSprite is present, automatically fits a BoxCollider to match the sprite size."),]
    private bool fitColliderToBillboard = true;

    [SerializeField]
    [Tooltip("Depth to apply when auto-fitting colliders for billboard sprites.")]
    private float billboardColliderDepth = 0.2f;

    [Header("Input Prompts")]
    [SerializeField]
    [Tooltip("Customize which inputs appear on the HUD when looking at this object.")]
    private List<InteractionPromptDefinition> customPrompts = new List<InteractionPromptDefinition>
    {
        new InteractionPromptDefinition()
    };

    private bool hasInteracted;

    private void Reset()
    {
        EnsureColliderReference();
        EnsureInteractableLayer();
        FitColliderIfPossible();
        EnsurePromptDefaults();
    }

    private void OnValidate()
    {
        EnsureColliderReference();
        EnsureInteractableLayer();
        FitColliderIfPossible();
        EnsurePromptDefaults();
    }

    private void Awake()
    {
        EnsureColliderReference();
        FitColliderIfPossible();
        EnsurePromptDefaults();
    }


    public void Interact()
    {
        if (interactOnce && hasInteracted)
        {
            return;
        }

        if (!hasInteracted)
        {
            hasInteracted = true;
            onFirstInteract.Invoke();

            if (interactOnce)
            {
                DisableConfiguredObjects();
            }
        }

        onInteract.Invoke();
    }

    public void AddOnInteractListener(UnityAction listener)
    {
        if (listener == null)
        {
            return;
        }

        onInteract.AddListener(listener);
    }

    public void RemoveOnInteractListener(UnityAction listener)
    {
        if (listener == null)
        {
            return;
        }

        onInteract.RemoveListener(listener);
    }

    public void AddOnFirstInteractListener(UnityAction listener)
    {
        if (listener == null)
        {
            return;
        }

        onFirstInteract.AddListener(listener);
    }

    private void DisableConfiguredObjects()
    {
        if (colliderToDisable != null)
        {
            colliderToDisable.enabled = false;
        }

        if (disableOnUse == null)
        {
            return;
        }

        foreach (var behaviour in disableOnUse)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }
    }

    private void EnsureColliderReference()
    {
        if (colliderToDisable != null)
        {
            return;
        }

        colliderToDisable = GetComponent<Collider>();
        if (colliderToDisable == null)
        {
            colliderToDisable = gameObject.AddComponent<BoxCollider>();
        }
    }

    private void EnsureInteractableLayer()
    {
        int interactLayer = LayerMask.NameToLayer("Interactable");
        if (interactLayer != -1 && gameObject.layer != interactLayer)
        {
            gameObject.layer = interactLayer;
        }
    }

    private void FitColliderIfPossible()
    {
        if (!fitColliderToBillboard)
        {
            return;
        }

        if (!TryGetComponent(out NvJ.Rendering.BillboardSprite billboard))
        {
            return;
        }

        if (colliderToDisable is not BoxCollider boxCollider)
        {
            // Replace with a BoxCollider so we can control the bounds.
            if (colliderToDisable != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(colliderToDisable);
                }
                else
                {
                    DestroyImmediate(colliderToDisable);
                }
            }

            boxCollider = gameObject.AddComponent<BoxCollider>();
            colliderToDisable = boxCollider;
        }

        var billboardSize = billboard.size;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(
            Mathf.Max(0.01f, billboardSize.x),
            Mathf.Max(0.01f, billboardSize.y),
            Mathf.Max(0.01f, billboardColliderDepth));
        boxCollider.isTrigger = false;
    }

    private void EnsurePromptDefaults()
    {
        if (customPrompts == null || customPrompts.Count == 0)
        {
            customPrompts = new List<InteractionPromptDefinition>
            {
                new InteractionPromptDefinition()
            };
        }
    }

    public IReadOnlyList<InteractionPromptDefinition> GetPrompts() => customPrompts;
}
