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
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        int operationId = ++currentOperationId;
        gameObject.SetActive(true);

        animator.SetTrigger("enter");

        try
        {
            await UniTask.WaitUntil(
                () => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f &&
                      !animator.IsInTransition(0),
                cancellationToken: cancellationTokenSource.Token
            );

            if (operationId == currentOperationId)
            {
                OnOpenComplete.OnNext(Unit.Default);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTask CloseAsync()
    {
        int operationId = ++currentOperationId;
        animator.SetTrigger("exit");

        try
        {
            // wait until exit animation is complete
            await UniTask.WaitUntil(
                () => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f &&
                      !animator.IsInTransition(0),
                cancellationToken: cancellationTokenSource.Token
            );

            // check if there is a new open call
            if (operationId == currentOperationId)
            {
                gameObject.SetActive(false);

                OnCloseComplete.OnNext(Unit.Default);
            }
        }
        catch (OperationCanceledException) { }
    }
}