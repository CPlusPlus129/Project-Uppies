using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;

/// <summary>
/// MessageBox UI Component
/// - Black background serves as Cancel button
/// - Configurable with 0 or more buttons
/// - When 0 buttons, must specify auto-close duration
/// - Returns selected button index or -1 (background click)
/// </summary>
public class MessageBoxUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private float fadeDuration = 0.2f;

    private IMessageBoxManager manager;
    private List<Button> currentButtons = new List<Button>();
    private UniTaskCompletionSource<int> completionSource;
    private CompositeDisposable disposables;

    public async UniTask Init()
    {
        buttonPrefab.gameObject.SetActive(false);
        manager = await ServiceLocator.Instance.GetAsync<IMessageBoxManager>();
        manager.OnShowRequested
            .Subscribe(request => HandleShowRequest(request))
            .AddTo(this);
    }

    private void OnEnable()
    {
        disposables = new CompositeDisposable();
    }

    private void OnDisable()
    {
        disposables.Dispose();
    }

    /// <summary>
    /// Handle show request from Manager
    /// </summary>
    private void HandleShowRequest(in MessageBoxShowRequest request)
    {
        ShowInternalAsync(request.Message, request.ButtonTexts, request.AutoCloseDuration).Forget();
    }

    private async UniTaskVoid ShowInternalAsync(string message, string[] buttonTexts, float duration = 0f)
    {
        // Clean up old state
        disposables?.Dispose();
        disposables = new CompositeDisposable();

        // Create new completion source
        completionSource = new UniTaskCompletionSource<int>();

        // Set message
        messageText.text = message;

        // Clear old buttons
        foreach (var btn in currentButtons)
        {
            Destroy(btn.gameObject);
        }
        currentButtons.Clear();

        // Setup background button with R3 Observable
        backgroundButton.OnClickAsObservable()
            .Subscribe(_ => CompleteWith(-1))
            .AddTo(disposables);

        // Create new buttons
        if (buttonTexts.Length > 0)
        {
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                int buttonIndex = i; // Capture index correctly
                Button button = Instantiate(buttonPrefab, buttonContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TextMeshProUGUI>().text = buttonTexts[i];

                button.OnClickAsObservable()
                    .Subscribe(_ => CompleteWith(buttonIndex))
                    .AddTo(disposables);

                currentButtons.Add(button);
            }
        }
        else if (duration > 0f)
        {
            // 0 buttons, set auto-close timer
            UniTask.Delay(System.TimeSpan.FromSeconds(duration))
                .ContinueWith(() => CompleteWith(0)).Forget();
        }

        // Show UI (fade in)
        await FadeInAsync();

        // Wait for user interaction or auto-close
        int result = await completionSource.Task;

        // Hide UI (fade out)
        await FadeOutAsync();

        // Call Manager directly to notify completion
        manager.NotifyCompletion(result);
    }

    private void CompleteWith(int result)
    {
        if (completionSource?.Task.Status == UniTaskStatus.Pending)
        {
            completionSource.TrySetResult(result);
        }
    }

    private async UniTask FadeInAsync()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            await UniTask.Yield();
        }

        canvasGroup.alpha = 1f;
    }

    private async UniTask FadeOutAsync()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            await UniTask.Yield();
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}