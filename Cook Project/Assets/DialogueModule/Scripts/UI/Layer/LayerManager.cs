using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueModule
{
    class LayerManager : MonoBehaviour, IScenarioBindable
    {
        [SerializeField] private LayerItem bgLayer;
        [SerializeField] private LayerItem defaultLayer;
        [SerializeField] private Image bgImage;
        [SerializeField] private CharacterObject characterObjectPrefab;
        private Dictionary<string, LayerItem> layerItemDict = new Dictionary<string, LayerItem>(); // doesn't include bg layer
        private List<LayerItem> layerItems = new List<LayerItem>();

        public void BindToScenario(ScenarioUIAdapter adapter)
        {
            adapter.onInit += Init;
            adapter.onEndScenario += OnEndScenario;
            adapter.characterAdapter.OnLayerChanged += OnCharacterLayerEvent;
        }

        public void UnbindFromScenario(ScenarioUIAdapter adapter)
        {
            adapter.onInit -= Init;
            adapter.onEndScenario -= OnEndScenario;
            adapter.characterAdapter.OnLayerChanged -= OnCharacterLayerEvent;
        }

        public void Init(InitData initData)
        {
            characterObjectPrefab.gameObject.SetActive(false);
            bgImage.gameObject.SetActive(false); // Not supporting bg for now

            var layerSettingDatas = initData.layerSettingDatas;
            layerItemDict[LayerSettings.DEFAULT_LAYER_NAME] = defaultLayer;
            layerItems.Add(defaultLayer);
            foreach (var d in layerSettingDatas)
            {
                var newItem = Instantiate(defaultLayer, defaultLayer.transform.parent);
                newItem.Init(d);
                layerItemDict[d.layerName] = newItem;
                layerItems.Add(newItem);
            }
            UpdateLayerOrders();
        }

        public void UpdateLayerOrders()
        {
            layerItems.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

            for (int i = 0; i < layerItems.Count; i++)
                layerItems[i].transform.SetSiblingIndex(i);
        }

        private void OnCharacterLayerEvent(CharacterLayerEvent e)
        {
            if (string.IsNullOrEmpty(e.Data.LayerName) || e.Data.LayerName == "None")
                return;

            if (!layerItemDict.TryGetValue(e.Data.LayerName, out var layer))
            {
                Debug.LogWarning($"Trying to execute character event on unknown layer {e.Data.LayerName}!");
                return;
            }
            var cObj = layer.GetCharacterObject(e.Data.SettingData.characterID);
            switch (e.EventType)
            {
                case CharacterLayerEventType.Show:
                    if (cObj == null)
                    {
                        cObj = CreateCharacterObject(e.Data, layer.transform);
                        cObj.transform.SetParent(layer.transform);
                    }
                    else
                        cObj.gameObject.SetActive(true);
                    break;
                case CharacterLayerEventType.Hide:
                    if (cObj != null)
                    {
                        cObj.Reset();
                        Destroy(cObj.gameObject);
                    }
                    break;
                default:
                    break;
            }
        }

        private CharacterObject CreateCharacterObject(CharacterLayerData data, Transform parent)
        {
            var obj = Instantiate(characterObjectPrefab, parent);
            obj.Setup(data);
            obj.gameObject.SetActive(true);
            return obj;
        }

        private void OnEndScenario()
        {
            foreach (var layer in layerItems)
            {
                for (int i = layer.transform.childCount - 1; i >= 0; i--)
                {
                    var child = layer.transform.GetChild(i);
                    if (child != characterObjectPrefab.transform)
                    {
                        var cObj = child.GetComponent<CharacterObject>();
                        cObj?.Reset();
                        Destroy(child.gameObject);
                    }
                }
            }
        }
    }
}