using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class TitleUI : MonoBehaviour, IUIInitializable
{
    [Header("UI References")]
    public Button startButton;
    public Button exitButton;

    private ISceneManagementService sceneManagementService;
    private IMessageBoxManager messageBoxManager;

    public async UniTask Init()
    {
        sceneManagementService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
        messageBoxManager = await ServiceLocator.Instance.GetAsync<IMessageBoxManager>();

        startButton?.OnClickAsObservable().Subscribe(OnStartButtonClicked).AddTo(this);
        exitButton?.OnClickAsObservable().Subscribe(OnExitButtonClicked).AddTo(this);
    }

    public void OnEnable()
    {
        InputManager.Instance.PushActionMap("Title");
    }

    public void OnDisable()
    {
        InputManager.Instance.PopActionMap("Title");
    }

    private async void OnStartButtonClicked(Unit _)
    {
        var result = await messageBoxManager.ShowYesNoAsync("Do you want to skip tutorial?");
        if (result.HasValue)
        {
            if (result.Value)
                LoadMainScene().Forget();
            else
                LoadIntroScene().Forget();
        }
    }

    private void OnExitButtonClicked(Unit _)
    {
        ExitGame();
    }

    private async UniTaskVoid LoadIntroScene()
    {
        if (sceneManagementService != null)
        {
            var nextSceneName = sceneManagementService.GetNextSceneName();
            if (!string.IsNullOrEmpty(nextSceneName))
                await sceneManagementService.LoadSceneAsync(nextSceneName, null, SceneTransitionType.Fade);
            else
                Debug.LogError("[TitleUI] title scene is the last scene, cannot determine next scene name");
        }
        else
        {
            Debug.LogError("[TitleUI] SceneManagementService is not available!");
        }
    }

    private async UniTaskVoid LoadMainScene()
    {
        if (sceneManagementService != null)
        {
            var nextSceneName = "WhiteBox02";
            if (!string.IsNullOrEmpty(nextSceneName))
                await sceneManagementService.LoadSceneAsync(nextSceneName, null, SceneTransitionType.Fade);
            else
                Debug.LogError("[TitleUI] title scene is the last scene, cannot determine next scene name");
        }
        else
        {
            Debug.LogError("[TitleUI] SceneManagementService is not available!");
        }
    }

    private void ExitGame()
    {
        Debug.Log("[TitleUI] Exiting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}