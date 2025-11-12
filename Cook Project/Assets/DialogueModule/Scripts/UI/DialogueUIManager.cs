using System.Collections;
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
        [SerializeField, Min(0f)]
        private float fadeDuration = 0.25f;

        private bool _isInited = false;
        private readonly List<IScenarioBindable> _bindables = new List<IScenarioBindable>();
        private readonly Dictionary<CanvasGroup, LayerState> _layerStateCache = new Dictionary<CanvasGroup, LayerState>();
        private readonly Dictionary<CanvasGroup, Coroutine> _layerCoroutines = new Dictionary<CanvasGroup, Coroutine>();
        private bool _layersDimmed = false;
        private Coroutine _pendingDeactivate;

        private struct LayerState
        {
            public float alpha;
            public bool interactable;
            public bool blocksRaycasts;
            public bool enabled;
        }

        protected void Awake()
        {
            Init();
        }

        protected void OnDestroy()
        {
            engine.uiHasInit = false;
            ForceRestoreLayerStates();
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

        private void UnbindFromEngine()
        {
            UnbindSelf();
            Unbind();
        }

        private void BindSelf()
        {
            engine.adapter.onStartScenario += OnStartScenario;
            engine.adapter.onEndScenario += OnEndScenario;
            engine.OnEngineDestroy += UnbindFromEngine;
        }

        private void UnbindSelf()
        {
            engine.adapter.onStartScenario -= OnStartScenario;
            engine.adapter.onEndScenario -= OnEndScenario;
            engine.OnEngineDestroy -= UnbindFromEngine;
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
            if (_pendingDeactivate != null)
            {
                StopCoroutine(_pendingDeactivate);
                _pendingDeactivate = null;
            }

            gameObject.SetActive(true);
            ApplyDialogueLayerStates();
        }

        private void OnEndScenario()
        {
            RestoreLayerStates();
            if (_pendingDeactivate != null)
            {
                StopCoroutine(_pendingDeactivate);
            }
            _pendingDeactivate = StartCoroutine(DeactivateAfterFades());
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
                    blocksRaycasts = group.blocksRaycasts,
                    enabled = group.enabled
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

                StartFade(group,
                    dialogueOpacity,
                    disableInteractionDuringDialogue ? false : _layerStateCache[group].interactable,
                    disableRaycastsDuringDialogue ? false : _layerStateCache[group].blocksRaycasts,
                    true);
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

                StartFade(group, state.alpha, state.interactable, state.blocksRaycasts, state.enabled);
            }

            _layersDimmed = false;
        }

        private void StartFade(CanvasGroup group, float targetAlpha, bool targetInteractable, bool targetBlocksRaycasts, bool targetEnabled)
        {
            if (group == null)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                if (_layerCoroutines.TryGetValue(group, out var pending) && pending != null)
                {
                    StopCoroutine(pending);
                }

                group.enabled = targetEnabled;
                group.interactable = targetInteractable;
                group.blocksRaycasts = targetBlocksRaycasts;
                group.alpha = targetAlpha;
                _layerCoroutines[group] = null;
                return;
            }

            if (_layerCoroutines.TryGetValue(group, out var existing) && existing != null)
            {
                StopCoroutine(existing);
            }

            if (Mathf.Approximately(fadeDuration, 0f))
            {
                group.enabled = targetEnabled;
                group.interactable = targetInteractable;
                group.blocksRaycasts = targetBlocksRaycasts;
                group.alpha = targetAlpha;
                _layerCoroutines[group] = null;
                return;
            }

            IEnumerator FadeRoutine()
            {
                float initialAlpha = group.alpha;
                float elapsed = 0f;
                bool wasEnabled = group.enabled;
                if (!wasEnabled)
                {
                    group.enabled = true;
                }

                group.interactable = targetInteractable;
                group.blocksRaycasts = targetBlocksRaycasts;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    group.alpha = Mathf.Lerp(initialAlpha, targetAlpha, t);
                    yield return null;
                }

                group.alpha = targetAlpha;
                group.enabled = targetEnabled;
                _layerCoroutines[group] = null;
            }

            var coroutine = StartCoroutine(FadeRoutine());
            _layerCoroutines[group] = coroutine;
        }

        private IEnumerator DeactivateAfterFades()
        {
            while (HasActiveFades())
            {
                yield return null;
            }

            gameObject.SetActive(false);
            _pendingDeactivate = null;
        }

        private bool HasActiveFades()
        {
            foreach (var kvp in _layerCoroutines)
            {
                if (kvp.Value != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void ForceRestoreLayerStates()
        {
            if (_pendingDeactivate != null)
            {
                StopCoroutine(_pendingDeactivate);
                _pendingDeactivate = null;
            }

            foreach (var kvp in _layerStateCache)
            {
                var group = kvp.Key;
                var state = kvp.Value;
                if (group == null)
                {
                    continue;
                }

                if (_layerCoroutines.TryGetValue(group, out var existing) && existing != null)
                {
                    StopCoroutine(existing);
                    _layerCoroutines[group] = null;
                }

                group.alpha = state.alpha;
                group.interactable = state.interactable;
                group.blocksRaycasts = state.blocksRaycasts;
                group.enabled = state.enabled;
            }

            _layersDimmed = false;
        }
    }
}
