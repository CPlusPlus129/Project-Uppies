using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class StoryEventAsset : ScriptableObject
{
    [SerializeField]
    [Tooltip("Unique identifier for this event. Defaults to the asset name if left blank.")]
    private string eventId;

    [SerializeField]
    [TextArea]
    [Tooltip("Optional notes to help designers understand what this event does.")]
    private string designerNotes;

    [SerializeField]
    [Tooltip("If false, the event will only run once. Subsequent attempts are skipped while keeping the first result.")]
    private bool replayable = true;

    public string EventId => string.IsNullOrWhiteSpace(eventId) ? name : eventId;
    public string DesignerNotes => designerNotes;
    public bool IsReplayable => replayable;

    public virtual UniTask<bool> CanExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        return UniTask.FromResult(true);
    }

    public abstract UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            eventId = name;
        }
    }
#endif
}
