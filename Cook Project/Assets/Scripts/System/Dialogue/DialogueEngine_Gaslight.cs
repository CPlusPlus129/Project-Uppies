using Cysharp.Threading.Tasks;
using DialogueModule;
using R3;
using UnityEngine;

class DialogueEngine_Gaslight : DialogueEngine, IDialogueService
{
    public Subject<Unit> onEndScenario { get; } = new Subject<Unit>();
    private readonly UniTaskCompletionSource readyTcs = new UniTaskCompletionSource();
    private bool isRuntimeReady;
    [SerializeField] private DialogueModule.DialogueUIManager dialogueUiPrefab;
    [SerializeField] private Transform uiParentOverride;
    private DialogueModule.DialogueUIManager spawnedUi;

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
        await EnsureDialogueUiAsync();
        
        scenarioManager.onEndScenario += OnEndScenario;
        isRuntimeReady = true;
        readyTcs.TrySetResult();
    }

    private async UniTask EnsureDialogueUiAsync()
    {
        var managers = await FindExistingUiManagersAsync();
        if (managers != null && managers.Length > 0)
        {
            foreach (var manager in managers)
            {
                if (manager == null)
                {
                    continue;
                }

                manager.SetEngine(this);
                manager.Init();
                spawnedUi ??= manager;
            }

            return;
        }

        if (dialogueUiPrefab == null)
        {
            Debug.LogWarning($"[{nameof(DialogueEngine_Gaslight)}] No Dialogue UI manager present in the scene and no fallback prefab assigned. Dialogue will not be visible.");
            return;
        }

        var parent = uiParentOverride;
        if (parent == null && UIRoot.Instance != null)
        {
            parent = UIRoot.Instance.transform;
        }

        spawnedUi = Object.Instantiate(dialogueUiPrefab, parent ?? transform);
        spawnedUi.name = dialogueUiPrefab.name;
        spawnedUi.SetEngine(this);
        spawnedUi.Init();
    }

    private async UniTask<DialogueModule.DialogueUIManager[]> FindExistingUiManagersAsync()
    {
        DialogueModule.DialogueUIManager[] managers = null;
        for (int i = 0; i < 30; i++)
        {
            managers = Object.FindObjectsByType<DialogueModule.DialogueUIManager>(FindObjectsSortMode.None);
            if (managers != null && managers.Length > 0)
            {
                break;
            }

            await UniTask.NextFrame();
        }

        return managers;
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
