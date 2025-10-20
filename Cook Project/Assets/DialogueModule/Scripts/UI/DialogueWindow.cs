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
        private ScenarioUIAdapter adapter;
        private int visibleCharacterCount = 0;
        private bool isTyping = false;
        private float typeWaitTime = 0.01f;

        private void Awake()
        {
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
        }

        private void OnNewText(MessageData data)
        {
            if (!string.IsNullOrEmpty(data.name))
            {
                nameText.text = data.name;
                nameTextRoot.SetActive(true);
            }
            else
                nameTextRoot.SetActive(false);

            contentText.text = data.message;
            visibleCharacterCount = 0;
            contentText.maxVisibleCharacters = 0;
            StopAllCoroutines();
            StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            isTyping = true;

            while (visibleCharacterCount < contentText.text.Length)
            {
                visibleCharacterCount++;
                contentText.maxVisibleCharacters = visibleCharacterCount;
                UpdateIconActiveAndPosition();
                yield return new WaitForSeconds(typeWaitTime);
            }

            isTyping = false;
            UpdateIconActiveAndPosition();
            adapter?.PlayTextEnd();
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
            visibleCharacterCount = contentText.text.Length;
            contentText.maxVisibleCharacters = visibleCharacterCount;
            isTyping = false;
            UpdateIconActiveAndPosition();
            adapter?.PlayTextEnd();
        }

    }
}