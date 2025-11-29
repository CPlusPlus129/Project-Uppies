using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class CookingStation : InteractableBase
{
    private IShiftSystem shiftSystem;
    private Collider interactCollider;
    private readonly CompositeDisposable disposables = new CompositeDisposable();

    protected override void Awake()
    {
        base.Awake();
        CacheColliderReference();
        InitializeAsync().Forget();
    }

    private void CacheColliderReference()
    {
        if (interactCollider == null)
        {
            interactCollider = GetComponent<Collider>();
        }
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();

        // Subscribe to shift state changes to disable interactable in AfterShift
        shiftSystem.currentState.Subscribe(OnShiftStateChanged).AddTo(disposables);
    }

    private void OnShiftStateChanged(ShiftSystem.ShiftState state)
    {
        // Disable this interactable when in AfterShift state
        bool shouldBeEnabled = state != ShiftSystem.ShiftState.AfterShift;
        enabled = shouldBeEnabled;
        
        // Also disable the collider when in AfterShift state
        if (interactCollider != null)
        {
            interactCollider.enabled = shouldBeEnabled;
        }
    }

    public override void Interact()
    {
        UIRoot.Instance.GetUIComponent<CookingUI>().Open();
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
    }
}
