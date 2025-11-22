using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying a single ability in the ability list.
/// Shows the hotkey and ability name.
/// </summary>
public class AbilityListItemUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private Image borderGlowImage;

    [Header("Display Settings")]
    [SerializeField] private string unboundKeyText = "?";
    [SerializeField] private string defaultAbilityName = "Unknown Ability";

    private PlayerSoulAbilityManager.AbilityDisplayInfo info;

    private void Awake()
    {
        var playerStat = PlayerStatSystem.Instance;
        playerStat.CurrentSouls
            .Subscribe(currSoul => borderGlowImage.gameObject.SetActive(currSoul >= info.SoulCost))
            .AddTo(this);
    }

    /// <summary>
    /// Updates the item display with ability information.
    /// </summary>
    /// <param name="info">The ability display information from PlayerSoulAbilityManager</param>
    public void UpdateDisplay(PlayerSoulAbilityManager.AbilityDisplayInfo info)
    {
        this.info = info;

        // Update key text
        if (keyText != null)
        {
            keyText.text = FormatHotkey(info.ActivationKey);
        }

        // Update cost text
        if (costText != null)
        {
            costText.text = info.SoulCost.ToString();
        }

        // Update ability name text
        if (abilityNameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(info.DisplayName)
                ? info.AbilityId
                : info.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = defaultAbilityName;
            }

            abilityNameText.text = displayName;
        }
    }

    /// <summary>
    /// Formats a Key into a user-friendly display string.
    /// </summary>
    private string FormatHotkey(Key key)
    {
        if (key == Key.None)
        {
            return unboundKeyText;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            KeyControl control = kb[key];
            if (control != null)
            {
                if (!string.IsNullOrWhiteSpace(control.shortDisplayName))
                {
                    return control.shortDisplayName;
                }

                if (!string.IsNullOrWhiteSpace(control.displayName))
                {
                    return control.displayName;
                }
            }
        }

        return key.ToString();
    }

}
