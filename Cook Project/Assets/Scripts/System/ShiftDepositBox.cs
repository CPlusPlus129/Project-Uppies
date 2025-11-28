using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("Gameplay/Shift Deposit Box")]
public class ShiftDepositBox : InteractableBase, IInteractionPromptProvider
{
    [SerializeField]
    [Tooltip("Seconds required to deposit the full quota amount.")]
    private float holdDurationForDepositAll = 0.75f;
    [SerializeField]
    [Tooltip("Input Action name to monitor for hold detection.")]
    private string interactActionName = "Interact";
    [SerializeField]
    [Tooltip("Label shown for the deposit prompt.")]
    private string holdPromptText = "Hold to Deposit Money";
    [SerializeField]
    [Tooltip("Control scheme used when looking up binding display strings.")]
    private string promptControlScheme = "keyboard&mouse";
    [SerializeField]
    [Tooltip("Optional collider reference that will be forced into the Interactable layer.")]
    private Collider interactCollider;

    private IShiftSystem shiftSystem;
    private bool isDepositing;
    private float depositRatePerSecond;
    private InputAction interactAction;
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly List<InteractionPromptDefinition> promptCache = new List<InteractionPromptDefinition>(1);

    private void Reset()
    {
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts(allowSingletonCreation: false);
    }

    private void OnValidate()
    {
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts(allowSingletonCreation: false);
    }

    protected override void Awake()
    {
        base.Awake();
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts();
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.quotaAmount.Subscribe(value =>
        {
            if (holdDurationForDepositAll == 0)
                depositRatePerSecond = value;
            else
                depositRatePerSecond = value / holdDurationForDepositAll;
        }).AddTo(this);

        // Get the interact action reference
        interactAction = InputSystem.actions?.FindAction(interactActionName);
        if (interactAction != null)
        {
            interactAction.canceled += OnInteractReleased;
        }

        // Setup continuous deposit logic
        Observable.EveryUpdate()
            .Where(_ => isDepositing && shiftSystem != null && !shiftSystem.HasMetQuota())
            .Subscribe(_ => DepositIncrementally())
            .AddTo(disposables);
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
        if (interactAction != null)
        {
            interactAction.canceled -= OnInteractReleased;
        }
    }

    public override void Interact()
    {
        if (shiftSystem == null || shiftSystem.HasMetQuota())
            return;

        isDepositing = true;
    }

    private void OnInteractReleased(InputAction.CallbackContext _)
    {
        isDepositing = false;
    }

    private void DepositIncrementally()
    {
        if (shiftSystem == null)
            return;

        // Calculate amount to deposit this frame
        // This might not be accurate, but is sufficient for our needs
        int amountThisFrame = Mathf.CeilToInt(depositRatePerSecond * Time.deltaTime);

        if (amountThisFrame > 0)
        {
            shiftSystem.TryDeposit(amountThisFrame);
        }

        // Stop depositing if quota is met
        if (shiftSystem.HasMetQuota())
        {
            isDepositing = false;
        }
    }


    private void CacheColliderReference()
    {
        if (interactCollider == null)
        {
            interactCollider = GetComponent<Collider>();
        }

        if (interactCollider == null)
        {
            interactCollider = gameObject.AddComponent<BoxCollider>();
        }

        interactCollider.isTrigger = false;
    }

    private void EnsureInteractableLayer()
    {
        var interactLayer = LayerMask.NameToLayer("Interactable");
        if (interactLayer >= 0 && gameObject.layer != interactLayer)
        {
            gameObject.layer = interactLayer;
        }
    }

    private void RefreshPrompts(bool allowSingletonCreation = true)
    {
        promptCache.Clear();
        var holdLabel = string.IsNullOrWhiteSpace(holdPromptText) ? "Hold to Deposit Money" : holdPromptText;
        promptCache.Add(new InteractionPromptDefinition
        {
            actionName = interactActionName,
            controlScheme = promptControlScheme,
            customText = holdLabel
        });
    }

    public IReadOnlyList<InteractionPromptDefinition> GetPrompts() => promptCache;
}
