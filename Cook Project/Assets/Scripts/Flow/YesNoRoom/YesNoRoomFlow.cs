using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class YesNoRoomFlow : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TriggerZone triggerZone_Yes;
    [SerializeField] private TriggerZone triggerZone_No;
    [SerializeField] private TriggerZone triggerZone_SwitchScene;
    [SerializeField] private Collider floorCollider;
    private CompositeDisposable disposables = new CompositeDisposable();
    private string nextSceneName = "TutorialLevel";

    private void Start()
    {
        StartYesNoRoomFlow().Forget();
    }

    private async UniTaskVoid StartYesNoRoomFlow()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
#if UNITY_WEBGL && !UNITY_EDITOR
        await UniTask.Delay(2000); //wait for webgl to load
#endif
        Debug.Log("YesNoRoomFlow started!");
        PlayerStatSystem.Instance.CanUseWeapon.Value = false;
        playerController.GetComponent<PlayerLightDamage>().enabled = false;
        var sceneService = await ServiceLocator.Instance.GetAsync<ISceneManagementService>();
        await WaitForPlayerToEnterDoor();
        floorCollider.enabled = false;
        await WaitForPlayerToDropThroughZone();
        await sceneService.LoadSceneAsync(nextSceneName);
    }

    private async UniTask WaitForPlayerToEnterDoor()
    {
        var tcs = new UniTaskCompletionSource();

        triggerZone_Yes.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                disposables.Clear();
                tcs.TrySetResult();
            })
            .AddTo(disposables);
        triggerZone_No.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                disposables.Clear();
                tcs.TrySetResult();
            })
            .AddTo(disposables);

        await tcs.Task;
    }

    private async UniTask WaitForPlayerToDropThroughZone()
    {
        var tcs = new UniTaskCompletionSource();

        triggerZone_SwitchScene.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                disposables.Clear();
                tcs.TrySetResult();
            })
            .AddTo(disposables);

        await tcs.Task;
    }
}