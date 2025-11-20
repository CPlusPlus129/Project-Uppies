using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
#endif

public interface IBackgroundStoryEvent
{
    /// <summary>
    /// If true, the GameFlow queue will not block while this event executes; it will run concurrently.
    /// </summary>
    bool RunInBackground { get; }

    /// <summary>
    /// If true, and RunInBackground is true, this event will prevent other events from the SAME source sequence
    /// from running until this event completes. Events from other sequences (or dynamic events) can still run.
    /// </summary>
    bool BlockSourceSequence { get; }
}

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
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureEventId();
    }

    private void EnsureEventId()
    {
        var trimmedCurrentId = string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId.Trim();
        var usedIds = CollectUsedEventIds(this);

        if (!string.IsNullOrEmpty(trimmedCurrentId) && !usedIds.Contains(trimmedCurrentId))
        {
            if (!string.Equals(eventId, trimmedCurrentId, StringComparison.Ordinal))
            {
                eventId = trimmedCurrentId;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            return;
        }

        var baseId = !string.IsNullOrEmpty(trimmedCurrentId) ? trimmedCurrentId : name;
        var sanitizedBase = SanitizeEventId(baseId);
        var uniqueId = GenerateUniqueEventId(sanitizedBase, usedIds);

        if (!string.Equals(eventId, uniqueId, StringComparison.Ordinal))
        {
            eventId = uniqueId;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    private static HashSet<string> CollectUsedEventIds(StoryEventAsset self)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var usedIds = new HashSet<string>(comparer);
        var guids = UnityEditor.AssetDatabase.FindAssets("t:StoryEventAsset");

        var selfPath = UnityEditor.AssetDatabase.GetAssetPath(self);
        var selfGuid = string.IsNullOrEmpty(selfPath) ? null : UnityEditor.AssetDatabase.AssetPathToGUID(selfPath);

        foreach (var guid in guids)
        {
            if (!string.IsNullOrEmpty(selfGuid) && guid == selfGuid)
            {
                continue;
            }

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryEventAsset>(path);
            if (asset == null)
            {
                continue;
            }

            var id = string.IsNullOrWhiteSpace(asset.eventId) ? asset.name : asset.eventId;
            id = id?.Trim();
            if (!string.IsNullOrEmpty(id))
            {
                usedIds.Add(id);
                var sanitized = SanitizeEventId(id);
                if (!string.IsNullOrEmpty(sanitized))
                {
                    usedIds.Add(sanitized);
                }
            }
        }

        return usedIds;
    }

    private static string SanitizeEventId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "StoryEvent";
        }

        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);

        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (ch == '_' || ch == '-')
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                if (builder.Length == 0 || builder[builder.Length - 1] == '_')
                {
                    continue;
                }
                builder.Append('_');
            }
        }

        var sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "StoryEvent" : sanitized;
    }

    private static string GenerateUniqueEventId(string baseId, HashSet<string> usedIds)
    {
        if (string.IsNullOrEmpty(baseId))
        {
            baseId = "StoryEvent";
        }

        if (!usedIds.Contains(baseId))
        {
            return baseId;
        }

        var index = 1;
        string candidate;
        do
        {
            candidate = $"{baseId}_{index:000}";
            index++;
        }
        while (usedIds.Contains(candidate));

        return candidate;
    }
#endif
}
