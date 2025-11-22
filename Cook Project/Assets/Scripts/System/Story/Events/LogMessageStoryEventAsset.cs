using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "LogMessageEvent", menuName = "Game Flow/Utility/Log Message")]
public class LogMessageStoryEventAsset : StoryEventAsset
{
    [SerializeField, TextArea]
    [Tooltip("The message to log to the console.")]
    private string message;

    [SerializeField]
    [Tooltip("Log as a warning (yellow).")]
    private bool logAsWarning;

    [SerializeField]
    [Tooltip("Log as an error (red).")]
    private bool logAsError;

    public override UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (logAsError)
        {
            Debug.LogError($"[Story] {message}");
        }
        else if (logAsWarning)
        {
            Debug.LogWarning($"[Story] {message}");
        }
        else
        {
            Debug.Log($"[Story] {message}");
        }

        return UniTask.FromResult(StoryEventResult.Completed($"Logged: {message}"));
    }
}
