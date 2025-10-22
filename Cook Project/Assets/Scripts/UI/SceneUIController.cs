using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

public class SceneUIController : MonoBehaviour
{
    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        var sceneService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
        sceneService.CurrentSceneName.Subscribe(name => SetupUIForScene(name)).AddTo(this);
        SetupUIForScene(sceneService.CurrentSceneName.Value);
    }

    private void SetupUIForScene(string sceneName)
    {
        UIRoot root = UIRoot.Instance;
        var transitionUI = root.GetUIComponent<TransitionCanvas>();
        root.CloseAll(transitionUI.transform);
        switch (sceneName)
        {
            case "Title":
                root.GetUIComponent<TitleUI>()?.gameObject.SetActive(true);
                break;
            default:
                root.GetUIComponent<HUD>()?.gameObject.SetActive(true);
                root.GetUIComponent<CompleteUI>()?.gameObject.SetActive(true);
                root.GetUIComponent<WorldBroadcastUI>()?.gameObject.SetActive(true);
                break;
        }
    }

}