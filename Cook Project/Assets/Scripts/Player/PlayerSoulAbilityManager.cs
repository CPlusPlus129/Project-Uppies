using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Handles player soul abilities, mapping configurable hotkeys to UnityEvents while
/// ensuring the required soul cost is deducted from the PlayerStatSystem.
/// Also provides optional initialization for the player's soul capacity at runtime.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSoulAbilityManager : MonoBehaviour
{
    [Serializable]
    private class SoulAbility
    {
        [Tooltip("Unique identifier to trigger this ability via script or UI.")]
        public string abilityId = "NewAbility";

        [Tooltip("Optional display name shown in UI and shop listings. Defaults to the ability id when empty.")]
        public string displayName;

        [Tooltip("Input System key binding for this ability. Use Key.None to disable hotkey activation.")]
        public Key activationKey = Key.None;

        [Min(0)]
        [Tooltip("Souls consumed when the ability activates successfully.")]
        public int soulCost = 10;

        [Min(0)]
        [Tooltip("Currency cost to unlock this ability in the shop. Set to 0 to unlock for free.")]
        public int unlockCost = 0;

        [Tooltip("If false, the player must purchase this ability before it can be used or shown in UI.")]
        public bool unlockedByDefault = true;

        [TextArea]
        [Tooltip("Short description shown in the shop when purchasing this ability.")]
        public string shopDescription;

        [Tooltip("Icon shown alongside this ability in the shop UI.")]
        public Sprite shopIcon;

        [Tooltip("If enabled, this ability only activates when an active order references at least one tracked ingredient.")]
        public bool requiresTrackedIngredient = false;

        [Tooltip("Invoked when the soul cost is paid and the ability activates.")]
        public UnityEvent onActivated = new UnityEvent();

        [Tooltip("Invoked when activation is attempted but the player lacks sufficient souls.")]
        public UnityEvent onInsufficientSouls = new UnityEvent();

        [NonSerialized] public bool isUnlocked;
    }

    public readonly struct AbilityDisplayInfo
    {
        public AbilityDisplayInfo(string abilityId, string displayName, Key activationKey, int soulCost)
        {
            AbilityId = abilityId;
            DisplayName = displayName;
            ActivationKey = activationKey;
            SoulCost = soulCost;
        }

        public string AbilityId { get; }
        public string DisplayName { get; }
        public Key ActivationKey { get; }
        public int SoulCost { get; }
    }

    public readonly struct AbilityShopEntry
    {
        public AbilityShopEntry(string abilityId, string displayName, string description, Sprite icon, int unlockCost)
        {
            AbilityId = abilityId;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            UnlockCost = unlockCost;
        }

        public string AbilityId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public int UnlockCost { get; }
    }

    [Header("Soul Meter Initialization")]
    [SerializeField] private bool applyStartingSoulValuesOnAwake = true;
    [SerializeField, Min(0)] private int startingMaxSouls = 100;
    [SerializeField, Min(0)] private int startingSouls = 0;

    [Header("Abilities")]
    [SerializeField] private List<SoulAbility> abilities = new List<SoulAbility>();

    [Header("Ability Requirements")]
    [SerializeField] private FridgeGlowEligibilityTracker fridgeEligibilityTracker;

    public static PlayerSoulAbilityManager Instance { get; private set; }
    public event Action AbilitiesChanged;

    private PlayerStatSystem playerStats;
    private Keyboard keyboard;
    private bool hasLoggedRequirementFailure;
    private bool hasLoggedMissingTracker;
    private bool hasLoggedLockedAbility;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerSoulAbilityManager instances detected. Only the latest instance will be used.");
        }

        Instance = this;
        playerStats = PlayerStatSystem.Instance;
        InitializeAbilityUnlockStates();

        if (applyStartingSoulValuesOnAwake && playerStats != null)
        {
            playerStats.SetMaxSouls(startingMaxSouls);
            playerStats.SetCurrentSouls(startingSouls);
        }
    }

    private void Start()
    {
        StartCoroutine(RefreshShopInventoryWhenReady());
    }

    private void Update()
    {
        EnsureDependencies();
        if (playerStats == null || keyboard == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (ability.activationKey == Key.None)
            {
                continue;
            }

            KeyControl keyControl = keyboard[ability.activationKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                ExecuteAbility(ability);
            }
        }
    }

    public bool TriggerAbilityByName(string abilityId)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return false;
        }

        EnsureDependencies();

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (string.Equals(ability.abilityId, abilityId, StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteAbility(ability);
            }
        }

        Debug.LogWarning($"PlayerSoulAbilityManager: Ability with id '{abilityId}' not found.", this);

        return false;
    }

    public bool TriggerAbilityByIndex(int index)
    {
        EnsureDependencies();

        if (index < 0 || index >= abilities.Count)
        {
            Debug.LogWarning($"PlayerSoulAbilityManager: Ability index {index} out of range.", this);

            return false;
        }

        return ExecuteAbility(abilities[index]);
    }

    public void PopulateAbilityDisplayInfo(List<AbilityDisplayInfo> buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        buffer.Clear();

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (ability == null || !ability.isUnlocked)
            {
                continue;
            }

            buffer.Add(new AbilityDisplayInfo(ability.abilityId, GetAbilityDisplayLabel(ability), ability.activationKey, ability.soulCost));
        }
    }

    public void PopulateLockedAbilityShopEntries(List<AbilityShopEntry> buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        buffer.Clear();

        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (ability == null || ability.isUnlocked || ability.unlockCost <= 0)
            {
                continue;
            }

            buffer.Add(new AbilityShopEntry(
                ability.abilityId,
                GetAbilityDisplayLabel(ability),
                ability.shopDescription,
                ability.shopIcon,
                ability.unlockCost));
        }
    }

    public bool TryUnlockAbility(string abilityId)
    {
        SoulAbility ability = FindAbility(abilityId);
        if (ability == null || ability.isUnlocked)
        {
            return false;
        }

        ability.isUnlocked = true;
        hasLoggedLockedAbility = false;
        AbilitiesChanged?.Invoke();
        return true;
    }

    public bool IsAbilityUnlocked(string abilityId)
    {
        SoulAbility ability = FindAbility(abilityId);
        return ability != null && ability.isUnlocked;
    }

    public void RefreshKeyboardDevice(InputDevice device)
    {
        keyboard = device as Keyboard;
    }

    private void EnsureDependencies()
    {
        playerStats ??= PlayerStatSystem.Instance;
        keyboard = Keyboard.current;
    }

    private bool ExecuteAbility(SoulAbility ability)
    {
        if (playerStats == null)
        {
            return false;
        }

        if (ability == null || !ability.isUnlocked)
        {
            if (!hasLoggedLockedAbility && ability != null)
            {
                hasLoggedLockedAbility = true;
                Debug.LogWarning($"PlayerSoulAbilityManager: Ability '{ability.abilityId}' is locked and cannot be used until purchased.", this);
            }

            return false;
        }

        if (!MeetsActivationRequirements(ability))
        {
            return false;
        }

        if (!playerStats.TrySpendSouls(ability.soulCost))
        {
            ability.onInsufficientSouls?.Invoke();
            return false;
        }

        ability.onActivated?.Invoke();
        return true;
    }

    private void OnValidate()
    {
        if (startingSouls > startingMaxSouls)
        {
            startingSouls = startingMaxSouls;
        }

        if (abilities == null)
        {
            return;
        }

        foreach (var ability in abilities)
        {
            if (ability == null)
            {
                continue;
            }

            if (ability.unlockCost < 0)
            {
                ability.unlockCost = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator RefreshShopInventoryWhenReady()
    {
        // Wait for the ShopSystem to be initialized so ability unlock items populate immediately.
        while (!ShopSystem.Instance.IsInitialized)
        {
            yield return null;
        }

        ShopSystem.Instance.RefreshShopItems();
    }

    private void InitializeAbilityUnlockStates()
    {
        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (ability == null)
            {
                continue;
            }

            ability.isUnlocked = ability.unlockedByDefault || ability.unlockCost <= 0;
        }
    }

    private bool MeetsActivationRequirements(SoulAbility ability)
    {
        if (ability == null || !ability.requiresTrackedIngredient)
        {
            return true;
        }

        if (HasActiveTrackedIngredients())
        {
            hasLoggedRequirementFailure = false;
            return true;
        }

        if (!hasLoggedRequirementFailure)
        {
            hasLoggedRequirementFailure = true;
            Debug.LogWarning("PlayerSoulAbilityManager: Ability activation blocked because no active orders have tracked ingredients.", this);
        }

        return false;
    }

    private bool HasActiveTrackedIngredients()
    {
        FridgeGlowEligibilityTracker tracker = ResolveFridgeTracker();
        if (tracker == null)
        {
            if (!hasLoggedMissingTracker)
            {
                hasLoggedMissingTracker = true;
                Debug.LogWarning("PlayerSoulAbilityManager: Could not locate FridgeGlowEligibilityTracker required for ability gating.", this);
            }

            return false;
        }

        var eligible = tracker.EligibleFridges;
        return eligible != null && eligible.Count > 0;
    }

    private FridgeGlowEligibilityTracker ResolveFridgeTracker()
    {
        if (fridgeEligibilityTracker != null)
        {
            return fridgeEligibilityTracker;
        }

#if UNITY_2023_1_OR_NEWER
        fridgeEligibilityTracker = FindFirstObjectByType<FridgeGlowEligibilityTracker>(FindObjectsInactive.Exclude);
#else
        fridgeEligibilityTracker = FindObjectOfType<FridgeGlowEligibilityTracker>();
#endif
        return fridgeEligibilityTracker;
    }

    private SoulAbility FindAbility(string abilityId)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return null;
        }

        if (abilities == null)
        {
            return null;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            SoulAbility ability = abilities[i];
            if (ability == null)
            {
                continue;
            }

            if (string.Equals(ability.abilityId, abilityId, StringComparison.OrdinalIgnoreCase))
            {
                return ability;
            }
        }

        return null;
    }

    private string GetAbilityDisplayLabel(SoulAbility ability)
    {
        if (ability == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(ability.displayName) ? ability.abilityId : ability.displayName;
    }
}
