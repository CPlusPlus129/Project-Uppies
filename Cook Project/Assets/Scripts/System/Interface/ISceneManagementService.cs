using Cysharp.Threading.Tasks;
using R3;

public interface ISceneManagementService : IGameService
{
    ReactiveProperty<string> CurrentSceneName { get; }

    UniTask LoadSceneAsync(string sceneName, string spawnPointId = null, SceneTransitionType transitionType = SceneTransitionType.Fade);
    string GetNextSceneName();
    void SetPlayerSpawnInfo(string sceneName, string spawnPointId);
}