using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

/// <summary>
/// Centralizes reactions to ShiftSystem state changes so designers can toggle
/// behaviours and objects between Shift and AfterShift without wiring each script.
/// </summary>
public class ShiftStateManager : SceneSingleton<ShiftStateManager>
{
    [Header("Disable During After Shift")]
    [SerializeField] private List<Behaviour> behavioursDisabledDuringAfterShift = new();
    [SerializeField] private List<GameObject> objectsDisabledDuringAfterShift = new();

    [Header("Disable During Shift")]
    [SerializeField] private List<Behaviour> behavioursDisabledDuringShift = new();
    [SerializeField] private List<GameObject> objectsDisabledDuringShift = new();
    [SerializeField] private bool disableDarknessDamageDuringAfterShift = false;
    [SerializeField] private PlayerLightDamage playerLightDamage;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool logTransitions = false;

    public ShiftSystem.ShiftState CurrentState { get; private set; } = ShiftSystem.ShiftState.None;
    public bool IsAfterShift => CurrentState == ShiftSystem.ShiftState.AfterShift;

    public event Action<ShiftSystem.ShiftState> OnShiftStateChanged;
    public event Action<bool> OnAfterShiftChanged;

    private IShiftSystem shiftSystem;
    private IDisposable shiftStateSubscription;
    private bool initializationStarted;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        InitializeAsync().Forget();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance != this)
            return;

        shiftStateSubscription?.Dispose();
        shiftStateSubscription = null;
    }

    private async UniTaskVoid InitializeAsync()
    {
        if (initializationStarted)
            return;

        initializationStarted = true;

        await UniTask.WaitUntil(() => GameFlow.Instance != null && GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        if (shiftSystem == null)
        {
            Debug.LogWarning("[ShiftStateManager] Unable to resolve IShiftSystem.", this);
            return;
        }

        shiftStateSubscription?.Dispose();
        shiftStateSubscription = shiftSystem.currentState.Subscribe(HandleShiftStateChanged).AddTo(this);
        HandleShiftStateChanged(shiftSystem.currentState.Value);
    }

    private void HandleShiftStateChanged(ShiftSystem.ShiftState state)
    {
        CurrentState = state;
        bool inAfterShift = state == ShiftSystem.ShiftState.AfterShift;

        ApplyInspectorToggles(inAfterShift);
        ApplyDarknessDamageToggle(inAfterShift);

        if (logTransitions)
        {
            Debug.Log($"[ShiftStateManager] State changed to {state}");
        }

        OnShiftStateChanged?.Invoke(state);
        OnAfterShiftChanged?.Invoke(inAfterShift);
    }

    private void ApplyInspectorToggles(bool afterShift)
    {
        foreach (var behaviour in behavioursDisabledDuringAfterShift)
        {
            if (behaviour != null)
            {
                behaviour.enabled = !afterShift;
            }
        }

        foreach (var obj in objectsDisabledDuringAfterShift)
        {
            if (obj != null)
            {
                obj.SetActive(!afterShift);
            }
        }

        bool shiftActive = !afterShift;

        foreach (var behaviour in behavioursDisabledDuringShift)
        {
            if (behaviour != null)
            {
                behaviour.enabled = !shiftActive;
            }
        }

        foreach (var obj in objectsDisabledDuringShift)
        {
            if (obj != null)
            {
                obj.SetActive(!shiftActive);
            }
        }
    }

    private void ApplyDarknessDamageToggle(bool afterShift)
    {
        if (!disableDarknessDamageDuringAfterShift)
        {
            return;
        }

        var lightDamage = ResolvePlayerLightDamage();
        if (lightDamage == null)
        {
            return;
        }

        lightDamage.SetDamageDisabled(afterShift);
    }

    public void RegisterBehaviourDisableDuringAfterShift(Behaviour behaviour, bool applyImmediately = true)
    {
        if (behaviour == null || behavioursDisabledDuringAfterShift.Contains(behaviour))
            return;

        behavioursDisabledDuringAfterShift.Add(behaviour);
        if (applyImmediately)
        {
            behaviour.enabled = !IsAfterShift;
        }
    }

    public void RegisterObjectDisableDuringAfterShift(GameObject obj, bool applyImmediately = true)
    {
        if (obj == null || objectsDisabledDuringAfterShift.Contains(obj))
            return;

        objectsDisabledDuringAfterShift.Add(obj);
        if (applyImmediately)
        {
            obj.SetActive(!IsAfterShift);
        }
    }

    public void RegisterBehaviourDisableDuringShift(Behaviour behaviour, bool applyImmediately = true)
    {
        if (behaviour == null || behavioursDisabledDuringShift.Contains(behaviour))
            return;

        behavioursDisabledDuringShift.Add(behaviour);
        if (applyImmediately)
        {
            behaviour.enabled = IsAfterShift;
        }
    }

    public void RegisterObjectDisableDuringShift(GameObject obj, bool applyImmediately = true)
    {
        if (obj == null || objectsDisabledDuringShift.Contains(obj))
            return;

        objectsDisabledDuringShift.Add(obj);
        if (applyImmediately)
        {
            obj.SetActive(IsAfterShift);
        }
    }

    public void RegisterAfterShiftListener(Action<bool> listener, bool invokeImmediately = true)
    {
        if (listener == null)
            return;

        OnAfterShiftChanged += listener;
        if (invokeImmediately)
        {
            listener.Invoke(IsAfterShift);
        }
    }

    public void UnregisterAfterShiftListener(Action<bool> listener)
    {
        if (listener == null)
            return;
        OnAfterShiftChanged -= listener;
    }

    private PlayerLightDamage ResolvePlayerLightDamage()
    {
        if (playerLightDamage != null)
        {
            return playerLightDamage;
        }

        playerLightDamage = FindFirstObjectByType<PlayerLightDamage>(FindObjectsInactive.Include);
        if (playerLightDamage == null)
        {
            playerLightDamage = FindFirstObjectByType<PlayerLightDamage>();
        }

        if (playerLightDamage == null && logTransitions)
        {
            Debug.LogWarning("[ShiftStateManager] PlayerLightDamage not found; cannot toggle darkness damage state.", this);
        }

        return playerLightDamage;
    }
}
