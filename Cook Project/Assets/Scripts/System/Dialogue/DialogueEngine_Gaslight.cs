using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;
using UnityEngine;

class DialogueEngine_Gaslight : DialogueEngine, IDialogueService
{
    public Subject<Unit> onBeginScenario { get; } = new Subject<Unit>();
    public Subject<Unit> onEndScenario { get; } = new Subject<Unit>();
    private readonly UniTaskCompletionSource readyTcs = new UniTaskCompletionSource();
    private bool isRuntimeReady;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (scenarioManager != null)
        {
            scenarioManager.onBeginScenario -= OnBeginScenario;
            scenarioManager.onEndScenario -= OnEndScenario;
        }
        if (!isRuntimeReady)
        {
            readyTcs.TrySetCanceled();
        }
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
        await UniTask.NextFrame();
    }

    public bool IsRuntimeReady => isRuntimeReady;

    public UniTask WaitUntilReadyAsync()
    {
        return isRuntimeReady ? UniTask.CompletedTask : readyTcs.Task;
    }

    async UniTask IGameService.Init()
    {
        Init();
        InitGaslightRelatedService().Forget();
        await UniTask.CompletedTask;
    }

    async UniTaskVoid InitGaslightRelatedService()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        await UniTask.WaitUntil(() => uiHasInit);
        
        var assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        assetManager = new DialogueAssetManager(assetLoader);
        
        scenarioManager.onBeginScenario += OnBeginScenario;
        scenarioManager.onEndScenario += OnEndScenario;
        isRuntimeReady = true;
        readyTcs.TrySetResult();
    }

    private void OnBeginScenario()
    {
        onBeginScenario.OnNext(Unit.Default);
    }

    private void OnEndScenario()
    {
        onEndScenario.OnNext(Unit.Default);
    }

    void System.IDisposable.Dispose()
    {
        onBeginScenario?.Dispose();
        onEndScenario?.Dispose();

        if (scenarioManager != null)
        {
            scenarioManager.onBeginScenario -= OnBeginScenario;
            scenarioManager.onEndScenario -= OnEndScenario;
        }

        if (!isRuntimeReady)
        {
            readyTcs.TrySetCanceled();
        }
    }
}
