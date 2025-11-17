using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Threading;
using UnityEngine;

public class UIAnimationController : MonoBehaviour
{
    public Subject<Unit> OnOpenComplete { get; } = new Subject<Unit>();
    public Subject<Unit> OnCloseComplete { get; } = new Subject<Unit>();
    private Animator animator;
    private int currentOperationId = 0;
    private CancellationTokenSource cancellationTokenSource;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        OpenAsync().Forget();
    }

    public void Close()
    {
        CloseAsync().Forget();
    }

    private async UniTask OpenAsync()
    {
        var animatorInstance = EnsureAnimator();
        if (animatorInstance == null)
        {
            return;
        }

        var token = ResetCancellationToken();
        int operationId = ++currentOperationId;
        gameObject.SetActive(true);

        animatorInstance.SetTrigger("enter");

        try
        {
            await WaitForAnimationAsync(animatorInstance, token);

            if (operationId == currentOperationId)
            {
                OnOpenComplete.OnNext(Unit.Default);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTask CloseAsync()
    {
        var animatorInstance = EnsureAnimator();
        if (animatorInstance == null)
        {
            return;
        }

        var token = ResetCancellationToken();
        int operationId = ++currentOperationId;
        animatorInstance.SetTrigger("exit");

        try
        {
            // wait until exit animation is complete
            await WaitForAnimationAsync(animatorInstance, token);

            // check if there is a new open call
            if (operationId == currentOperationId)
            {
                gameObject.SetActive(false);

                OnCloseComplete.OnNext(Unit.Default);
            }
        }
        catch (OperationCanceledException) { }
    }

    private Animator EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[{nameof(UIAnimationController)}] Animator component missing on '{name}'.");
            }
        }

        return animator;
    }

    private CancellationToken ResetCancellationToken()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();
        return cancellationTokenSource.Token;
    }

    private static bool IsAnimatorPlayable(Animator animatorInstance)
    {
        return animatorInstance != null && animatorInstance.isActiveAndEnabled && animatorInstance.runtimeAnimatorController != null;
    }

    private async UniTask WaitForAnimationAsync(Animator animatorInstance, CancellationToken token)
    {
        if (!IsAnimatorPlayable(animatorInstance))
        {
            return;
        }

        await UniTask.WaitUntil(
            () =>
            {
                if (!IsAnimatorPlayable(animatorInstance))
                {
                    return true;
                }

                var state = animatorInstance.GetCurrentAnimatorStateInfo(0);
                return state.normalizedTime >= 1f && !animatorInstance.IsInTransition(0);
            },
            cancellationToken: token);
    }

    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
