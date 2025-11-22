using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's soul meter with a fill gauge, current/max text, and ability list.
/// Fill amount maps 0%-100% to 0.167-1.0 range.
/// </summary>
public class SoulMeterUI2 : MonoBehaviour
{
    [Header("Soul Meter Components")]
    [SerializeField] private Image soulFillImage;
    [SerializeField] private TextMeshProUGUI soulCurrentText;
    [SerializeField] private TextMeshProUGUI soulMaxText;

    [Header("Fill Amount Settings")]
    [SerializeField] private float fillAmountMin = 0.167f; // 0%
    [SerializeField] private float fillAmountMax = 1.0f;   // 100%

    [Header("Ability List")]
    [SerializeField] private RectTransform abilityListContainer;
    [SerializeField] private AbilityListItemUI abilityListItemPrefab;
    [SerializeField] private PlayerSoulAbilityManager abilityManager;

    private PlayerStatSystem playerStats;
    private readonly List<PlayerSoulAbilityManager.AbilityDisplayInfo> abilityInfos = new();
    private readonly List<AbilityListItemUI> abilityItems = new();
    private bool listeningForAbilityChanges;

    private void Awake()
    {
        playerStats = PlayerStatSystem.Instance;

        if (playerStats != null)
        {
            playerStats.CurrentSouls.Subscribe(_ => UpdateSoulDisplay()).AddTo(this);
            playerStats.MaxSouls.Subscribe(_ => UpdateSoulDisplay()).AddTo(this);
            playerStats.CanUseWeapon.Subscribe(can => gameObject.SetActive(can)).AddTo(this);
        }

        // Hide prefab
        if (abilityListItemPrefab != null)
        {
            abilityListItemPrefab.gameObject.SetActive(false);
        }

        UpdateSoulDisplay();
    }

    private void OnEnable()
    {
        UpdateSoulDisplay();
        ResolveAbilityManager();
        EnsureAbilityChangeSubscription();
        RefreshAbilityList();
    }

    private void OnDisable()
    {
        UnsubscribeFromAbilityChanges();
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
                EnsureAbilityChangeSubscription();
                RefreshAbilityList();
            }
        }
    }

    private void UpdateSoulDisplay()
    {
        if (playerStats == null)
        {
            return;
        }

        int currentSouls = playerStats.CurrentSouls.Value;
        int maxSouls = playerStats.MaxSouls.Value;

        // Calculate soul percentage (0-1)
        float soulPercentage = maxSouls > 0 ? Mathf.Clamp01((float)currentSouls / maxSouls) : 0f;

        // Update fill amount
        if (soulFillImage != null)
        {
            soulFillImage.fillAmount = Mathf.Lerp(fillAmountMin, fillAmountMax, soulPercentage);
        }

        // Update text displays
        if (soulCurrentText != null)
        {
            soulCurrentText.text = currentSouls.ToString();
        }

        if (soulMaxText != null)
        {
            soulMaxText.text = maxSouls.ToString();
        }
    }

    public void RefreshAbilityList()
    {
        ResolveAbilityManager();
        EnsureAbilityChangeSubscription();

        if (abilityListContainer == null || abilityListItemPrefab == null)
        {
            Debug.LogWarning("SoulMeterUI: Ability list container or prefab is not assigned.", this);
            return;
        }

        if (abilityManager == null)
        {
            ClearAbilityList();
            return;
        }

        abilityManager.PopulateAbilityDisplayInfo(abilityInfos);

        // Adjust item count
        while (abilityItems.Count < abilityInfos.Count)
        {
            var itemUI = Instantiate(abilityListItemPrefab, abilityListContainer);
            itemUI.gameObject.SetActive(true);
            if (itemUI != null)
            {
                abilityItems.Add(itemUI);
            }
            else
            {
                Debug.LogWarning("SoulMeterUI: Ability list item prefab is missing AbilityListItemUI component.", this);
                Destroy(itemUI.gameObject);
            }
        }

        // Hide excess items
        for (int i = abilityInfos.Count; i < abilityItems.Count; i++)
        {
            abilityItems[i].gameObject.SetActive(false);
        }

        // Update visible items
        for (int i = 0; i < abilityInfos.Count; i++)
        {
            var info = abilityInfos[i];
            abilityItems[i].gameObject.SetActive(true);
            abilityItems[i].UpdateDisplay(info);
        }
    }

    private void ClearAbilityList()
    {
        foreach (var item in abilityItems)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureAbilityChangeSubscription()
    {
        if (abilityManager == null || listeningForAbilityChanges)
        {
            return;
        }

        abilityManager.AbilitiesChanged += HandleAbilitiesChanged;
        listeningForAbilityChanges = true;
    }

    private void UnsubscribeFromAbilityChanges()
    {
        if (abilityManager != null && listeningForAbilityChanges)
        {
            abilityManager.AbilitiesChanged -= HandleAbilitiesChanged;
        }

        listeningForAbilityChanges = false;
    }

    private void HandleAbilitiesChanged()
    {
        RefreshAbilityList();
    }

    private void ResolveAbilityManager()
    {
        if (abilityManager != null)
        {
            return;
        }

        abilityManager = FindFirstObjectByType<PlayerSoulAbilityManager>(FindObjectsInactive.Exclude);
    }

    private void OnValidate()
    {
        // Ensure min < max for fill amounts
        if (fillAmountMin > fillAmountMax)
        {
            fillAmountMin = fillAmountMax;
        }

        // Ensure fill amounts are in valid range
        fillAmountMin = Mathf.Clamp01(fillAmountMin);
        fillAmountMax = Mathf.Clamp01(fillAmountMax);
    }
}
