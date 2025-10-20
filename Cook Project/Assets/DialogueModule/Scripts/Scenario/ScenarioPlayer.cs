using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueModule
{
    class ScenarioPlayer : MonoBehaviour
    {
        DialogueEngine engine;
        public LabelData currentLabelData { get; private set; }
        public LabelData nextLabel { get; set; }
        public bool isPlaying { get; set; }
        public bool isPaused { get; set; }
        public bool isLoading { get; private set; }
        private CommandBase currentCommand;

        internal void Init(DialogueEngine engine)
        {
            this.engine = engine;
            engine.adapter.onNextLine += OnNextLine;
        }

        internal void Clear()
        {
            isPlaying = false;
            StopAllCoroutines();
            engine.adapter.onNextLine -= OnNextLine;
        }

        void StartPlaying()
        {
            isPlaying = true;
            engine.adapter.StartScenario();
        }

        void EndPlaying()
        {
            engine.adapter.EndScenario();
            isPlaying = false;
            currentLabelData = null;
            engine.scenarioManager.EndScenario();
        }

        internal void StartScenario(LabelData labelData)
        {
            if (labelData.commands.Count == 0)
            {
                Debug.Log($"No command found in labelData {labelData.name}, will skip start scenario");
                return;
            }
            StartCoroutine(StartScenarioAsync(labelData));
        }

        IEnumerator StartScenarioAsync(LabelData labelData)
        {
            StartPlaying();
            yield return new WaitUntil(() => !engine.isLoading);
            currentLabelData = labelData;
            do
            {
                yield return PreloadAssets();
                yield return StartLabelData();

                currentLabelData = nextLabel;
                nextLabel = null;
            } while (currentLabelData != null);

            EndPlaying();
        }

        IEnumerator StartLabelData()
        {
            foreach (var cmd in currentLabelData.commands)
            {
                currentCommand = cmd;
                currentCommand.Execute(engine);
                yield return new WaitUntil(() => !isPaused);
                yield return new WaitUntil(() => !currentCommand.isWaiting);
            }
        }

        IEnumerator PreloadAssets()
        {
            var usingList = new HashSet<string>();
            List<Coroutine> loadTasks = new();
            foreach (var cmd in currentLabelData.commands)
            {
                if (cmd is not CommandCharacter character)
                    continue;

                if (!engine.dataManager.settingDataManager.characterSettings.DataDict
                    .TryGetValue(character.characterId, out var characterSettingData))
                {
                    Debug.LogError($"Could not find character setting for character ID: {character.characterId}");
                    continue;
                }

                if (!string.IsNullOrEmpty(characterSettingData.fileName))
                    usingList.Add(characterSettingData.fileName);
            }
            foreach (var key in engine.assetManager.CurrentUsingAssetDict.Keys.ToArray())
            {
                if (!usingList.Contains(key))
                    engine.assetManager.Release(key);
            }
            foreach (var fileName in usingList)
            {
                if (!engine.assetManager.CurrentUsingAssetDict.ContainsKey(fileName))
                {
                    loadTasks.Add(StartCoroutine(engine.assetManager.LoadCoroutine<Sprite>(fileName))); //only sprite for now
                }
            }

            foreach (var task in loadTasks)
                yield return task;
        }

        void OnNextLine()
        {
            if (currentCommand != null)
                currentCommand.isWaiting = false;
        }

    }
}