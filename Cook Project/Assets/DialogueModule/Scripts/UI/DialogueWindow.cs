using System.Collections;
using TMPro;
using UnityEngine;

namespace DialogueModule
{
    class DialogueWindow : MonoBehaviour, IScenarioBindable
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private GameObject ongoingNextIcon;
        [SerializeField] private GameObject waitNextIcon;
        [SerializeField] private GameObject nameTextRoot;
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private DialogueNameCardImage nameCardBackground;

        private const float DefaultTypeWaitTime = 0.01f;
        private const float MinTypeWaitTime = 0.0025f;
        private const float MinClipWaitTime = 0.0005f;

        private ScenarioUIAdapter adapter;
        private int visibleCharacterCount = 0;
        private int targetCharacterCount = 0;
        private bool isTyping = false;
        private float typeWaitTime = DefaultTypeWaitTime;
        private float currentVoiceSpeed = 1f;
        private AudioClip currentVoiceClip;
        private float voicePlaybackCooldown = 0f;

        private void Awake()
        {
            if (voiceAudioSource == null)
            {
                voiceAudioSource = GetComponent<AudioSource>();
                if (voiceAudioSource == null)
                    voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            Clear();
        }

        public void BindToScenario(ScenarioUIAdapter adapter)
        {
            adapter.onPlayText += OnNewText;
            adapter.onSkipTypingText += Skip;
            this.adapter = adapter;
        }

        public void UnbindFromScenario(ScenarioUIAdapter adapter)
        {
            adapter.onPlayText -= OnNewText;
            adapter.onSkipTypingText -= Skip;
            this.adapter = null;
        }

        private void Clear()
        {
            nameText.text = string.Empty;
            contentText.text = string.Empty;
            ongoingNextIcon?.SetActive(false);
            waitNextIcon?.SetActive(false);
            nameTextRoot?.SetActive(false);
            nameCardBackground?.ResetToDefault();
            StopVoicePlayback();
            targetCharacterCount = 0;
            typeWaitTime = DefaultTypeWaitTime;
            currentVoiceSpeed = 1f;
            currentVoiceClip = null;
            voicePlaybackCooldown = 0f;
        }

        private void OnNewText(MessageData data)
        {
            if (!string.IsNullOrEmpty(data.name))
            {
                nameText.text = data.name;
                if (data.hasNameCardColor)
                    nameCardBackground?.SetColor(data.nameCardColor);
                else
                    nameCardBackground?.ResetToDefault();
                nameTextRoot.SetActive(true);
            }
            else
            {
                nameTextRoot.SetActive(false);
                nameCardBackground?.ResetToDefault();
            }

            contentText.text = data.message;
            contentText.maxVisibleCharacters = 0;
            contentText.ForceMeshUpdate();
            visibleCharacterCount = 0;
            targetCharacterCount = contentText.textInfo.characterCount;
            if (targetCharacterCount == 0 && !string.IsNullOrEmpty(data.message))
                targetCharacterCount = data.message.Length;

            StopVoicePlayback();
            currentVoiceClip = data.voiceClip;
            voicePlaybackCooldown = 0f;
            currentVoiceSpeed = data.voiceSpeedMultiplier <= 0f ? 1f : data.voiceSpeedMultiplier;
            typeWaitTime = GetDefaultTypeWaitTime(currentVoiceSpeed);

            if (currentVoiceClip != null && voiceAudioSource != null)
            {
                typeWaitTime = GetTypeWaitTimeForClip(currentVoiceClip, currentVoiceSpeed);
                voiceAudioSource.pitch = currentVoiceSpeed;
            }

            typeWaitTime = Mathf.Max(typeWaitTime, MinClipWaitTime);

            StopAllCoroutines();
            StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            isTyping = true;
            contentText.ForceMeshUpdate();

            if (targetCharacterCount == 0 && !string.IsNullOrEmpty(contentText.text))
            {
                targetCharacterCount = contentText.textInfo.characterCount;
                if (targetCharacterCount == 0)
                    targetCharacterCount = contentText.text.Length;
            }

            float elapsed = 0f;

            while (visibleCharacterCount < targetCharacterCount)
            {
                float delta = Time.deltaTime;
                elapsed += delta;

                if (voicePlaybackCooldown > 0f)
                {
                    voicePlaybackCooldown -= delta;
                    if (voicePlaybackCooldown < 0f)
                        voicePlaybackCooldown = 0f;
                }

                int steps;
                if (typeWaitTime <= Mathf.Epsilon)
                {
                    steps = targetCharacterCount - visibleCharacterCount;
                    elapsed = 0f;
                }
                else
                {
                    steps = Mathf.FloorToInt(elapsed / typeWaitTime);
                    if (steps > 0)
                        elapsed -= steps * typeWaitTime;
                }

                if (steps <= 0)
                {
                    yield return null;
                    continue;
                }

                for (int i = 0; i < steps && visibleCharacterCount < targetCharacterCount; i++)
                {
                    visibleCharacterCount++;
                    contentText.maxVisibleCharacters = visibleCharacterCount;
                    UpdateIconActiveAndPosition();
                    TryPlayVoiceTick(visibleCharacterCount - 1);
                }

                yield return null;
            }

            isTyping = false;
            visibleCharacterCount = targetCharacterCount;
            contentText.maxVisibleCharacters = visibleCharacterCount;
            UpdateIconActiveAndPosition();
            adapter?.PlayTextEnd();
        }

        private void TryPlayVoiceTick(int characterIndex)
        {
            if (voiceAudioSource == null || currentVoiceClip == null)
                return;
            if (voicePlaybackCooldown > 0f)
                return;
            if (characterIndex < 0 || characterIndex >= contentText.textInfo.characterCount)
                return;

            var charInfo = contentText.textInfo.characterInfo[characterIndex];
            if (!charInfo.isVisible || char.IsWhiteSpace(charInfo.character))
                return;

            voiceAudioSource.pitch = currentVoiceSpeed;
            voiceAudioSource.PlayOneShot(currentVoiceClip);
            voicePlaybackCooldown = GetVoicePlaybackInterval(currentVoiceClip, currentVoiceSpeed);
        }

        private void UpdateIconActiveAndPosition()
        {
            if (visibleCharacterCount == 0) return;

            contentText.ForceMeshUpdate();

            int lastIndex = Mathf.Min(visibleCharacterCount - 1, contentText.textInfo.characterCount - 1);
            if (lastIndex < 0 || lastIndex >= contentText.textInfo.characterCount)
                return;

            TMP_CharacterInfo charInfo = contentText.textInfo.characterInfo[lastIndex];
            if (!charInfo.isVisible)
                return;

            if (isTyping)
            {
                ongoingNextIcon?.SetActive(true);
                waitNextIcon?.SetActive(false);
            }
            else
            {
                waitNextIcon?.SetActive(true);
                ongoingNextIcon?.SetActive(false);
            }

            Vector3 worldPos = contentText.transform.TransformPoint(charInfo.topRight);
            if (waitNextIcon != null)
                waitNextIcon.transform.position = worldPos;
            if (ongoingNextIcon != null)
                ongoingNextIcon.transform.position = worldPos;
        }

        private void Skip()
        {
            if (!isTyping) return;

            StopAllCoroutines();
            StopVoicePlayback();
            visibleCharacterCount = targetCharacterCount;
            contentText.maxVisibleCharacters = visibleCharacterCount;
            isTyping = false;
            UpdateIconActiveAndPosition();
            adapter?.PlayTextEnd();
        }

        private void StopVoicePlayback()
        {
            currentVoiceClip = null;
            voicePlaybackCooldown = 0f;
            if (voiceAudioSource == null)
                return;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = null;
            voiceAudioSource.pitch = 1f;
        }

        private float GetTypeWaitTimeForClip(AudioClip clip, float speedMultiplier)
        {
            if (clip == null)
                return GetDefaultTypeWaitTime(speedMultiplier);

            float interval = GetVoicePlaybackInterval(clip, speedMultiplier);
            return Mathf.Max(interval, MinClipWaitTime);
        }

        private float GetDefaultTypeWaitTime(float speedMultiplier)
        {
            float effectiveSpeed = Mathf.Max(speedMultiplier, 0.01f);
            float perCharacter = DefaultTypeWaitTime / effectiveSpeed;
            return Mathf.Max(perCharacter, MinTypeWaitTime);
        }

        private float GetVoicePlaybackInterval(AudioClip clip, float speedMultiplier)
        {
            if (clip == null)
                return 0f;
            return Mathf.Max(clip.length / Mathf.Max(speedMultiplier, 0.01f), MinClipWaitTime);
        }
    }
}
