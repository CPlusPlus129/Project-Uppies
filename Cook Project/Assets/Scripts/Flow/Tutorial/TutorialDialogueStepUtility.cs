using System;
using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;

public static class TutorialDialogueStepUtility
{
    /// <summary>
    /// Plays up to two dialogue events and waits for a trigger between them.
    /// </summary>
    public static async UniTask PlayDialogueWithTriggerAsync(
        IDialogueService dialogueService,
        DialogueEventAsset beforeTrigger,
        TriggerZone triggerZone,
        DialogueEventAsset afterTrigger)
    {
        if (dialogueService == null)
        {
            throw new ArgumentNullException(nameof(dialogueService));
        }

        if (beforeTrigger != null)
        {
            await dialogueService.PlayDialogueAsync(beforeTrigger);
        }

        if (triggerZone != null)
        {
            await WaitForTriggerAsync(triggerZone);
        }

        if (afterTrigger != null)
        {
            await dialogueService.PlayDialogueAsync(afterTrigger);
        }
    }

    /// <summary>
    /// Waits until the supplied trigger zone fires once.
    /// </summary>
    public static UniTask WaitForTriggerAsync(TriggerZone triggerZone)
    {
        if (triggerZone == null)
        {
            throw new ArgumentNullException(nameof(triggerZone));
        }

        var tcs = new UniTaskCompletionSource();
        IDisposable disposable = null;
        disposable = triggerZone.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                disposable?.Dispose();
                tcs.TrySetResult();
            });

        return tcs.Task;
    }
}
