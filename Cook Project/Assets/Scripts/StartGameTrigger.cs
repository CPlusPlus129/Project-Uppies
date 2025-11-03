using Cysharp.Threading.Tasks;
using UnityEngine;

public class StartGameTrigger : MonoBehaviour
{
    private void Start()
    {
        StartGame().Forget();
    }

    private async UniTask StartGame()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
#if UNITY_WEBGL && !UNITY_EDITOR
        await UniTask.Delay(2000); //wait for webgl to load
#endif
        var shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.StartGame();
    }
}