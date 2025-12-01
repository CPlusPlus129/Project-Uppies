using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEngine.EventSystems;

namespace UI
{
    public class EndingScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private UIAnimationController uiAnimation;

        private UniTaskCompletionSource _continueCompletionSource;

        private void Awake()
        {
            if (continueButton == null)
            {
                // Auto-resolve for easier setup
                var btnTransform = transform.Find("ContinueButton");
                if (btnTransform != null) continueButton = btnTransform.GetComponent<Button>();
            }

            if (returnToTitleButton == null)
            {
                // Auto-resolve for easier setup
                var btnTransform = transform.Find("ReturnToTitleButton");
                if (btnTransform != null) returnToTitleButton = btnTransform.GetComponent<Button>();
            }

            if (uiAnimation == null)
            {
                uiAnimation = GetComponent<UIAnimationController>();
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
            }

            // Ensure it's hidden by default if needed, though usually handled by UIAnimationController
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _continueCompletionSource = new UniTaskCompletionSource();
            
            if (uiAnimation != null)
            {
                uiAnimation.Open();
            }

            // Ensure input is grabbed for controller/keyboard navigation
            // We wait a frame to ensure the GameObject is fully active and registered by the EventSystem
            SelectButtonLater().Forget();
        }

        private async UniTaskVoid SelectButtonLater()
        {
            // Wait for end of frame or next update to ensure UI is ready
            await UniTask.Yield(PlayerLoopTiming.Update);
            
            if (continueButton != null && EventSystem.current != null)
            {
                // Clearing selection first sometimes helps force the highlight update
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            }
        }

        public async UniTask WaitForContinueAsync()
        {
            if (_continueCompletionSource == null)
            {
                return;
            }

            await _continueCompletionSource.Task;
        }

        private void OnContinueClicked()
        {
            if (uiAnimation != null)
            {
                uiAnimation.Close();
            }
            else
            {
                gameObject.SetActive(false);
            }

            _continueCompletionSource?.TrySetResult();
        }

        private void OnReturnToTitleClicked()
        {
            // 1. Reset Input to UI for Title Screen
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetActionMap("UI");
            }

            // 2. Clean up GameFlow state
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.ClearStoryQueue();
                GameFlow.Instance.ClearHistory();
            }

            // 3. Resolve the wait task so the StoryEvent finishes cleanly
            _continueCompletionSource?.TrySetResult();

            // 4. Load the Title scene via Service to ensure UI updates correctly
            ReturnToTitleAsync().Forget();
        }

        private async UniTaskVoid ReturnToTitleAsync()
        {
            var sceneService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
            if (sceneService != null)
            {
                await sceneService.LoadSceneAsync("Title");
            }
            else
            {
                Debug.LogWarning("SceneManagementService not found, falling back to SceneManager");
                SceneManager.LoadScene("Title");
            }
        }
    }
}
