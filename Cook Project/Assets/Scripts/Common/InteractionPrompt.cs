using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a single input hint for an interactable object.
/// </summary>
[Serializable]
public class InteractionPromptDefinition
{
    public string actionName = "Interact";
    public string controlScheme = "keyboard&mouse";
    [Tooltip("Custom text shown next to the binding. Falls back to the action name if empty.")]
    public string customText = "Interact";
}

/// <summary>
/// Optional interface that allows an interactable to expose custom input hints to the HUD.
/// </summary>
public interface IInteractionPromptProvider
{
    IReadOnlyList<InteractionPromptDefinition> GetPrompts();
}
