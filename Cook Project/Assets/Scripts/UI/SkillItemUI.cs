using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SkillKeybindUI : MonoBehaviour
{
    [Header("Ability Id")]
    [SerializeField] private string abilityId;

    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI keyLabel;

    [Header("Settings")]
    [SerializeField] private bool hideWhenLocked = true;
    [SerializeField] private float lockedAlpha = 0.3f;

    private PlayerSoulAbilityManager abilityManager;

    private void Start()
    {
        abilityManager = PlayerSoulAbilityManager.Instance;

        if (abilityManager != null)
        {
            abilityManager.AbilitiesChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (abilityManager != null)
        {
            abilityManager.AbilitiesChanged -= RefreshUI;
        }
    }

    /// <summary>
    /// Refresh the displayed keybind according to PlayerSoulAbilityManager.
    /// </summary>
    private void RefreshUI()
    {
        if (abilityManager == null || keyLabel == null)
            return;

        bool unlocked = abilityManager.IsAbilityUnlocked(abilityId);
        var ability = GetAbilityData();
        if (ability == null)
            return;
        var activationKeyField = ability.GetType().GetField("activationKey");
        Key key = (Key)activationKeyField.GetValue(ability);

        if (icon != null)
        {
            var c = icon.color;
            c.a = unlocked ? 1f : lockedAlpha;
            icon.color = c;
        }

        keyLabel.text = key == Key.None ? "" : key.ToString().ToUpper();
        keyLabel.color = unlocked ? Color.white : new Color(1, 1, 1, 0.4f);

        if (hideWhenLocked)
        {
            gameObject.SetActive(unlocked);
        }
    }


    private object GetAbilityData()
    {
        var abilitiesField = typeof(PlayerSoulAbilityManager)
            .GetField("abilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (abilitiesField == null)
            return null;

        var list = abilitiesField.GetValue(abilityManager) as System.Collections.IList;
        if (list == null)
            return null;

        foreach (var entry in list)
        {
            var idField = entry.GetType().GetField("abilityId");
            if (idField == null)
                continue;

            string id = idField.GetValue(entry) as string;

            if (id == abilityId)
            {
                return entry;
            }
        }

        return null;
    }
}
