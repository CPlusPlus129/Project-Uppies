using System;
using Cysharp.Threading.Tasks;
using Meryuhi.Rendering;
using R3;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class ShiftFogController : MonoBehaviour
{
    [SerializeField] private Color afterShiftColor = Color.white;

    private Volume volume;
    private FullScreenFog fogComponent;
    private Color defaultFogColor;
    private bool hasDefaultColor;
    private IShiftSystem shiftSystem;
    private IDisposable shiftSubscription;
    private bool initializationRequested;

    private void Awake()
    {
        volume = GetComponent<Volume>();
    }

    private void OnEnable()
    {
        InitializeAsync().Forget();
    }

    private void OnDisable()
    {
        shiftSubscription?.Dispose();
        shiftSubscription = null;

        // Allow the controller to reinitialize the next time it is enabled
        initializationRequested = false;

        if (fogComponent != null && hasDefaultColor)
        {
            fogComponent.color.value = defaultFogColor;
        }
    }

    private async UniTaskVoid InitializeAsync()
    {
        if (initializationRequested)
            return;

        initializationRequested = true;

        if (volume == null)
        {
            volume = GetComponent<Volume>();
            if (volume == null)
                return;
        }

        var runtimeProfile = EnsureRuntimeProfile();
        if (runtimeProfile == null)
        {
            Debug.LogWarning($"[{nameof(ShiftFogController)}] No volume profile available on {name}.", this);
            return;
        }

        if (!runtimeProfile.TryGet(out fogComponent) || fogComponent == null)
        {
            Debug.LogWarning($"[{nameof(ShiftFogController)}] FullScreenFog component missing on profile for {name}.", this);
            return;
        }

        defaultFogColor = fogComponent.color.value;
        hasDefaultColor = true;

        await UniTask.WaitUntil(() => GameFlow.Instance != null && GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        if (shiftSystem == null)
            return;

        shiftSubscription?.Dispose();
        shiftSubscription = shiftSystem.currentState.Subscribe(OnShiftStateChanged);
        OnShiftStateChanged(shiftSystem.currentState.Value);
    }

    private VolumeProfile EnsureRuntimeProfile()
    {
        var profile = volume.profile;
        if (profile == null)
        {
            profile = volume.sharedProfile != null
                ? Instantiate(volume.sharedProfile)
                : ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
        }
        return profile;
    }

    private void OnShiftStateChanged(ShiftSystem.ShiftState state)
    {
        if (fogComponent == null)
            return;

        if (state == ShiftSystem.ShiftState.AfterShift)
        {
            fogComponent.color.value = afterShiftColor;
        }
        else if (hasDefaultColor)
        {
            fogComponent.color.value = defaultFogColor;
        }
    }
}
