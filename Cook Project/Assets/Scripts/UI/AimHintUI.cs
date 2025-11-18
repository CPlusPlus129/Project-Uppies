using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class AimHintUI : MonoBehaviour
{
    [SerializeField] private HintItem hintItemPrefab;
    [SerializeField] private Transform hintContainer;
    [SerializeField] private string defaultActionName = "Interact";
    [SerializeField] private string defaultControlScheme = "keyboard&mouse";
    [SerializeField] private string defaultActionLabel = "Interact";

    private List<HintItem> activeHintItems = new List<HintItem>();
    private List<HintItem> hintItemPool = new List<HintItem>();

    private void Awake()
    {
        hintItemPrefab.gameObject.SetActive(false);
    }

    public void UpdateHint(IInteractable interactable)
    {
        ClearHintItems();

        if (interactable != null)
        {
            ShowHints(interactable);
        }
    }

    private void ShowHints(IInteractable interactable)
    {
        if (interactable is IInteractionPromptProvider promptProvider)
        {
            var prompts = promptProvider.GetPrompts();
            if (prompts != null && prompts.Count > 0)
            {
                ShowPromptItems(prompts);
                return;
            }
        }

        // Fallback to default hint
        ShowDefaultHint();
    }

    private void ShowPromptItems(IReadOnlyList<InteractionPromptDefinition> prompts)
    {
        foreach (var prompt in prompts)
        {
            var actionName = string.IsNullOrWhiteSpace(prompt.actionName) ? defaultActionName : prompt.actionName;
            var controlScheme = string.IsNullOrWhiteSpace(prompt.controlScheme) ? defaultControlScheme : prompt.controlScheme;
            var label = string.IsNullOrWhiteSpace(prompt.customText) ? actionName : prompt.customText;

            var binding = InputManager.Instance.GetBindingDisplayString(actionName, controlScheme);
            if (string.IsNullOrEmpty(binding))
            {
                binding = actionName;
            }

            CreateHintItem(binding, label);
        }
    }

    private void ShowDefaultHint()
    {
        var label = string.IsNullOrWhiteSpace(defaultActionLabel) ? defaultActionName : defaultActionLabel;
        var binding = InputManager.Instance.GetBindingDisplayString(defaultActionName, defaultControlScheme);
        if (string.IsNullOrEmpty(binding))
        {
            binding = defaultActionName;
        }

        CreateHintItem(binding, label);
    }

    private void CreateHintItem(string key, string hint)
    {
        HintItem item = GetOrCreateHintItem();
        item.transform.SetAsLastSibling();
        item.SetHint(key, hint);
        item.Show();
        activeHintItems.Add(item);
    }

    private HintItem GetOrCreateHintItem()
    {
        // try to get from pool
        if (hintItemPool.Count > 0)
        {
            var item = hintItemPool[hintItemPool.Count - 1];
            hintItemPool.RemoveAt(hintItemPool.Count - 1);
            return item;
        }

        // create new item if pool is empty
        var newItem = Instantiate(hintItemPrefab, hintContainer);
        newItem.gameObject.SetActive(true);
        return newItem;
    }

    private void ClearHintItems()
    {
        // put all active item back to pool
        foreach (var item in activeHintItems)
        {
            item.Hide();
            hintItemPool.Add(item);
        }
        activeHintItems.Clear();
    }
}