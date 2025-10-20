using UnityEngine;
using UnityEngine.EventSystems;

namespace DialogueModule
{
    class ClickHandler : MonoBehaviour, IPointerClickHandler, IScenarioBindable
    {
        ScenarioUIAdapter adapter;

        public void BindToScenario(ScenarioUIAdapter adapter)
        {
            this.adapter = adapter;
            adapter.onSetClickHandler += SetSelf;
        }

        public void UnbindFromScenario(ScenarioUIAdapter adapter)
        {
            this.adapter = null;
            adapter.onSetClickHandler -= SetSelf;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            adapter.OnNext();
        }

        private void SetSelf(bool isOn)
        {
            gameObject.SetActive(isOn);
        }
    }
}
