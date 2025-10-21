using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    public class DialogueUIManager : MonoBehaviour
    {
        [SerializeField]
        private List<MonoBehaviour> bindableSources = new List<MonoBehaviour>();
        [SerializeField]
        private DialogueEngine engine;

        private bool _isInited = false;
        private readonly List<IScenarioBindable> _bindables = new List<IScenarioBindable>();

        protected void Awake()
        {
            Init();
        }

        protected void OnDestroy()
        {
            engine.uiHasInit = false;
            UnbindSelf();
            Unbind();
        }

        public void Init()
        {
            if (_isInited) return;
            _isInited = true;
            SetEngine();
            BindSelf();
            CacheBindables();
            Bind();
            engine.uiHasInit = true;
        }

        private void BindSelf()
        {
            engine.adapter.onStartScenario += OnStartScenario;
            engine.adapter.onEndScenario += OnEndScenario;
        }

        private void UnbindSelf()
        {
            if (engine?.adapter != null)
            {
                engine.adapter.onStartScenario -= OnStartScenario;
                engine.adapter.onEndScenario -= OnEndScenario;
            }
        }

        private void CacheBindables()
        {
            _bindables.Clear();

            foreach (var source in bindableSources)
            {
                if (source == null)
                {
                    Debug.LogWarning($"{name}: Missing reference in bindableSources list.");
                    continue;
                }

                if (source is IScenarioBindable bindable)
                {
                    _bindables.Add(bindable);
                }
                else
                {
                    Debug.LogWarning($"{source.name} does not implement IScenarioBindable.");
                }
            }
        }

        private void Bind()
        {
            foreach (var bindables in _bindables)
            {
                bindables?.BindToScenario(engine.adapter);
            }
        }

        private void Unbind()
        {
            foreach (var bindables in _bindables)
            {
                bindables?.UnbindFromScenario(engine.adapter);
            }
        }

        private void OnStartScenario()
        {
            gameObject.SetActive(true);
        }

        private void OnEndScenario()
        {
            gameObject.SetActive(false);
        }

        private void SetEngine()
        {
            if (engine == null)
            {
                engine = FindFirstObjectByType<DialogueEngine>();
            }
            if (engine == null)
                Debug.LogError("[Dialogue UI] Failed to find Dialogue Engine in the scene!");
        }

        public void SetEngine(DialogueEngine engine)
        {
            this.engine = engine;
        }
    }
}