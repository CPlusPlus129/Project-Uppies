using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

/// <summary>
/// Centralizes reactions to ShiftSystem state changes so designers can toggle
/// behaviours and objects between Shift and AfterShift without wiring each script.
/// </summary>
public class ShiftStateManager : MonoBehaviour
{
    public static ShiftStateManager Instance { get; private set; }

    [Header("Disable During After Shift")]
    [SerializeField] private List<Behaviour> behavioursDisabledDuringAfterShift = new();
    [SerializeField] private List<GameObject> objectsDisabledDuringAfterShift = new();

    [Header("Disable During Shift")]
    [SerializeField] private List<Behaviour> behavioursDisabledDuringShift = new();
    [SerializeField] private List<GameObject> objectsDisabledDuringShift = new();
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool logTransitions = false;

    public ShiftSystem.ShiftState CurrentState { get; private set; } = ShiftSystem.ShiftState.None;
    public bool IsAfterShift => CurrentState == ShiftSystem.ShiftState.AfterShift;

    public event Action<ShiftSystem.ShiftState> OnShiftStateChanged;
    public event Action<bool> OnAfterShiftChanged;

    private IShiftSystem shiftSystem;
    private IDisposable shiftStateSubscription;
    private bool initializationStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad && Application.isPlaying)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        InitializeAsync().Forget();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

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
}
