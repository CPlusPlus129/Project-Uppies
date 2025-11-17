using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;

/// <summary>
/// MessageBox Manager
/// Sends show requests to UI via EventSystem
/// Receives completion results directly from UI
/// </summary>
public class MessageBoxManager : IMessageBoxManager
{
    public Subject<MessageBoxShowRequest> OnShowRequested { get; private set; } = new Subject<MessageBoxShowRequest>();
    private CompositeDisposable disposables = new CompositeDisposable();
    private Queue<UniTaskCompletionSource<int>> pendingRequests = new Queue<UniTaskCompletionSource<int>>();

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// Called directly by UI to notify completion with result
    /// </summary>
    public void NotifyCompletion(int result)
    {
        if (pendingRequests.Count > 0)
        {
            var completionSource = pendingRequests.Dequeue();
            completionSource.TrySetResult(result);
        }
    }

    /// <summary>
    /// Show notification without buttons (auto-close)
    /// </summary>
    public async UniTask<int> ShowNotificationAsync(string message, float duration = 2f)
    {
        return await ShowDialogInternalAsync(message, new string[0], duration);
    }

    /// <summary>
    /// Show message with buttons
    /// </summary>
    public async UniTask<int> ShowDialogAsync(string message, params string[] buttonTexts)
    {
        if (buttonTexts.Length == 0)
        {
            Debug.LogError("ShowDialogAsync requires at least 1 button. Use ShowNotificationAsync(message, duration) for auto-close.");
            return -1;
        }

        return await ShowDialogInternalAsync(message, buttonTexts, duration: 0f);
    }

    private async UniTask<int> ShowDialogInternalAsync(string message, string[] buttonTexts, float duration = 0f)
    {
        // Create completion source for this request
        var completionSource = new UniTaskCompletionSource<int>();
        pendingRequests.Enqueue(completionSource);

        // Send show request to UI
        var request = new MessageBoxShowRequest
        {
            Message = message,
            ButtonTexts = buttonTexts,
            AutoCloseDuration = duration
        };

        OnShowRequested.OnNext(request);

        // Wait for result from UI
        int result = await completionSource.Task;

        return result;
    }

    /// <summary>
    /// Show OK dialog
    /// </summary>
    public async UniTask ShowOkAsync(string message)
    {
        await ShowDialogAsync(message, "OK");
    }

    /// <summary>
    /// Show Yes/No dialog, returns true if "Yes" selected
    /// </summary>
    public async UniTask<bool?> ShowYesNoAsync(string message)
    {
        int result = await ShowDialogAsync(message, "Yes", "No");
        return result switch
        {
            0 => true,    // Yes
            1 => false,   // No
            -1 => null,   // Background click (null)
            _ => null     // Default case
        };
    }

    /// <summary>
    /// Show confirmation dialog (customizable buttons)
    /// </summary>
    public async UniTask<bool?> ShowConfirmAsync(string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        int result = await ShowDialogAsync(message, confirmText, cancelText);
        return result switch
        {
            0 => true,    // Yes
            1 => false,   // No
            -1 => null,   // Background click (null)
            _ => null     // Default case
        };
    }

    /// <summary>
    /// Show multiple choice dialog
    /// </summary>
    public async UniTask<int> ShowChoiceAsync(string message, params string[] choices)
    {
        return await ShowDialogAsync(message, choices);
    }

    public void Dispose()
    {
        OnShowRequested.OnCompleted();
    }
}