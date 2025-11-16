using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class AimHintUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string defaultActionName = "Interact";
    [SerializeField] private string defaultControlScheme = "keyboard&mouse";
    [SerializeField] private string defaultActionLabel = "Interact";

    public void UpdateHint(IInteractable interactable)
    {
        if (interactable != null)
        {
            hintText.text = BuildHintText(interactable);
            hintText.gameObject.SetActive(true);
        }
        else
        {
            hintText.gameObject.SetActive(false);
        }
    }

    private string BuildHintText(IInteractable interactable)
    {
        if (interactable is IInteractionPromptProvider promptProvider)
        {
            var prompts = promptProvider.GetPrompts();
            var formatted = FormatPrompts(prompts);
            if (!string.IsNullOrEmpty(formatted))
            {
                return formatted;
            }
        }

        return BuildFallbackHint();
    }

    private string FormatPrompts(IReadOnlyList<InteractionPromptDefinition> prompts)
    {
        if (prompts == null || prompts.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < prompts.Count; i++)
        {
            var prompt = prompts[i];
            var actionName = string.IsNullOrWhiteSpace(prompt.actionName) ? defaultActionName : prompt.actionName;
            var controlScheme = string.IsNullOrWhiteSpace(prompt.controlScheme) ? defaultControlScheme : prompt.controlScheme;
            var label = string.IsNullOrWhiteSpace(prompt.customText) ? actionName : prompt.customText;
            var binding = InputManager.Instance.GetBindingDisplayString(actionName, controlScheme);
            if (string.IsNullOrEmpty(binding))
            {
                binding = actionName;
            }

            sb.Append("[ ").Append(binding).Append(" ] ").Append(label);
            if (i < prompts.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private string BuildFallbackHint()
    {
        var label = string.IsNullOrWhiteSpace(defaultActionLabel) ? defaultActionName : defaultActionLabel;
        var binding = InputManager.Instance.GetBindingDisplayString(defaultActionName, defaultControlScheme);
        if (string.IsNullOrEmpty(binding))
        {
            binding = defaultActionName;
        }
        return $"[ {binding} ] {label}";
    }
}