using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class TitleUI : MonoBehaviour
{
    public string startSceneName = "Tutorial";
    [Header("UI References")]
    public Button startButton;
    public Button exitButton;

    private ISceneManagementService sceneManagementService;

    private async void Start()
    {
        sceneManagementService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();

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

    private void OnStartButtonClicked(Unit _)
    {
        LoadIntroScene().Forget();
    }

    private void OnExitButtonClicked(Unit _)
    {
        ExitGame();
    }

    private async UniTaskVoid LoadIntroScene()
    {
        if (sceneManagementService != null)
        {
            await sceneManagementService.LoadSceneAsync(startSceneName, null, SceneTransitionType.Fade);

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