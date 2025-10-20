using System;
using UnityEngine;

namespace DialogueModule
{
    class ScenarioManager : MonoBehaviour
    {
        public event Action onBeginScenario;
        public event Action onEndScenario;
        public event Action onPauseScenario;
        public event Action onEndOrPauseScenario;
        public bool isLoading => mainPlayer.isLoading;
        ScenarioPlayer mainPlayer;

        internal void StartScenario(string label)
        {
            ClearMainPlayer();
            CreateMainPlayer();
            var labelData = GetComponent<DataManager>().GetLabelData(label);
            if (labelData == null)
            {
                Debug.LogError($"can't find labelData for label {label}!");
                return;
            }
            onBeginScenario?.Invoke();
            mainPlayer.StartScenario(labelData);
        }

        internal void EndScenario()
        {
            onEndScenario?.Invoke();
            onEndOrPauseScenario?.Invoke();
            ClearMainPlayer();
        }

        internal void Pause()
        {
            if (mainPlayer != null)
                mainPlayer.isPaused = true;
            onPauseScenario?.Invoke();
            onEndOrPauseScenario?.Invoke();
        }

        internal void Resume()
        {
            if (mainPlayer != null)
                mainPlayer.isPaused = false;
        }

        internal void SetNextLabel(LabelData labelData)
        {
            if (mainPlayer != null)
                mainPlayer.nextLabel = labelData;
        }

        private void ClearMainPlayer()
        {
            if (mainPlayer == null)
                return;

            mainPlayer.Clear();
            Destroy(mainPlayer);
            mainPlayer = null;
        }

        private void CreateMainPlayer()
        {
            var go = new GameObject("MainScenarioPlayer");
            var sp = go.AddComponent<ScenarioPlayer>();
            go.transform.parent = transform;
            mainPlayer = sp;
            sp.Init(GetComponent<DialogueEngine>());
        }
    }
}