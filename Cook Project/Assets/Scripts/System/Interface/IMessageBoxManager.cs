using Cysharp.Threading.Tasks;
using R3;

public interface IMessageBoxManager : IGameService
{
    Subject<MessageBoxShowRequest> OnShowRequested { get; }
    void NotifyCompletion(int result);
    UniTask<int> ShowNotificationAsync(string message, float duration = 2f);
    UniTask<int> ShowDialogAsync(string message, params string[] buttonTexts);
    UniTask ShowOkAsync(string message);
    UniTask<bool?> ShowYesNoAsync(string message);
    UniTask<bool?> ShowConfirmAsync(string message, string confirmText = "OK", string cancelText = "Cancel");
    UniTask<int> ShowChoiceAsync(string message, params string[] choices);
}