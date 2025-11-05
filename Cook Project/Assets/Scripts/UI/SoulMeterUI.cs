using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Displays the player's soul meter alongside the configured soul abilities.
/// Uses PlayerStatSystem for meter data and PlayerSoulAbilityManager for ability metadata.
/// </summary>
public class SoulMeterUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private TextMeshProUGUI soulCountLabel;
    [SerializeField] private RectTransform abilityListRoot;
    [SerializeField] private PlayerSoulAbilityManager abilityManager;

    [Header("Display Text")]
    [SerializeField] private string soulCountFormat = "{0}/{1} Souls";
    [SerializeField] private string abilityLineFormat = "{0} - {1} ({2})";
    [SerializeField] private string freeCostLabel = "Free";
    [SerializeField] private string noAbilitiesPlaceholder = "No abilities unlocked";

    [Header("Styling")]
    [SerializeField] private float abilityFontSize = 18f;
    [SerializeField] private Color abilityTextColor = Color.white;

    private PlayerStatSystem playerStats;
    private readonly List<PlayerSoulAbilityManager.AbilityDisplayInfo> abilityInfos = new();
    private readonly List<TextMeshProUGUI> abilityLabels = new();
    private bool hasLoggedMissingLabel;
    private bool hasLoggedMissingAbilityRoot;

    private void Awake()
    {
        playerStats = PlayerStatSystem.Instance;

        if (playerStats != null)
        {
            playerStats.CurrentSouls.Subscribe(_ => UpdateSoulCount()).AddTo(this);
            playerStats.MaxSouls.Subscribe(_ => UpdateSoulCount()).AddTo(this);
        }

        UpdateSoulCount();
    }

    private void OnEnable()
    {
        UpdateSoulCount();
        RefreshAbilityList();
    }

    private void Start()
    {
        RefreshAbilityList();
    }

    private void Update()
    {
        if (abilityManager == null)
        {
            ResolveAbilityManager();
            if (abilityManager != null)
            {
                RefreshAbilityList();
            }
        }
    }

    public void RefreshAbilityList()
    {
        ResolveAbilityManager();

        if (abilityListRoot == null)
        {
            if (!hasLoggedMissingAbilityRoot)
            {
                hasLoggedMissingAbilityRoot = true;
                Debug.LogWarning("SoulMeterUI: Ability list root is not assigned.", this);
            }

            return;
        }

        if (abilityManager == null)
        {
            ShowEmptyAbilityMessage(noAbilitiesPlaceholder);
            return;
        }

        abilityManager.PopulateAbilityDisplayInfo(abilityInfos);

        if (abilityInfos.Count == 0)
        {
            ShowEmptyAbilityMessage(noAbilitiesPlaceholder);
            return;
        }

        EnsureAbilityLabelCount(abilityInfos.Count);

        for (int i = 0; i < abilityInfos.Count; i++)
        {
            var info = abilityInfos[i];
            TextMeshProUGUI label = abilityLabels[i];
            label.gameObject.SetActive(true);
            label.color = abilityTextColor;
            label.text = string.Format(abilityLineFormat, FormatHotkey(info.ActivationKey), info.AbilityId, FormatCost(info.SoulCost));
        }

        for (int i = abilityInfos.Count; i < abilityLabels.Count; i++)
        {
            abilityLabels[i].gameObject.SetActive(false);
        }
    }

    public void ForceRefresh()
    {
        UpdateSoulCount();
        RefreshAbilityList();
    }

    private void UpdateSoulCount()
    {
        if (soulCountLabel == null)
        {
            if (!hasLoggedMissingLabel)
            {
                hasLoggedMissingLabel = true;
                Debug.LogWarning("SoulMeterUI: Soul count label is not assigned.", this);
            }

            return;
        }

        if (playerStats == null)
        {
            soulCountLabel.text = string.Format(soulCountFormat, 0, 0);
            return;
        }

        soulCountLabel.text = string.Format(soulCountFormat, playerStats.CurrentSouls.Value, playerStats.MaxSouls.Value);
    }

    private void ShowEmptyAbilityMessage(string message)
    {
        EnsureAbilityLabelCount(1);
        TextMeshProUGUI label = abilityLabels[0];
        label.gameObject.SetActive(true);
        label.color = abilityTextColor;
        label.text = message;

        for (int i = 1; i < abilityLabels.Count; i++)
        {
            abilityLabels[i].gameObject.SetActive(false);
        }
    }

    private void EnsureAbilityLabelCount(int targetCount)
    {
        while (abilityLabels.Count < targetCount)
        {
            abilityLabels.Add(CreateAbilityLabel());
        }
    }

    private TextMeshProUGUI CreateAbilityLabel()
    {
        var go = new GameObject("AbilityLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(abilityListRoot, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = Vector2.zero;

        var text = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.fontSize = abilityFontSize;
        text.color = abilityTextColor;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = string.Empty;

        return text;
    }

    private string FormatCost(int soulCost)
    {
        if (soulCost <= 0)
        {
            return freeCostLabel;
        }

        return soulCost == 1 ? "1 Soul" : $"{soulCost} Souls";
    }

    private string FormatHotkey(Key key)
    {
        if (key == Key.None)
        {
            return "Unbound";
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

    private void ResolveAbilityManager()
    {
        if (abilityManager != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        abilityManager = FindFirstObjectByType<PlayerSoulAbilityManager>(FindObjectsInactive.Exclude);
#else
        abilityManager = FindObjectOfType<PlayerSoulAbilityManager>();
#endif
    }
}
