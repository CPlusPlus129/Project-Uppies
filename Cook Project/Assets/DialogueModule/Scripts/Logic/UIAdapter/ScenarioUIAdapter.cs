using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    public class ScenarioUIAdapter : MonoBehaviour
    {
        public event Action<InitData> onInit;
        public event Action<MessageData> onPlayText;
        public event Action onSkipTypingText;
        public event Action onNextLine;
        public event Action onStartScenario;
        public event Action onEndScenario;
        public event Action<List<SelectionData>, Action<string>> onShowSelections;
        public event Action<bool> onSetClickHandler;
        public readonly ObservableValue<string> currentLine = new ObservableValue<string>();
        public readonly CharacterAdapter characterAdapter = new CharacterAdapter();
        private List<SelectionData> selections = new List<SelectionData>();
        private ControllerStatus controllerStatus = ControllerStatus.None;

        public void Init(InitData initData)
        {
            onInit?.Invoke(initData);
        }

        public void PlayText(string characterDisplayName, string fullText, AudioClip voiceClip, float voiceSpeedMultiplier)
        {
            currentLine.Value = fullText;
            controllerStatus = ControllerStatus.TypingText;
            onPlayText?.Invoke(new MessageData()
            {
                name = characterDisplayName,
                message = fullText,
                voiceClip = voiceClip,
                voiceSpeedMultiplier = voiceSpeedMultiplier
            });
        }

        public void PlayTextEnd()
        {
            controllerStatus = ControllerStatus.WaitingInput;
        }

        public void OnNext()
        {
            if (controllerStatus == ControllerStatus.TypingText)
                onSkipTypingText?.Invoke();
            else if (controllerStatus == ControllerStatus.WaitingInput)
            {
                controllerStatus = ControllerStatus.None;
                onNextLine?.Invoke();
            }
        }

        public void StartScenario()
        {
            onStartScenario?.Invoke();
        }

        public void EndScenario() 
        {
            controllerStatus = ControllerStatus.None;
            onEndScenario?.Invoke();
        }

        internal void AddSelection(string jumpLabel, string textContent)
        {
            selections.Add(new SelectionData()
            {
                jumpLabel = jumpLabel,
                textContent = textContent
            });
        }

        public void ShowSelections()
        {
            var list = new List<SelectionData>(selections);
            selections.Clear();
            onShowSelections?.Invoke(list, ChooseSelection);
            onSetClickHandler?.Invoke(false);
        }

        public void ChooseSelection(string jumpLabel)
        {
            var engine = GetComponent<DialogueEngine>();
            var labelData = engine.dataManager.GetLabelData(jumpLabel);
            if (labelData == null)
            {
                Debug.LogError($"Failed to find jump label name: {jumpLabel}!");
                return;
            }
            engine.scenarioManager.SetNextLabel(labelData);
            onSetClickHandler?.Invoke(true);
            onNextLine?.Invoke();
        }

        enum ControllerStatus
        {
            None,
            TypingText,
            WaitingInput,
        }
    }
}
