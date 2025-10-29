using UnityEngine;

namespace DialogueModule
{
    /// <summary>
    /// Scriptable definition for a dialogue moment that can be reused across scenes.
    /// </summary>
    [CreateAssetMenu(menuName = "Dialogue/Event Asset", fileName = "DialogueEvent")]
    public class DialogueEventAsset : ScriptableObject
    {
        [SerializeField]
        private string label;

        [SerializeField]
        [Tooltip("Optional designer note shown in the inspector only.")]
        private string notes;

        [SerializeField]
        [TextArea]
        [Tooltip("Optional hint or subtitle text that systems can surface alongside this dialogue.")]
        private string hint;

        [SerializeField]
        [Tooltip("If enabled, event players will only allow this dialogue to trigger once per instance.")]
        private bool playOnce = true;

        /// <summary>
        /// Dialogue label passed to the dialogue service.
        /// </summary>
        public string Label => label;

        /// <summary>
        /// Optional tutorial or helper hint text.
        /// </summary>
        public string Hint => hint;

        /// <summary>
        /// Free-form notes for designers.
        /// </summary>
        public string Notes => notes;

        /// <summary>
        /// Suggests whether the dialogue should only play once per scene instance.
        /// </summary>
        public bool PlayOnce => playOnce;
    }
}
