using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;
using UnityEngine;

class DialogueEngine_Gaslight : DialogueEngine, IDialogueService
{
    public Subject<Unit> onEndScenario { get; } = new Subject<Unit>();
    private readonly UniTaskCompletionSource readyTcs = new UniTaskCompletionSource();
    private bool isRuntimeReady;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        scenarioManager.onEndScenario -= OnEndScenario;
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
        var assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        assetManager = new DialogueAssetManager(assetLoader);
        
        scenarioManager.onEndScenario += OnEndScenario;
        isRuntimeReady = true;
        readyTcs.TrySetResult();
    }

    private void OnEndScenario()
    {
        onEndScenario.OnNext(Unit.Default);
    }

    void System.IDisposable.Dispose()
    {
        onEndScenario?.Dispose();

        if (scenarioManager != null)
        {
            scenarioManager.onEndScenario -= OnEndScenario;
        }

        if (!isRuntimeReady)
        {
            readyTcs.TrySetCanceled();
        }
    }
}
