using Cysharp.Threading.Tasks;
using UnityEngine;

public class StartGameTrigger : MonoBehaviour
{
    [Header("Story Flow")]
    [SerializeField]
    private StorySequenceAsset startingSequence;

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

        if (startingSequence != null)
        {
            GameFlow.Instance.EnqueueSequence(startingSequence);
        }

        var shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.StartGame();
    }
}