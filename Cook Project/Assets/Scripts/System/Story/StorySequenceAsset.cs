using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StorySequence", menuName = "Game Flow/Sequences/Story Sequence")]
public class StorySequenceAsset : ScriptableObject
{
    [SerializeField]
    [Tooltip("Optional identifier to reference this sequence at runtime.")]
    private string sequenceId;

    [SerializeField]
    [Tooltip("Ordered list of story events that compose this sequence.")]
    private List<StoryEventAsset> events = new List<StoryEventAsset>();

    [SerializeField]
    [Tooltip("Optional story sequence automatically enqueued after this one completes.")]
    private StorySequenceAsset nextSequence;

    [SerializeField]
    [Tooltip("When enabled, GameFlow will log when each event in this sequence begins and ends.")]
    private bool logEventLifecycle = false;

    public string SequenceId => string.IsNullOrWhiteSpace(sequenceId) ? name : sequenceId;
    public IReadOnlyList<StoryEventAsset> Events => events;
    public StorySequenceAsset NextSequence => nextSequence;
    public bool LogEventLifecycle => logEventLifecycle;

    public IEnumerable<StoryEventAsset> EnumerateEvents()
    {
        for (int i = 0; i < events.Count; i++)
        {
            if (events[i] != null)
            {
                yield return events[i];
            }
        }
    }
}
