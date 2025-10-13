using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class FridgeSpawnList : SpawnPointList
{
    [Header("Spawn Settings")]
    public int SpawnCount = 7;
    public FoodSource fridgePrefab;
    public string[] essentialIngredients;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private IFridgeGlowManager glowManager;

    protected override async void Awake()
    {
        base.Awake();
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        glowManager = await ServiceLocator.Instance.GetAsync<IFridgeGlowManager>();
        var shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.OnGameStart.Subscribe(_ => SpawnFridges()).AddTo(this);
    }

    private void SpawnFridges()
    {
        Reset();

        var spArr = RandomHelper.PickWithoutReplacement(spawnPoints, SpawnCount);
        var eArr = RandomHelper.PickWithoutReplacement(essentialIngredients, essentialIngredients.Length);
        var essentialIndex = 0;

        if (enableDebugLogs) Debug.Log($"Spawning {SpawnCount} fridges");

        foreach (var spawnPoint in spArr)
        {
            var f = spawnPoint.Spawn(fridgePrefab);

            if (essentialIndex < eArr.Length)
            {
                f.SetItemName(eArr[essentialIndex]);
                if (enableDebugLogs) Debug.Log($"Spawned fridge with ingredient: {eArr[essentialIndex]}");
                essentialIndex++;
            }
            else
            {
                var randomIngredient = RandomHelper.PickOne(essentialIngredients);
                f.SetItemName(randomIngredient);
                if (enableDebugLogs) Debug.Log($"Spawned fridge with random ingredient: {randomIngredient}");
            }
        }

        StartCoroutine(RefreshGlowStatesDelayed());
    }

    private System.Collections.IEnumerator RefreshGlowStatesDelayed()
    {
        yield return null;

        if (glowManager != null)
        {
            glowManager.RefreshGlowStates();
            if (enableDebugLogs) Debug.Log("Refreshed glow states after spawning");
        }
    }

    private void Reset()
    {
        foreach (var spawnPoint in spawnPoints)
        {
            spawnPoint.Reset();
        }
    }
}
