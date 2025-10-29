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
        [SerializeField]
        private List<CanvasGroup> layersToDim = new List<CanvasGroup>();
        [SerializeField, Range(0f, 1f)]
        private float dialogueOpacity = 0f;
        [SerializeField]
        private bool disableInteractionDuringDialogue = true;
        [SerializeField]
        private bool disableRaycastsDuringDialogue = true;

        private bool _isInited = false;
        private readonly List<IScenarioBindable> _bindables = new List<IScenarioBindable>();
        private readonly Dictionary<CanvasGroup, LayerState> _layerStateCache = new Dictionary<CanvasGroup, LayerState>();
        private bool _layersDimmed = false;

        private struct LayerState
        {
            public float alpha;
            public bool interactable;
            public bool blocksRaycasts;
        }

        protected void Awake()
        {
            Init();
        }

        protected void OnDestroy()
        {
            engine.uiHasInit = false;
            UnbindSelf();
            Unbind();
            RestoreLayerStates();
        }

        public void Init()
        {
            if (_isInited) return;
            _isInited = true;
            SetEngine();
            BindSelf();
            CacheBindables();
            CacheLayerStates();
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
            RestoreLayerStates();
        }

        private void OnStartScenario()
        {
            gameObject.SetActive(true);
            ApplyDialogueLayerStates();
        }

        private void OnEndScenario()
        {
            gameObject.SetActive(false);
            RestoreLayerStates();
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

        private void CacheLayerStates()
        {
            _layerStateCache.Clear();
            foreach (var group in layersToDim)
            {
                if (group == null)
                {
                    continue;
                }

                if (_layerStateCache.ContainsKey(group))
                {
                    continue;
                }

                _layerStateCache[group] = new LayerState
                {
                    alpha = group.alpha,
                    interactable = group.interactable,
                    blocksRaycasts = group.blocksRaycasts
                };
            }
        }

        private void ApplyDialogueLayerStates()
        {
            if (_layersDimmed)
            {
                return;
            }

            foreach (var group in layersToDim)
            {
                if (group == null)
                {
                    continue;
                }

                if (!_layerStateCache.ContainsKey(group))
                {
                    _layerStateCache[group] = new LayerState
                    {
                        alpha = group.alpha,
                        interactable = group.interactable,
                        blocksRaycasts = group.blocksRaycasts
                    };
                }

                group.alpha = dialogueOpacity;

                if (disableInteractionDuringDialogue)
                {
                    group.interactable = false;
                }

                if (disableRaycastsDuringDialogue)
                {
                    group.blocksRaycasts = false;
                }
            }

            _layersDimmed = true;
        }

        private void RestoreLayerStates()
        {
            if (!_layersDimmed)
            {
                return;
            }

            foreach (var kvp in _layerStateCache)
            {
                var group = kvp.Key;
                var state = kvp.Value;
                if (group == null)
                {
                    continue;
                }

                group.alpha = state.alpha;
                group.interactable = state.interactable;
                group.blocksRaycasts = state.blocksRaycasts;
            }

            _layersDimmed = false;
        }
    }
}
