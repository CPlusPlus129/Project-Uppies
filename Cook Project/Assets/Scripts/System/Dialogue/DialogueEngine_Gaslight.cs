using Cysharp.Threading.Tasks;
using DialogueModule;

class DialogueEngine_Gaslight : DialogueEngine, IDialogueService
{
    async UniTask IGameService.Init()
    {
        Init();
        InitDialogueAssetManager().Forget();
        await UniTask.CompletedTask;
    }

    async UniTaskVoid InitDialogueAssetManager()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        var assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        assetManager = new DialogueAssetManager(assetLoader);
    }
}