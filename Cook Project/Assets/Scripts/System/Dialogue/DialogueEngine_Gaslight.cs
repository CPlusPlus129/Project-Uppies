using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;

class DialogueEngine_Gaslight : DialogueEngine, IDialogueService
{
    public Subject<Unit> onEndScenario { get; } = new Subject<Unit>();

    protected void OnDestroy()
    {
        scenarioManager.onEndScenario -= OnEndScenario;
    }

    public async UniTask StartDialogueAsync(string label)
    {
        var tcs = new UniTaskCompletionSource();
        onEndScenario
            .Take(1)
            .Subscribe(_ => tcs.TrySetResult())
            .AddTo(this);

        StartDialogue(label);
        await tcs.Task;
    }

    async UniTask IGameService.Init()
    {
        Init();
        InitGaslightRelatedService().Forget();
        await UniTask.CompletedTask;
    }

    async UniTaskVoid InitGaslightRelatedService()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        var assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        assetManager = new DialogueAssetManager(assetLoader);
        
        scenarioManager.onEndScenario += OnEndScenario;
    }

    private void OnEndScenario()
    {
        onEndScenario.OnNext(Unit.Default);
    }
}