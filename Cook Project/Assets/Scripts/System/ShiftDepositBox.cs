using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("Gameplay/Shift Deposit Box")]
public class ShiftDepositBox : MonoBehaviour, IInteractable, IInteractionPromptProvider
{
    [SerializeField][Min(1)] private int manualDepositAmount = 50;
    [SerializeField][Tooltip("Seconds the interact button must be held to trigger a full deposit.")]
    private float holdDurationForDepositAll = 0.75f;
    [SerializeField][Tooltip("Input Action name to monitor for hold detection.")]
    private string interactActionName = "Interact";
    [SerializeField][Tooltip("Label shown for the quick deposit prompt. {0} will be replaced with the amount.")]
    private string chunkPromptTemplate = "Deposit ${0}";
    [SerializeField][Tooltip("Label shown for the hold-to-deposit-all prompt.")]
    private string holdPromptText = "Hold to deposit all";
    [SerializeField][Tooltip("Control scheme used when looking up binding display strings.")]
    private string promptControlScheme = "keyboard&mouse";
    [SerializeField][Tooltip("Optional collider reference that will be forced into the Interactable layer.")]
    private Collider interactCollider;
    private IShiftSystem shiftSystem;
    private bool isProcessing;
    private readonly List<InteractionPromptDefinition> promptCache = new List<InteractionPromptDefinition>(2);
    private const int fallbackDepositChunk = 50;

    private void Reset()
    {
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts();
    }

    private async void Awake()
    {
        CacheColliderReference();
        EnsureInteractableLayer();
        RefreshPrompts();
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
    }

    public void Interact()
    {
        if (shiftSystem == null || isProcessing)
            return;

        HandleInteractionAsync().Forget();
    }

    private async UniTaskVoid HandleInteractionAsync()
    {
        isProcessing = true;
        try
        {
            var depositAll = await ShouldDepositAllAsync();
            if (depositAll)
            {
                var deposited = shiftSystem.DepositAllAvailableFunds();
                if (deposited <= 0)
                {
                    WarnPlayer();
                }
            }
            else
            {
                var amount = DetermineDepositAmount();
                if (!shiftSystem.TryDeposit(amount))
                {
                    WarnPlayer();
                }
            }
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async UniTask<bool> ShouldDepositAllAsync()
    {
        if (holdDurationForDepositAll <= 0f)
        {
            return true;
        }

        var action = InputSystem.actions?.FindAction(interactActionName);
        if (action == null)
        {
            return false;
        }

        var token = this.GetCancellationTokenOnDestroy();
        var elapsed = 0f;
        while (action.IsPressed())
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
            if (elapsed >= holdDurationForDepositAll)
            {
                return true;
            }
        }

        return false;
    }

    private int DetermineDepositAmount()
    {
        if (manualDepositAmount > 0)
        {
            return manualDepositAmount;
        }

        var data = Database.Instance?.shiftData;
        if (data == null)
            return fallbackDepositChunk;

        return Mathf.Max(1, data.defaultDepositChunk);
    }

    private void WarnPlayer()
    {
        WorldBroadcastSystem.Instance.Broadcast("No cash to drop off or deposit is locked right now.", 4f);
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

    private void RefreshPrompts()
    {
        promptCache.Clear();
        var chunkAmount = GetDisplayDepositAmount();
        var chunkLabel = string.IsNullOrWhiteSpace(chunkPromptTemplate)
            ? $"Deposit ${chunkAmount}"
            : string.Format(chunkPromptTemplate, chunkAmount);
        promptCache.Add(new InteractionPromptDefinition
        {
            actionName = interactActionName,
            controlScheme = promptControlScheme,
            customText = chunkLabel
        });

        var holdLabel = string.IsNullOrWhiteSpace(holdPromptText) ? "Hold to deposit all" : holdPromptText;
        promptCache.Add(new InteractionPromptDefinition
        {
            actionName = interactActionName,
            controlScheme = promptControlScheme,
            customText = holdLabel
        });
    }

    private int GetDisplayDepositAmount()
    {
        return DetermineDepositAmount();
    }

    public IReadOnlyList<InteractionPromptDefinition> GetPrompts() => promptCache;
}
