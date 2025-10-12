using Cysharp.Threading.Tasks;

public class GameFlow : MonoSingleton<GameFlow>
{
    public bool isInitialized { get; private set; } = false;

    protected override void Awake()
    {
        if (isInitialized) return;
        base.Awake();
        StartGame().Forget();
    }

    private async UniTask StartGame()
    {
        isInitialized = false;
        await InitServices();
        await LoadTables();
        await SetupGame();
        isInitialized = true;
        await StartGameLoop();
    }

    private async UniTask InitServices()
    {
        await ServiceLocator.Instance.Init();
    }

    private async UniTask LoadTables()
    {
        var tableManager = await ServiceLocator.Instance.GetAsync<ITableManager>();
        await tableManager.LoadAllTables();
    }

    private async UniTask SetupGame()
    {
        await UniTask.CompletedTask;
    }

    private async UniTask StartGameLoop()
    {        
        await UniTask.CompletedTask;
    }

}