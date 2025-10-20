using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueModule
{
    [AddComponentMenu("Dialogue Module/Dialogue Engine")]
    public class DialogueEngine : MonoBehaviour
    {
        internal DataManager dataManager => GetComponent<DataManager>();
        internal ScenarioManager scenarioManager => GetComponent<ScenarioManager>();
        public ScenarioUIAdapter adapter => GetComponent<ScenarioUIAdapter>();
        public IDialogueAssetManager assetManager = new AssetManager();
        public bool isLoading => scenarioManager.isLoading;
        internal bool uiHasInit { get; set; } = false;
        private bool _isInited = false;

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (_isInited) return;
            _isInited = true;
            StartCoroutine(_Init());
        }

        protected IEnumerator _Init()
        {
            dataManager.Init();
            yield return new WaitUntil(() => uiHasInit);
            adapter.Init(PrepareExposedInitData());
        }

        public void StartDialogue(string label)
        {
            if (label.Length > 1 && label[0] == '*')
            {
                label = label.Substring(1);
            }
            scenarioManager.StartScenario(label);
        }

        private InitData PrepareExposedInitData()
        {
            var result = new InitData();
            var layerSettings = dataManager.settingDataManager.layerSettings;
            result.DEFAULT_LAYER_NAME = LayerSettings.DEFAULT_LAYER_NAME;
            result.layerSettingDatas = layerSettings.DataDict.Values.ToList();

            return result;
        }
    }
}