using System;
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

        [Tooltip("Input System key binding for this ability. Use Key.None to disable hotkey activation.")]
        public Key activationKey = Key.None;

        [Min(0)]
        [Tooltip("Souls consumed when the ability activates successfully.")]
        public int soulCost = 10;

        [Tooltip("Invoked when the soul cost is paid and the ability activates.")]
        public UnityEvent onActivated = new UnityEvent();

        [Tooltip("Invoked when activation is attempted but the player lacks sufficient souls.")]
        public UnityEvent onInsufficientSouls = new UnityEvent();
    }

    public readonly struct AbilityDisplayInfo
    {
        public AbilityDisplayInfo(string abilityId, Key activationKey, int soulCost)
        {
            AbilityId = abilityId;
            ActivationKey = activationKey;
            SoulCost = soulCost;
        }

        public string AbilityId { get; }
        public Key ActivationKey { get; }
        public int SoulCost { get; }
    }

    [Header("Soul Meter Initialization")]
    [SerializeField] private bool applyStartingSoulValuesOnAwake = true;
    [SerializeField, Min(0)] private int startingMaxSouls = 100;
    [SerializeField, Min(0)] private int startingSouls = 0;

    [Header("Abilities")]
    [SerializeField] private List<SoulAbility> abilities = new List<SoulAbility>();

    private PlayerStatSystem playerStats;
    private Keyboard keyboard;

    private void Awake()
    {
        playerStats = PlayerStatSystem.Instance;

        if (applyStartingSoulValuesOnAwake && playerStats != null)
        {
            playerStats.SetMaxSouls(startingMaxSouls);
            playerStats.SetCurrentSouls(startingSouls);
        }
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
            if (ability == null)
            {
                continue;
            }

            buffer.Add(new AbilityDisplayInfo(ability.abilityId, ability.activationKey, ability.soulCost));
        }
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
    }
}
