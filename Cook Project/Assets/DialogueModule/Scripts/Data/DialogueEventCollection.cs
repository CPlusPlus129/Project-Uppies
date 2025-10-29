using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    /// <summary>
    /// Defines a reusable collection of dialogue events with a preferred playback strategy.
    /// </summary>
    [CreateAssetMenu(menuName = "Dialogue/Event Collection", fileName = "DialogueEventCollection")]
    public class DialogueEventCollection : ScriptableObject
    {
        public enum PlaybackMode
        {
            Sequential,
            Random
        }

        [SerializeField]
        [Tooltip("Ordered list of dialogue events that belong to this collection.")]
        private List<DialogueEventAsset> dialogueEvents = new();

        [SerializeField]
        [Tooltip("Default playback strategy when this collection is consumed by a DialogueEventPlayer.")]
        private PlaybackMode defaultPlaybackMode = PlaybackMode.Sequential;

        [Header("Sequential Settings")]
        [SerializeField]
        [Tooltip("When enabled, sequential playback wraps back to the first entry after reaching the end. If disabled, playback will stay on the final entry until reset or the collection is swapped out.")]
        private bool loopSequential = true;

        [Header("Random Settings")]
        [SerializeField]
        [Tooltip("Avoid repeating the same event twice in a row when using random playback.")]
        private bool avoidImmediateRepeat = true;

        [SerializeField]
        [Tooltip("When enabled, random playback will cycle through every entry before starting a new shuffle.")]
        private bool exhaustBeforeRepeat = false;

        [Header("General Options")]
        [SerializeField]
        [Tooltip("Allow the DialogueEventPlayer to call RepeatLast on this collection even if it is managed randomly/sequentially.")]
        private bool allowRepeatLast = true;

        public IReadOnlyList<DialogueEventAsset> DialogueEvents => dialogueEvents;
        public PlaybackMode DefaultPlayback => defaultPlaybackMode;
        public bool LoopSequential => loopSequential;
        public bool AvoidImmediateRepeat => avoidImmediateRepeat;
        public bool ExhaustBeforeRepeat => exhaustBeforeRepeat;
        public bool AllowRepeatLast => allowRepeatLast;

        public bool IsEmpty => dialogueEvents == null || dialogueEvents.Count == 0;
    }
}
