using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    class SelectionManager : MonoBehaviour, IScenarioBindable
    {
        [SerializeField] private SelectionItem itemPrefab;
        private List<SelectionItem> selectionItems = new List<SelectionItem>();

        private void Awake()
        {
            itemPrefab.gameObject.SetActive(false);
        }

        public void BindToScenario(ScenarioUIAdapter adapter)
        {
            adapter.onEndScenario += OnEndScenario;
            adapter.onShowSelections += UpdateItems;
        }

        public void UnbindFromScenario(ScenarioUIAdapter adapter)
        {
            adapter.onEndScenario -= OnEndScenario;
            adapter.onShowSelections -= UpdateItems;
        }

        private void ClearAll()
        {
            foreach (var item in selectionItems)
            {
                GameObject.Destroy(item.gameObject);
            }
            selectionItems.Clear();
        }

        public void UpdateItems(List<SelectionData> datas, Action<string> chooseAction)
        {
            ClearAll();
            foreach (var d in datas)
            {
                var s = Instantiate(itemPrefab, itemPrefab.transform.parent);
                s.gameObject.SetActive(true);
                s.content.text = d.textContent;
                s.btn.onClick.RemoveAllListeners();
                s.btn.onClick.AddListener(() =>
                {
                    chooseAction?.Invoke(d.jumpLabel);
                    s.btn.onClick.RemoveAllListeners();
                    ClearAll();
                });
                selectionItems.Add(s);
            }
        }

        private void OnEndScenario()
        {
            ClearAll();
        }
    }
}