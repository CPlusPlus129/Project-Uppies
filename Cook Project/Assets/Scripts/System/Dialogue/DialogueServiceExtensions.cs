using System;
using Cysharp.Threading.Tasks;
using DialogueModule;

public static class DialogueServiceExtensions
{
    /// <summary>
    /// Plays a dialogue event asset via the provided service.
    /// </summary>
    public static UniTask PlayDialogueAsync(this IDialogueService service, DialogueEventAsset dialogueEventAsset)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (dialogueEventAsset == null)
        {
            throw new ArgumentNullException(nameof(dialogueEventAsset));
        }

        if (string.IsNullOrWhiteSpace(dialogueEventAsset.Label))
        {
            throw new InvalidOperationException("DialogueEventAsset does not contain a valid label.");
        }

        var label = dialogueEventAsset.Label;
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new InvalidOperationException("DialogueEventAsset does not contain a valid label.");
        }

        if (!label.StartsWith("*"))
        {
            label = "*" + label;
        }

        return service.StartDialogueAsync(label);
    }
}
